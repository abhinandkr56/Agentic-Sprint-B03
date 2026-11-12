// ─────────────────────────────────────────────────────────────────────────────
// Session 06 assignment SOLUTION · Your First RAG Assistant
//
// Syllabus: "Wire your Session 5 store into a simple 'ask a question, get a grounded answer'
//            endpoint. Try 5 questions and just observe what happens."
//
// TODO 1–5 done, plus stretch S1 (skip the model when nothing passes the gate) and S5 (token count).
//
//   docker start pgvector   ·   ollama pull all-minilm   ·   dotnet run   ·   then MyAiApi.http
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
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
builder.Services.AddChatClient(client);

// TODO 2 · checked: same table, same record, same model as Session 5
// Session 5's store: your notes in pgvector, embedded by all-minilm on Ollama.
// The connection string comes from config: user-secrets or the env var ConnectionStrings__Vectors.
//   dotnet user-secrets init
//   dotnet user-secrets set "ConnectionStrings:Vectors" "Host=localhost;Username=postgres;Password=postgres"
// Without either, it falls back to the local Docker container from Session 5.
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();
var connection = builder.Configuration.GetConnectionString("Vectors") ?? "Host=localhost;Username=postgres;Password=postgres";
VectorStoreCollection<int, Note> collection = new PostgresVectorStore(connection, new() { EmbeddingGenerator = embedder }).GetCollection<int, Note>("notes");
builder.Services.AddSingleton(collection);

var app = builder.Build();

// No "notes" table yet (you skipped Session 5's assignment)? Seed three samples so the endpoint has something to find.
// If the table exists, your own notes are left exactly as they are.
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

// Build 3's gate: below 0.3 it isn't really a match (all-minilm, measured in Session 5)
var gate = new VectorSearchOptions<Note> { ScoreThreshold = 0.3 };

// POST /rag: ask a question, get a grounded answer, and see where it came from
app.MapPost("/rag", async (AskRequest req, IChatClient chat, VectorStoreCollection<int, Note> notes) =>
{
    // TODO 3 · RETRIEVE: the 3 closest notes above the gate
    var hits = await notes.SearchAsync(req.Question, top: 3, gate).ToListAsync();
    List<Source> sources = hits.Select(h => new Source(h.Record.Id, h.Record.Topic, h.Record.Text, Math.Round(h.Score ?? 0, 2))).ToList();

    // STRETCH S1 · nothing relevant? Don't call the model at all. It can't make anything up, and it costs nothing.
    if (sources.Count == 0)
        return new RagResponse("I don't know. It isn't in our notes.", provider, "(model not called)", sources);

    // TODO 4 · AUGMENT: rules + numbered notes + question.  GENERATE: one call.
    var context = string.Join("\n", sources.Select(s => $"[{s.Id}] {s.Text}"));
    List<ChatMessage> messages =
    [
        new(ChatRole.System, Prompts.Rag),
        new(ChatRole.User, $"Notes:\n{context}\n\nQuestion: {req.Question}"),
    ];
    var response = await chat.GetResponseAsync(messages);

    // STRETCH S5 · how big was the prompt? (printed, so the JSON stays the same shape)
    Console.WriteLine($"/rag · {sources.Count} notes · {response.Usage?.InputTokenCount} tokens in · {req.Question}");

    return new RagResponse(response.Text, provider, response.ModelId ?? model, sources);
});

app.Run();

// TODO 5 · QUESTIONS.md is filled in next to this file
// STRETCH S2 · Groq vs Gemini on the same notes:  dotnet run --AI:Provider=Gemini --urls http://localhost:5081
