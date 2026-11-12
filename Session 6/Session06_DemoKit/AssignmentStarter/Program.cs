// ─────────────────────────────────────────────────────────────────────────────
// Session 06 assignment starter · Your First RAG Assistant
//
// Syllabus: "Wire your Session 5 store into a simple 'ask a question, get a grounded answer'
//            endpoint. Try 5 questions and just observe what happens."
//
// This is your Session 4 MyAiApi plus your Session 5 notes in pgvector, already connected.
// It runs as-is. POST /rag answers, but without looking at your notes. Work through TODO 1 → TODO 5.
//
//   PowerShell:  $env:GROQ_API_KEY = "gsk_..."      bash/zsh:  export GROQ_API_KEY="gsk_..."
//   dotnet run        then send the requests in MyAiApi.http
//
// ── TODO 1 · Start the pieces (terminal, not code) ──────────────────────────
//   docker start pgvector        (or the docker run command from Session 5)
//   ollama pull all-minilm       (already there since Session 5)
// Done when `docker ps` shows pgvector "Up" and GET http://localhost:5080/ answers.
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

// ── TODO 2 · Check this matches YOUR Session 5 store ───────────────────────────
// Same table name ("notes"), same record shape (Note in Models.cs), same embedding model (all-minilm, 384).
// A different model or size here than in Session 5 = vectors that don't match. (Session 5, Build 5)
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

// NEW · POST /rag: "ask a question, get a grounded answer". Today it runs, but it isn't grounded:
// it never looks at your notes, so Sources is always empty. TODO 3 and TODO 4 fix that (Build 5 from class).
app.MapPost("/rag", async (AskRequest req, IChatClient chat, VectorStoreCollection<int, Note> notes) =>
{
    // ── TODO 3 · RETRIEVE ─────────────────────────────────────────────────────
    // Search your notes for the 3 closest to req.Question, with a minimum score (Build 3's gate):
    //     var gate = new VectorSearchOptions<Note> { ScoreThreshold = 0.3 };
    //     var hits = await notes.SearchAsync(req.Question, top: 3, gate).ToListAsync();
    // Then turn the hits into Sources:  new Source(h.Record.Id, h.Record.Topic, h.Record.Text, Math.Round(h.Score ?? 0, 2))
    List<Source> sources = [];

    // ── TODO 4 · AUGMENT + GENERATE ───────────────────────────────────────────
    // Build the prompt like Build 5: Prompts.Rag as the system message, then a user message with
    // the numbered notes ("[3] text", one per line) and the question. Ask the model with both messages.
    var response = await chat.GetResponseAsync(req.Question);

    return new RagResponse(response.Text, provider, response.ModelId ?? model, sources);
});

app.Run();

// ── TODO 5 · Try 5 questions and just observe (MyAiApi.http has a template for each) ─────
// 1. answered by one note, in its own words   2. the same, in DIFFERENT words   3. NOT in your notes
// 4. needs TWO notes to answer   5. your choice: another language, a negation, a trick
// For each: what came back, which sources, right or wrong, and WHY (retrieval or generation?). Fill in QUESTIONS.md.

// ── STRETCH (pick one or more) ─────────────────────────────────────────────
// S1. Don't call the model when nothing passes the gate: return "I don't know" with empty Sources (Build 3, EDIT 2).
// S2. Groq vs Gemini, same notes: dotnet run --AI:Provider=Gemini --urls http://localhost:5081  (Build 5, EDIT 3).
// S3. Add a real document: cut a long text of yours into chunks on "## " and upsert them into notes (Build 4).
// S4. Ask in another language. Then try gemini-embedding-001 in a NEW collection "notes_gemini" (size 3072).
// S5. Return the token count too (response.Usage?.InputTokenCount) and try top: 1 vs top: 5. What changes?
