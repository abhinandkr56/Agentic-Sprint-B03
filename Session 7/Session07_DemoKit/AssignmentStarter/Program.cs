// ─────────────────────────────────────────────────────────────────────────────
// Session 07 assignment starter · Function & Tool Calling Basics
//
// Syllabus: "Give your chat client one simple C# tool (a stub is fine) and get the model
//            to call it correctly on 3 different prompts."
//
// This is your Session 6 MyAiApi (/ask, /triage, /rag), plus a new POST /chat.
// It runs as-is: /chat answers, but has no tools yet, so ToolCalls is always empty. Work through TODO 1 → TODO 5.
//
//   docker start pgvector   ·   ollama list (all-minilm)   ·   dotnet run   ·   then MyAiApi.http
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using System.ComponentModel;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
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

// ── TODO 3 · Turn on the tool loop (Build 2, EDIT 1 / Build 5, EDIT 1) ──────────
// Add .UseFunctionInvocation() to this line. It only does something when a request passes tools,
// so /ask, /triage and /rag behave exactly as before.
builder.Services.AddChatClient(client);

// Session 5's store: your notes in pgvector, embedded by all-minilm on Ollama (Session 6's /rag uses it).
// Connection string from user-secrets or the env var ConnectionStrings__Vectors; falls back to the local Docker container.
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

app.MapGet("/", () => new { provider, model, endpoint, keySet = key != "not-needed", notes = collection.Name });

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

// Session 6: POST /rag, a grounded answer and its sources (your solution; unchanged)
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

// ── TODO 2 · Turn your method into a tool (Build 1, EDIT 1) ─────────────────────
//     var chatOptions = new ChatOptions { Tools = [AIFunctionFactory.Create(GetOnCall, "GetOnCall")] };
// (Pass the name: local functions get mangled names like _Main_g_GetOnCall_0_0.)

// NEW · POST /chat: a question in, an answer out, and which tools the model called.
app.MapPost("/chat", async (AskRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Chat), new(ChatRole.User, req.Question)];

    // ── TODO 4 · Pass the tools, and report which ones were called ────────────────
    // 1. GetResponseAsync(messages, chatOptions)
    // 2. toolCalls = every FunctionCallContent in response.Messages, as "Name(arg1, arg2)" (Build 5's toolCalls line)
    var response = await chat.GetResponseAsync(messages);
    List<string> toolCalls = [];

    return new ChatReply(response.Text, provider, response.ModelId ?? model, toolCalls);
});

app.Run();

// ── TODO 1 · Write ONE tool: a plain C# method. A stub is fine ──────────────────
// Pick something your team asks about that needs LIVE data. Ideas: who's on call for a team, a build's status,
// an environment's version, a ticket's state, today's deploy window. Return a short string.
// Then DESCRIBE it (Build 2, EDIT 3): [Description] on the method (what it does + WHEN to use it) and on each parameter.
//
//   [Description("...what it does, and when to call it...")]
//   static string GetOnCall([Description("...")] string team)
//   {
//       Console.WriteLine($"  [tool] GetOnCall({team})");      // so you can SEE when the model calls it
//       return team.ToLowerInvariant() switch { "payments" => "...", _ => $"No on-call rota for {team}." };
//   }

// ── TODO 5 · Prompts (PROMPTS.md) ────────────────────────────────────────────────
// 3 prompts that SHOULD call your tool, each worded differently. 1 that should NOT. 1 with a missing argument.
// For each: ToolCalls, the answer, and was it right? Fill in PROMPTS.md.

// ── STRETCH (pick one or more) ──────────────────────────────────────────────────
// S1. Agentic RAG: make /rag's search a second tool, SearchNotes(query) (Build 3). Ask something that needs both tools.
// S2. Run /chat on Gemini (--AI:Provider=Gemini --urls http://localhost:5081) and on Ollama. What happens? (Build 4)
// S3. Add a risky tool (RestartService? DeleteNote?) and wrap it in ApprovalRequiredAIFunction. What comes back? (Build 5)
// S4. Print your tool's JsonSchema at startup. Remove a [Description] and watch the schema (and the behaviour) change.
// S5. ChatOptions.ToolMode: try ChatToolMode.RequireAny and ChatToolMode.None on the prompt that should NOT call it.
