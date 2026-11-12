// ─────────────────────────────────────────────────────────────────────────────
// Session 05 assignment starter · Vector Search & Microsoft.Extensions.VectorData
//
// Syllabus: "Store 8–10 short pieces of text in a pgvector-backed store and run 3 searches
//            against them. Just get it working — polish isn't the point yet."
//
// This runs as-is: it stores 2 sample notes IN MEMORY and runs 1 search.
// Work through TODO 1 → TODO 5 (it's Build 4 from class, with your own texts), then fill in SEARCHES.md.
//
//   dotnet run
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using CommunityToolkit.VectorData.InMemory;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

// ── TODO 1 · Start Postgres + pgvector, and pull the embedding model (terminal, not code) ──
//   docker run -d --name pgvector -p 5432:5432 -e POSTGRES_PASSWORD=postgres pgvector/pgvector:pg18
//   ollama pull all-minilm
// Done when `docker ps` shows pgvector "Up" and `ollama list` shows all-minilm.
// Port 5432 already taken (a local Postgres)? Use -p 5433:5432 and Port=5433 in TODO 3.

IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();

// ── TODO 2 · Replace these 2 samples with 8–10 short texts of YOUR own ──────
// One or two sentences each, from something you know: your team's FAQ, a runbook, release notes, onboarding docs.
// Give each a Topic (2–4 different topics) so you can filter later (stretch S1).
List<Note> notes =
[
    new() { Id = 1, Topic = "Sample", Text = "Our API returns HTTP 429 when you send more than 100 requests per minute." },
    new() { Id = 2, Topic = "Sample", Text = "Deployments to production happen every Tuesday and Thursday after 6 pm IST." },
];

// ── TODO 3 · Swap the in-memory store for pgvector ─────────────────────────
// a) Replace the line below with a PostgresVectorStore (Build 4, EDIT 1). Read the connection string
//    from an environment variable, with your local Docker one as the fallback:
//      Environment.GetEnvironmentVariable("PG_CONNECTION") ?? "Host=localhost;Username=postgres;Password=postgres"
// b) In the Note class at the bottom, make the score a similarity, not a distance (Build 4, EDIT 2).
var store = new InMemoryVectorStore(new() { EmbeddingGenerator = embedder });

var collection = store.GetCollection<int, Note>("notes");
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(notes);
Console.WriteLine($"Stored {notes.Count} notes in '{collection.Name}'");

// ── TODO 4 · Write your 3 searches ─────────────────────────────────────────
// At least one should use NONE of the words in the note you hope to find.
string[] searches = ["how often can I call the API?"];

foreach (var search in searches)
{
    Console.WriteLine($"\n🔎 {search}");
    await foreach (var result in collection.SearchAsync(search, top: 3))
        Console.WriteLine($"  {result.Score:F2}  [{result.Record.Topic}] {result.Record.Text}");
}

// ── TODO 5 · Fill in SEARCHES.md ───────────────────────────────────────────
// Paste the psql proof, your 3 searches with the top hit and score, and your verdicts.
//   docker exec -it pgvector psql -U postgres -c "SELECT \"Id\", \"Topic\", left(\"Text\", 40) FROM notes;"

// ── STRETCH (pick one or more) ─────────────────────────────────────────────
// S1. Filter by topic (Build 3, EDIT 3): new VectorSearchOptions<Note> { Filter = n => n.Topic == "..." }
// S2. "No good match": skip results below a minimum score you chose from your own runs (Build 2, EDIT 3).
//     Try a search that has nothing to do with your notes, and check it says so.
// S3. Second model: switch the embedder to Gemini (gemini-embedding-001, 3072 numbers) with a NEW collection
//     ("notes_gemini") and a new size in the attribute (Build 5). Compare the scores for your 3 searches.
// S4. Look inside: docker exec -it pgvector psql -U postgres -c "\d notes"  What type is the Embedding column?
// S5. Head start on Session 6: add GET /search?q=… to your Session 3/4 MyAiApi that returns the top 3 notes as JSON.

// One note = one row in the "notes" table
class Note
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Topic { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";

    // 384 = how many numbers all-minilm returns. The store embeds Text for us.
    [VectorStoreVector(384)] public string Embedding => Text;
}
