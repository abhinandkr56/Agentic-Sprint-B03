// ─────────────────────────────────────────────────────────────────────────────
// Session 10 assignment starter · Agent Frameworks in .NET: From Semantic Kernel to MAF
//
// Syllabus: "Build one simple agent using the Microsoft Agent Framework that completes a small
//            multi-step task, e.g. 'look up an order and draft a reply.'"
//
// This is your Session 9 MyAiApi (/ask, /triage, /rag, POST /chat with local + MCP tools and the gate).
// It runs as-is. Work through TODO 0 → TODO 5; Builds 3 and 5 showed every step live.
//
//   docker start pgvector   ·   ollama list (all-minilm)   ·   dotnet run   ·   then MyAiApi.http
// ─────────────────────────────────────────────────────────────────────────────
// ── TODO 0 · The package ─────────────────────────────────────────────────────
// MyAiApi.csproj already references Microsoft.Agents.AI 1.25.0. In your OWN MyAiApi:
//   dotnet add package Microsoft.Agents.AI --version 1.25.0
using System.ClientModel;
using System.ComponentModel;
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

// Session 7: the tool loop for /chat. (The agent tonight builds its own, from the same plain client.)
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

// ── TODO 1 · Prompts.Agent (Models.cs): the agent's job, in 4–6 lines (Build 3's Rules) ──

// ── TODO 2 · A second tool: FindOrders(email), a stub is fine (Build 3, bottom of Program.cs) ──
// Write it at the bottom of this file, next to GetOnCall. Give it a [Description] that says WHEN to use it.

// ── TODO 3 · The agent: create it ONCE, here at startup (Build 5, EDIT 1) ─────
//   AIAgent supportAgent = client.AsAIAgent(name: "SupportAgent", instructions: Prompts.Agent,
//       tools: [AIFunctionFactory.Create(FindOrders, "FindOrders"), .. orderTools]);
// Use the plain `client` (the agent adds its own tool loop) and the UNWRAPPED orderTools (GetOrder only reads).

// ── TODO 4 · POST /agent/reply: run it, return the draft AND the steps (Build 5, EDIT 1–2) ──
//   app.MapPost("/agent/reply", async (ReplyRequest req) => { ... return new AgentReply(draft, provider, model, steps); });
// Steps = every FunctionCallContent in response.Messages, as "Name(args)".

// ── TODO 5 · REPLY.md: run 3 tickets (email only · order number · neither), paste steps + draft, mark each right/wrong ──

// ── STRETCH (pick one or more) ──────────────────────────────────────────────────
// S1. Conversations (Build 5, EDIT 3): a ConcurrentDictionary<string, AgentSession> keyed by req.ConversationId.
//     Send a follow-up ("which order was that?") with the same id, then with a new id.
// S2. Middleware (Build 4): agent.AsBuilder().Use(async (_, call, next, ct) => ...).Build(): log every tool call
//     with its time, then add an IssueRefund stub and BLOCK it in the middleware. Ask the agent for a refund.
// S3. Stream it: a GET /agent/stream?ticket=... that writes agent.RunStreamingAsync(...) updates as they arrive.
// S4. Typed draft: agent.RunAsync<Draft>(ticket) with record Draft(string Subject, string Body, bool NeedsHuman).
// S5. Same ticket to POST /chat and to POST /agent/reply. What's actually different? (Hint: Build 1, EDIT 2.)
// S6. --AI:Provider=Gemini: does the agent's two-step job work there? (Session 7's thought-signature issue.)

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
