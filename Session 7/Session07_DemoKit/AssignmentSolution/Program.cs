// ─────────────────────────────────────────────────────────────────────────────
// Session 07 assignment SOLUTION · Function & Tool Calling Basics
//
// TODO 1–5 done with a GetOnCall stub, plus stretch S1 (SearchNotes: /rag's search as a second tool)
// and S4 (the tools' schemas printed at startup).
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

// TODO 3 · the tool loop. Only active when a request passes tools: /ask, /triage, /rag are unchanged.
builder.Services.AddChatClient(client).UseFunctionInvocation();

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

// TODO 2 · the tools (STRETCH S1: SearchNotes, Session 6's search, now chosen by the model)
var chatOptions = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetOnCall, "GetOnCall"), AIFunctionFactory.Create(SearchNotes, "SearchNotes")],
};

// STRETCH S4 · this is all the model knows about our methods
foreach (var tool in chatOptions.Tools.OfType<AIFunction>())
    Console.WriteLine($"Tool {tool.Name}: {tool.JsonSchema}");

// POST /chat: a question in, an answer out, and which tools the model called
app.MapPost("/chat", async (AskRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Chat), new(ChatRole.User, req.Question)];

    // TODO 4 · pass the tools; report every call the model made
    var response = await chat.GetResponseAsync(messages, chatOptions);
    List<string> toolCalls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
        .Select(c => $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})").ToList();

    return new ChatReply(response.Text, provider, response.ModelId ?? model, toolCalls);
});

app.Run();

// TODO 1 · the tool: a stub on-call rota. In real life: the PagerDuty or Opsgenie API.
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

// STRETCH S1 · Session 6's /rag search, as a tool: the model decides when to look in the notes
[Description("Searches our team's notes (API limits, deployments, incidents, onboarding). Returns numbered notes.")]
async Task<string> SearchNotes([Description("What to look for, in a few words")] string query)
{
    var hits = await collection.SearchAsync(query, top: 3, gate).ToListAsync();
    Console.WriteLine($"  [tool] SearchNotes(\"{query}\") -> {hits.Count} hit(s)");
    return hits.Count == 0 ? "No matching note." : string.Join("\n", hits.Select(h => $"[{h.Record.Id}] {h.Record.Text}"));
}
