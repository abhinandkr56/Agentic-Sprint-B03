// ─────────────────────────────────────────────────────────────────────────────
// Session 09 assignment starter · Using MCP Tools from Your .NET App
//
// Syllabus: "Connect your Session 3 API to the MCP server from Session 8 as a client,
//            and get the model to successfully use one of its tools."
//
// This is your Session 7 MyAiApi (/ask, /triage, /rag, POST /chat with GetOnCall + SearchNotes).
// It runs as-is. Work through TODO 0 → TODO 5; Build 4 (McpApi) showed every step live.
//
//   docker start pgvector   ·   ollama list (all-minilm)   ·   dotnet run   ·   then MyAiApi.http
// ─────────────────────────────────────────────────────────────────────────────
// ── TODO 0 · The model ───────────────────────────────────────────────────────
// Groq shut down llama-3.3-70b-versatile for free accounts on 16 Aug 2026. This starter's appsettings.json already
// says "openai/gpt-oss-120b". Using your OWN MyAiApi? Change AI:Groq:Model in its appsettings.json the same way.
using System.ClientModel;
using System.ComponentModel;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ModelContextProtocol.Client;
using MyAiApi;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Sessions 3–4: the chat provider comes from config. Groq by default; --AI:Provider=Gemini or =Ollama to swap.
// Session 9: Groq retired llama-3.3-70b-versatile. appsettings.json now says "openai/gpt-oss-120b" (TODO 0).
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

// Session 7: the tool loop. Only active when a request passes tools: /ask, /triage, /rag are unchanged.
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

// ── TODO 1 · Connect to the MCP server you explored in Session 8 (Build 4, EDIT 1) ──
// Default: Microsoft Learn (remote, Streamable HTTP, no key). Explored another server? Use that one.
//   await using var learn = await McpClient.CreateAsync(new HttpClientTransport(new() { Name = "learn", Endpoint = new Uri("https://learn.microsoft.com/api/mcp") }));
// Connect ONCE, here at startup (before app.Run), not inside /chat: one connection, shared by every request.

// ── TODO 2 · Hand the model its tools (Build 1, EDIT 1–2) ────────────────────────
// An McpClientTool IS an AIFunction: add it to chatOptions.Tools next to GetOnCall and SearchNotes.
// Only the tool(s) you need, e.g.  .Where(t => t.Name == "microsoft_docs_search")
// Then print what /chat can call:  Console.WriteLine($"/chat tools: {string.Join(", ", chatOptions.Tools!.Select(t => t.Name))}");

// ── TODO 3 · Show it at GET / ────────────────────────────────────────────────────
// Add the tool names to GET / so you can see what the model can call:  tools = chatOptions.Tools!.Select(t => t.Name)
app.MapGet("/", () => new { provider, model, endpoint, keySet = key != "not-needed", notes = collection.Name });

// ── TODO 4 · Prompts.Chat (Models.cs): say WHEN to use the MCP tool, and to cite it ──
// ── TODO 5 · MCP.md: 3 prompts (one should use the MCP tool, one a local tool, one both) + what came back ──

// ── STRETCH (pick one or more) ──────────────────────────────────────────────────
// S1. A local server too: Session 8's OrdersServer over stdio (Build 4, EDIT 2), started with
//     InheritEnvironmentVariables = false + StdioClientTransportOptions.GetDefaultEnvironmentVariables().
// S2. The gate (Build 4, EDIT 3): every tool you haven't reviewed becomes new ApprovalRequiredAIFunction(tool).
//     Return needsApproval like Build 4 does. Which tools did you trust, and why?
// S3. Build 3 against YOUR terminal: how many environment variables, how many secrets, before and after EDIT 1?
// S4. Token check: ask the same docs question with all of Learn's tools vs only microsoft_docs_search. Tokens in?
// S5. Resilience: Learn says tool lists can change. Catch a failed MCP call, call ListToolsAsync() again, retry once.
// S6. --AI:Provider=Gemini (gemini-3.6-flash): do MCP tools work there? (Session 7's thought-signature issue.)

// POST /chat: a question in, an answer out, and which tools the model called (Session 7)
app.MapPost("/chat", async (AskRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Chat), new(ChatRole.User, req.Question)];
    var response = await chat.GetResponseAsync(messages, chatOptions);
    List<string> toolCalls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
        .Select(c => $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})").ToList();
    return new ChatReply(response.Text, provider, response.ModelId ?? model, toolCalls);
});

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
