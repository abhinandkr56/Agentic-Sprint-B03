// ─────────────────────────────────────────────────────────────────────────────
// Session 11 assignment starter · Multiple Agents Working Together
//
// Syllabus: "Build a 2-agent workflow (e.g. Writer → Reviewer) for one small task, and run it end-to-end once."
//
// This is your Session 10 MyAiApi: /ask, /triage, /rag, /chat, and POST /agent/reply with the SupportAgent
// (FindOrders + Session 8's MCP GetOrder, middleware that blocks IssueRefund, a session per conversation).
// It runs as-is. Work through TODO 0 → TODO 4; Builds 1, 2 and 5 showed every step live.
//
//   docker start pgvector   ·   ollama list (all-minilm)   ·   dotnet run   ·   then MyAiApi.http
// ─────────────────────────────────────────────────────────────────────────────
// ── TODO 0 · Nothing to install ──────────────────────────────────────────────
// The reviewer is a second Microsoft.Agents.AI agent: the package you added in Session 10.
// (MyAiApi.csproj also references Microsoft.Agents.AI.Workflows 1.25.0 for stretch S1. In your OWN MyAiApi:
//   dotnet add package Microsoft.Agents.AI.Workflows --version 1.25.0   only if you try S1.)
using System.ClientModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ModelContextProtocol.Client;
using MyAiApi;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Sessions 3–4: the chat provider comes from config. Groq by default; --AI:Provider=Gemini or =Ollama to swap.
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var endpoint = ai["Endpoint"] ?? "";
var model = ai["Model"] ?? "";
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "not-needed";
var nativeJsonSchema = ai.GetValue("NativeJsonSchema", true);

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(model)
    .AsIChatClient();

// Session 7: the tool loop for /chat. (The agents build their own, from the same plain client.)
builder.Services.AddChatClient(client).UseFunctionInvocation();

// Session 5's store: your notes in pgvector, embedded by all-minilm on Ollama (Session 6's /rag uses it).
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();
var connection = builder.Configuration.GetConnectionString("Vectors") ?? "Host=localhost;Username=postgres;Password=postgres";
VectorStoreCollection<int, Note> collection = new PostgresVectorStore(connection, new() { EmbeddingGenerator = embedder }).GetCollection<int, Note>("notes");
builder.Services.AddSingleton(collection);
var gate = new VectorSearchOptions<Note> { ScoreThreshold = 0.3 };

var app = builder.Build();

// No "notes" table yet? Seed three samples. If the table exists, your own notes are left exactly as they are.
if (!await collection.CollectionExistsAsync())
{
    await collection.EnsureCollectionExistsAsync();
    await collection.UpsertAsync(new List<Note>
    {
        new() { Id = 1, Topic = "API", Text = "Our API returns HTTP 429 when you send more than 100 requests per minute." },
        new() { Id = 2, Topic = "Deployments", Text = "Deployments to production happen every Tuesday and Thursday after 6 pm IST." },
        new() { Id = 3, Topic = "Incidents", Text = "If the site is down, page the on-call engineer through PagerDuty, then post in #incidents." },
    });
    Console.WriteLine("No notes table found: seeded 3 sample notes");
}

app.MapPost("/ask", async (AskRequest req, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(req.Question);
    return new AskResponse(response.Text, provider, response.ModelId ?? model);
});

app.MapPost("/triage", async (TriageRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Triage), new(ChatRole.User, req.Text)];
    var response = await chat.GetResponseAsync<TicketTriage>(messages, useJsonSchemaResponseFormat: nativeJsonSchema);
    return response.TryGetResult(out var result) ? Results.Ok(result) : Results.Problem(response.Text);
});

// Session 6: POST /rag, a grounded answer and its sources (unchanged)
app.MapPost("/rag", async (AskRequest req, IChatClient chat, VectorStoreCollection<int, Note> notes) =>
{
    var hits = await notes.SearchAsync(req.Question, top: 3, gate).ToListAsync();
    List<Source> sources = hits.Select(h => new Source(h.Record.Id, h.Record.Topic, h.Record.Text, Math.Round(h.Score ?? 0, 2))).ToList();
    if (sources.Count == 0)
        return new RagResponse("I don't know. It isn't in our notes.", provider, "(model not called)", sources);
    var context = string.Join("\n", sources.Select(s => $"[{s.Id}] {s.Text}"));
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Rag), new(ChatRole.User, $"Notes:\n{context}\n\nQuestion: {req.Question}")];
    var response = await chat.GetResponseAsync(messages);
    return new RagResponse(response.Text, provider, response.ModelId ?? model, sources);
});

