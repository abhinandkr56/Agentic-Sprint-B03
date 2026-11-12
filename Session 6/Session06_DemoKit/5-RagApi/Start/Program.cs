// Build 5 · RagApi (START: open this before the call)
// Session 4's AskApi (provider from config, middleware) + Session 5's FAQs in pgvector.
// Everything is wired up. Only /ask doesn't use the FAQs yet.
using System.ClientModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.VectorData.PgVector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Sessions 3–4: the chat provider comes from config. Groq by default; --AI:Provider=Gemini to swap.
var provider = builder.Configuration["AI:Provider"]!;
var ai = builder.Configuration.GetSection($"AI:{provider}");
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "not-needed";

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient();

// Session 3's middleware: mask e-mails, show what the model sees, time it
builder.Services.AddChatClient(client)
    .Use(async (messages, options, next, ct) =>
    {
        var masked = messages.Select(m => new ChatMessage(m.Role, Regex.Replace(m.Text, @"\S+@\S+", "[email]"))).ToList();
        Console.WriteLine($"→ model sees:\n{masked[^1].Text}");
        var timer = Stopwatch.StartNew();
        await next(masked, options, ct);
        Console.WriteLine($"⏱ {provider}: {timer.ElapsedMilliseconds} ms");
    });

// Session 5: the FAQs in pgvector, embedded by all-minilm on Ollama.
// Connection string from config (env var ConnectionStrings__Vectors or user-secrets); the fallback is the local Docker one.
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();
var connection = builder.Configuration.GetConnectionString("Vectors") ?? "Host=localhost;Username=postgres;Password=postgres";
// Typed as the abstraction on purpose: that's the type /ask asks DI for.
VectorStoreCollection<int, Faq> collection = new PostgresVectorStore(connection, new() { EmbeddingGenerator = embedder }).GetCollection<int, Faq>("faqs");
builder.Services.AddSingleton(collection);

// Session 5's ten FAQs: what we ingest at startup
List<Faq> seed =
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

// Build 3's rules and gate
const string Rules = """
    You answer customer questions for our help centre.
    Use ONLY the FAQs you are given. If they don't contain the answer, reply exactly:
    "I don't know. Please contact support."
    Cite the FAQ number, like [10]. Reply in the language of the question.
    """;
var gate = new VectorSearchOptions<Faq> { ScoreThreshold = 0.3 };

var app = builder.Build();

// INGEST at startup. Upsert = insert or update, so restarting is safe.
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(seed);
Console.WriteLine($"Ingested {seed.Count} FAQs into pgvector");

// What is this instance running?  http://localhost:5080/
app.MapGet("/", () => new { provider, model = ai["Model"], rules = Rules });

// Open in a browser:  http://localhost:5080/ask?q=where is my refund
// ▼ EDIT 1: ground the answer: RETRIEVE (faqs.SearchAsync), AUGMENT (Rules + FAQs), GENERATE
// ▼ EDIT 2: return the answer AND its sources, as JSON
app.MapGet("/ask", async (string q, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(q);
    return $"[{provider} · {response.ModelId}]\n\n{response.Text}";
});

app.Run();

// ▼ EDIT 3 is not code. Same FAQs, two answer models, side by side:
//     Terminal A:  dotnet run                                                   → Groq    http://localhost:5080/ask?q=…
//     Terminal B:  dotnet run --AI:Provider=Gemini --urls http://localhost:5081  → Gemini  http://localhost:5081/ask?q=…

// Session 5's record: one FAQ = one row. Similarity, not distance (Session 5, Build 4).
class Faq
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Category { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}
