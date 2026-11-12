using System.ClientModel;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// The chat model: Groq by default; AI:Provider=Gemini swaps it. The key comes from the variable KeyVariable names.
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var chatKey = Environment.GetEnvironmentVariable(ai["KeyVariable"]!) ?? "not-set";

IChatClient chat = new OpenAIClient(new ApiKeyCredential(chatKey), new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient()
    .AsBuilder().UseOpenTelemetry().Build();

// The embedding model: Gemini (Groq has no embeddings endpoint).
var emb = builder.Configuration.GetSection("Embeddings");
var embKey = Environment.GetEnvironmentVariable(emb["KeyVariable"]!) ?? "not-set";
IEmbeddingGenerator<string, Embedding<float>> embedder =
    new OpenAIClient(new ApiKeyCredential(embKey), new OpenAIClientOptions { Endpoint = new Uri(emb["Endpoint"]!) })
        .GetEmbeddingClient(emb["Model"]!)
        .AsIEmbeddingGenerator()
        .AsBuilder().UseOpenTelemetry().Build();

// The vector store: Postgres + pgvector through Session 5's connector. The AppHost hands us the connection string.
var store = new PostgresVectorStore(builder.Configuration.GetConnectionString("vectors")!, new() { EmbeddingGenerator = embedder });
VectorStoreCollection<int, Note> collection = store.GetCollection<int, Note>("notes");
await collection.EnsureCollectionExistsAsync();   // CREATE EXTENSION vector + CREATE TABLE notes, at startup

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/ask", async (string q) =>
{
    // 1. Retrieve: the three notes closest in meaning to the question (the store embeds q for us)
    var notes = new List<string>();
    await foreach (var result in collection.SearchAsync(q, top: 3))
        notes.Add(result.Record.Text);

    // 2. Generate: answer from those notes only
    var prompt = $"""
        Answer the question using ONLY these notes. If they don't contain the answer, say "I don't know."
        Notes:
        - {string.Join("\n- ", notes)}
        Question: {q}
        """;
    var response = await chat.GetResponseAsync(prompt);
    app.Logger.LogInformation("Answered {Question} from {NoteCount} notes with {Model}", q, notes.Count, response.ModelId);
    return new { answer = response.Text, notes, model = response.ModelId };
});

app.MapPost("/notes", async (Note note) =>
{
    await collection.UpsertAsync(note);   // embeds note.Text with Gemini, then inserts the row (or updates it: same Id)
    return new { saved = note.Id, note.Text };
});

app.Run();

// One note = one row. The attributes tell the store which property is which (Session 5).
class Note
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(3072, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}