// Session 7: the local tools (GetOnCall + stretch S1's SearchNotes)
var chatOptions = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetOnCall, "GetOnCall"), AIFunctionFactory.Create(SearchNotes, "SearchNotes")],
};

// Session 9: Microsoft Learn (remote MCP) + Session 8's OrdersServer (local MCP, minimal environment)
await using var learn = await McpClient.CreateAsync(new HttpClientTransport(new() { Name = "learn", Endpoint = new Uri("https://learn.microsoft.com/api/mcp") }));
Console.WriteLine($"MCP: connected to {learn.ServerInfo.Name} (protocol {learn.NegotiatedProtocolVersion})");
await using var orders = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "orders", Command = "dotnet", Arguments = ["run", "--project", "../OrdersServer"],
    InheritEnvironmentVariables = false, EnvironmentVariables = StdioClientTransportOptions.GetDefaultEnvironmentVariables(),
}));
var orderTools = await orders.ListToolsAsync();   // Session 10: the agent reuses these
foreach (var tool in (await learn.ListToolsAsync()).Where(t => t.Name == "microsoft_docs_search")) chatOptions.Tools!.Add(tool);
foreach (var tool in orderTools) chatOptions.Tools!.Add(tool);

// Session 9's gate for /chat: tools we haven't reviewed wait for a human
string[] reviewed = ["GetOnCall", "SearchNotes", "microsoft_docs_search"];
chatOptions.Tools = [.. chatOptions.Tools!.Select(t => reviewed.Contains(t.Name) ? t : new ApprovalRequiredAIFunction((AIFunction)t))];

app.MapGet("/", () => new { provider, model, endpoint, keySet = key != "not-needed", notes = collection.Name, tools = chatOptions.Tools.Select(t => t.Name) });

// POST /chat: Session 7–9 (unchanged)
app.MapPost("/chat", async (AskRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Chat), new(ChatRole.User, req.Question)];
    var response = await chat.GetResponseAsync(messages, chatOptions);
    List<string> toolCalls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
        .Select(c => $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})").ToList();
    toolCalls.AddRange(response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>()
        .Select(a => a.ToolCall is FunctionCallContent c ? $"NEEDS APPROVAL: {c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})" : "NEEDS APPROVAL: ?"));
    return new ChatReply(response.Text, provider, response.ModelId ?? model, toolCalls);
});

// Session 10: the SupportAgent, created once. Plain client (it adds its own tool loop); MCP's GetOrder unwrapped (it only reads).
AIAgent supportAgent = client.AsAIAgent(name: "SupportAgent", instructions: Prompts.Agent,
    tools: [AIFunctionFactory.Create(FindOrders, "FindOrders"), .. orderTools, AIFunctionFactory.Create(IssueRefund, "IssueRefund")]);

// Session 10, stretch S2 · middleware: every tool call logged with its time; refunds blocked until a human says yes (Session 12)
supportAgent = supportAgent.AsBuilder()
    .Use(async (_, call, next, ct) =>
    {
        if (call.Function.Name == "IssueRefund")
        {
            Console.WriteLine($"  [agent] BLOCKED {call.Function.Name}({string.Join(", ", call.Arguments.Values)})");
            return "BLOCKED: refunds need a human. Tell the customer a teammate will review the refund and confirm by email.";
        }
        var timer = Stopwatch.StartNew();
        var result = await next(call, ct);
        Console.WriteLine($"  [agent] {call.Function.Name}({string.Join(", ", call.Arguments.Values)}) took {timer.ElapsedMilliseconds} ms");
        return result;
    })
    .Build();

// Session 10, stretch S1 · one session per conversation (in memory: lost on restart. Session 12 stores them.)
var sessions = new ConcurrentDictionary<string, AgentSession>();

// Session 10 · POST /agent/reply: a ticket in; a draft and the steps that produced it out (unchanged)
app.MapPost("/agent/reply", async (ReplyRequest req) =>
{
    AgentSession? session = null;
    if (req.ConversationId is { } id && !sessions.TryGetValue(id, out session))
        sessions[id] = session = await supportAgent.CreateSessionAsync();

    var response = await supportAgent.RunAsync(req.Ticket, session);
    List<string> steps = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
        .Select(c => $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})").ToList();
    return new AgentReply(response.Text, provider, model, steps);
});

// ── TODO 1 · Prompts.Reviewer (Models.cs): a checklist, one line per rule (Build 1) ──

// ── TODO 2 · The reviewer agent: create it ONCE, here (Build 5, EDIT 1) ──────
//   AIAgent reviewerAgent = client.AsAIAgent(name: "ReviewerAgent", instructions: Prompts.Reviewer);
// Plain client, NO tools: its only job is to read and judge.

