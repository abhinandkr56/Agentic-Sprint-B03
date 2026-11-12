// Build 4 · Postgres (FINAL: where you end up after the edits)
// Same code, a real database: Postgres with the pgvector extension, running in Docker.
// Before class (or at minute 0):
//     docker run -d --name pgvector -p 5432:5432 -e POSTGRES_PASSWORD=postgres pgvector/pgvector:pg18
using System.ClientModel;
using CommunityToolkit.VectorData.InMemory;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

// Build 1's embedder
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();

// Build 2's FAQs, now as records with an Id and a Category
List<Faq> faqs =
[
    new() { Id = 1,  Category = "Billing",      Text = "If a UPI payment fails but money is deducted, the bank reverses it automatically within 3-5 working days." },
    new() { Id = 2,  Category = "Billing",      Text = "GST invoices are emailed on the 1st of every month and can be downloaded from Settings > Billing." },
    new() { Id = 3,  Category = "Billing",      Text = "We accept UPI, credit and debit cards, and net banking. Cheques are not accepted." },
    new() { Id = 4,  Category = "Account",      Text = "To reset your password, click 'Forgot password' on the login page. The link expires in 15 minutes." },
    new() { Id = 5,  Category = "Account",      Text = "Invite teammates from Settings > Team. The Starter plan allows up to 5 users; Pro has no limit." },
    new() { Id = 6,  Category = "HowTo",        Text = "Export your invoices to Excel from Reports > Export, then choose .xlsx." },
    new() { Id = 7,  Category = "HowTo",        Text = "Turn on dark mode under Profile > Appearance." },
    new() { Id = 8,  Category = "HowTo",        Text = "The mobile app works offline and syncs your changes when you are back online." },
    new() { Id = 9,  Category = "Cancellation", Text = "You can cancel your subscription anytime. You keep access until the end of the billing cycle." },
    new() { Id = 10, Category = "Cancellation", Text = "Refunds for annual plans are pro-rated and reach your account within 7 working days." },
];

// EDIT 1: swap the store. One line. (Local Docker password only; real connection strings go in env vars or user-secrets.)
// EnsureCollectionExistsAsync creates the pgvector extension and a "faqs" table the first time.
var store = new PostgresVectorStore("Host=localhost;Username=postgres;Password=postgres", new() { EmbeddingGenerator = embedder });
var collection = store.GetCollection<int, Faq>("faqs");
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(faqs);
Console.WriteLine($"Stored {faqs.Count} FAQs in '{collection.Name}'");

while (true)
{
    Console.Write("\nSearch> ");
    var query = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(query)) break;

    await foreach (var result in collection.SearchAsync(query, top: 3))
        Console.WriteLine($"  {result.Score:F2}  [{result.Record.Category}] {result.Record.Text}");
}

// One record = one row. The attributes tell the store which property is which.
class Faq
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Category { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";

    // 384 = how many numbers all-minilm returns. The store embeds Text for us, using the store's embedder.
    // EDIT 2: say what the score should mean. Postgres defaults to cosine DISTANCE (0 = identical, lower = better);
    // the in-memory store defaulted to cosine SIMILARITY (1 = identical, higher = better). Same ranking, opposite numbers.
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}

// EDIT 3 is not code. It's just a table: look at it from a second terminal
//     docker exec -it pgvector psql -U postgres -c "\d faqs"
//     docker exec -it pgvector psql -U postgres -c "SELECT \"Id\", \"Category\", left(\"Text\", 40) FROM faqs;"
