// ─────────────────────────────────────────────────────────────────────────────
// Session 05 assignment · SOLUTION (TODO 1–5 done, plus stretch S1 filter and S2 "no good match")
//
//   docker run -d --name pgvector -p 5432:5432 -e POSTGRES_PASSWORD=postgres pgvector/pgvector:pg18
//   ollama pull all-minilm
//   dotnet run
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using CommunityToolkit.VectorData.InMemory;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();

// TODO 2 · 10 short texts: one team's engineering handbook, 4 topics
List<Note> notes =
[
    new() { Id = 1,  Topic = "API",         Text = "Our API returns HTTP 429 when you send more than 100 requests per minute." },
    new() { Id = 2,  Topic = "API",         Text = "Every request needs an X-Api-Key header; keys are created in the developer portal." },
    new() { Id = 3,  Topic = "API",         Text = "Version 1 of the API is switched off on 31 March; move to /v2 before then." },
    new() { Id = 4,  Topic = "Deployments", Text = "Deployments to production happen every Tuesday and Thursday after 6 pm IST." },
    new() { Id = 5,  Topic = "Deployments", Text = "A pull request needs two approvals and a green build before it can be merged." },
    new() { Id = 6,  Topic = "Deployments", Text = "Roll back a release from the pipeline page by re-running the previous successful deploy." },
    new() { Id = 7,  Topic = "Onboarding",  Text = "New joiners get a laptop on day one; ask IT on the #help-it channel for VPN access." },
    new() { Id = 8,  Topic = "Onboarding",  Text = "Run 'dotnet test' before pushing; the build fails if code coverage drops below 80%." },
    new() { Id = 9,  Topic = "Incidents",   Text = "If the site is down, page the on-call engineer through PagerDuty, then post in #incidents." },
    new() { Id = 10, Topic = "Incidents",   Text = "Write a blameless postmortem within 48 hours of every Sev-1 incident." },
];

// TODO 3a · pgvector instead of memory. The connection string comes from an env var, with local Docker as the fallback.
var connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION") ?? "Host=localhost;Username=postgres;Password=postgres";
var store = new PostgresVectorStore(connectionString, new() { EmbeddingGenerator = embedder });

var collection = store.GetCollection<int, Note>("notes");
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(notes);
Console.WriteLine($"Stored {notes.Count} notes in '{collection.Name}'");

// TODO 4 · 3 searches. The second one shares no words with the note it should find (#9).
string[] searches =
[
    "how often can I call the API?",
    "everything is broken and customers can't log in, who do I tell?",
    "can I ship code on a Friday?",
];

const double MinScore = 0.3;   // STRETCH S2 · picked after looking at my own runs with all-minilm; a different model needs a different number

foreach (var search in searches)
{
    Console.WriteLine($"\n🔎 {search}");
    var found = 0;
    await foreach (var result in collection.SearchAsync(search, top: 3))
    {
        if (result.Score < MinScore) continue;   // STRETCH S2
        Console.WriteLine($"  {result.Score:F2}  [{result.Record.Topic}] {result.Record.Text}");
        found++;
    }
    if (found == 0) Console.WriteLine("  (no good match)");
}

// STRETCH S1 · same search, only one topic: exact rules on the data, similarity on the meaning
Console.WriteLine("\n🔎 what happens on Thursday?   (Topic == \"Deployments\" only)");
var deploymentsOnly = new VectorSearchOptions<Note> { Filter = n => n.Topic == "Deployments" };
await foreach (var result in collection.SearchAsync("what happens on Thursday?", top: 2, deploymentsOnly))
    Console.WriteLine($"  {result.Score:F2}  [{result.Record.Topic}] {result.Record.Text}");

// STRETCH S2 · a search that has nothing to do with these notes
Console.WriteLine("\n🔎 best biryani in Hyderabad");
await foreach (var result in collection.SearchAsync("best biryani in Hyderabad", top: 1))
    Console.WriteLine(result.Score >= MinScore ? $"  {result.Score:F2}  {result.Record.Text}" : $"  (no good match: best was {result.Score:F2})");

// TODO 5 · SEARCHES.md is filled in next to this file

// One note = one row in the "notes" table
class Note
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Topic { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";

    // TODO 3b · Postgres defaults to cosine DISTANCE (lower = closer). Ask for SIMILARITY so higher = closer, like in memory.
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}