// ── TODO 3 · POST /agent/reviewed-reply: writer → reviewer → (one fix round) (Build 5, EDIT 2–3) ──
//   app.MapPost("/agent/reviewed-reply", async (ReplyRequest req) => { ... });
//   a) AgentSession session = await supportAgent.CreateSessionAsync();  var draft = await supportAgent.RunAsync(req.Ticket, session);
//   b) steps = every FunctionCallContent in draft.Messages, as "Name(args)"  (Session 10)
//      facts = every FunctionResultContent in draft.Messages, $"{r.Result}", joined with "\n"
//   c) var review = await reviewerAgent.RunAsync<ReviewVerdict>($"TICKET:\n...\n\nFACTS:\n...\n\nDRAFT:\n{draft.Text}");
//   d) not approved? send review.Result.Problems back to supportAgent IN THE SAME SESSION, then review again (once)
//   e) return new ReviewedReply(draft.Text, review.Result.Approved, review.Result.Problems, rounds, steps, provider, model);

// ── TODO 4 · REVIEW.md: run it end to end on 3 tickets (one should pass, one should be caught, one has no facts) ──

// ── STRETCH (pick one or more) ──────────────────────────────────────────────────
// S1. The same pipeline as a MAF workflow (Build 3): AgentWorkflowBuilder.BuildSequential(supportAgent, reviewerAgent).AsAIAgent()
//     behind POST /agent/reviewed-reply-wf. Print each message's AuthorName. Then try chainOnlyAgentResponses: true.
// S2. A reviewer on a DIFFERENT provider: --AI:ReviewerProvider=Gemini. Different model, different blind spots.
//     (The reviewer has no tools, so Session 7's Gemini thought-signature issue doesn't apply.)
// S3. Two reviewers in parallel: one for tone, one for facts (Task.WhenAll). Approved only if both approve.
// S4. The reviewer as a tool (Build 4): supportAgent with reviewerAgent.AsAIFunction(). Does it ALWAYS call it?
// S5. What did the review cost? Log tokens in per agent, per round (response.Usage?.InputTokenCount).
// S6. Conversations: use Session 10's sessions dictionary for the writer, so a follow-up is reviewed too.

app.Run();

// Session 7's assignment tool: a stub on-call rota. In real life: the PagerDuty or Opsgenie API.
[Description("Gets who is on call right now for an engineering team. Use it when someone needs to page, wake up or reach the on-call engineer.")]
static string GetOnCall([Description("The team name: payments, platform or mobile")] string team)
{
    Console.WriteLine($"  [tool] GetOnCall({team})");
    return team.ToLowerInvariant() switch
    {
        "payments" => "Payments on call: Priya Nair, +91 98450 00001, until 9 am IST tomorrow.",
        "platform" => "Platform on call: Arjun Mehta, +91 98450 00002, until 9 am IST tomorrow.",
        "mobile" => "Mobile on call: Sara Thomas, +91 98450 00003, until 6 pm IST today.",
        _ => $"No on-call rota found for '{team}'. Teams: payments, platform, mobile.",
    };
}

// Session 7, stretch S1 · Session 6's /rag search, as a tool: the model decides when to look in the notes
[Description("Searches our team's notes (API limits, deployments, incidents, onboarding). Returns numbered notes.")]
async Task<string> SearchNotes([Description("What to look for, in a few words")] string query)
{
    var hits = await collection.SearchAsync(query, top: 3, gate).ToListAsync();
    Console.WriteLine($"  [tool] SearchNotes(\"{query}\") -> {hits.Count} hit(s)");
    return hits.Count == 0 ? "No matching note." : string.Join("\n", hits.Select(h => $"[{h.Record.Id}] {h.Record.Text}"));
}

// Session 10 · TODO 2: find a customer's orders from their email (a stub). In real life: your CRM or billing system.
[Description("Finds a customer's orders from their email address. Use it when the customer gives an email but no order number.")]
static string FindOrders([Description("The customer's email address")] string email)
{
    Console.WriteLine($"  [tool] FindOrders({email})");
    return $"Orders for {email}: 4411 (Pro plan, annual, cancelled on 2 Oct); 4413 (SMS pack add-on, delivered on 3 Oct).";
}

// Session 10, stretch S2 · a tool that changes something: the middleware never lets it run
[Description("Issues a full refund for an order to the customer's original payment method.")]
static string IssueRefund([Description("The order number")] int orderId)
{
    Console.WriteLine($"  [tool] IssueRefund({orderId})  >>> REFUND ISSUED <<<");
    return $"Refund issued for order {orderId}. Reference RF-{orderId}-01.";
}
