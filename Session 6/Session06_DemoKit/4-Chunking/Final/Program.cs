// Build 4 · Chunking (FINAL: where you end up after the edits)
// Real knowledge isn't ten one-line FAQs. It's documents. Tonight: our refund policy, policy.md (~420 tokens, 11 sections).
// Back on all-minilm (local): it reads at most 256 tokens of any text you give it.
using System.ClientModel;
using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient();

// Build 3's rules
const string Rules = """
    You answer customer questions for our help centre.
    Use ONLY the FAQs you are given. If they don't contain the answer, reply exactly:
    "I don't know. Please contact support."
    Cite the FAQ number, like [10]. Reply in the language of the question.
    """;

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

// INGEST a document as well
var policy = File.ReadAllText("policy.md");
// EDIT 1: one record per "## " section, not one record for the whole file.
// The heading stays with its paragraph, so every chunk still says what it's about.
var chunks = policy.Split("## ", options:StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
faqs.AddRange(chunks.Select((chunk, i) => new Faq { Id = 100 + i, Category = "Policy", Text = chunk }));
// EDIT 2: always look at your chunks
foreach (var chunk in chunks)
    Console.WriteLine($"  {chunk.Length,4} chars · {chunk.Split('\n')[0]}");

var store = new InMemoryVectorStore(new() { EmbeddingGenerator = embedder });
var collection = store.GetCollection<int, Faq>("faqs");
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(faqs);
Console.WriteLine($"Ingested {faqs.Count} records");

// Build 3's gate
var gate = new VectorSearchOptions<Faq> { ScoreThreshold = 0.3 };

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    var hits = await collection.SearchAsync(question, top: 3, gate).ToListAsync();
    foreach (var hit in hits)
        Console.WriteLine($"  {hit.Score:F2}  [{hit.Record.Id}] {hit.Record.Text.Split('\n')[0]}");   // first line only: a chunk can be long
    if (hits.Count == 0)
    {
        Console.WriteLine("\n(no good match: not calling the model)");
        continue;
    }

    var context = string.Join("\n", hits.Select(h => $"[{h.Record.Id}] {h.Record.Text}"));
    List<ChatMessage> messages =
    [
        new(ChatRole.System, Rules),
        new(ChatRole.User, $"FAQs:\n{context}\n\nQuestion: {question}"),
    ];
    var response = await chat.GetResponseAsync(messages);
    Console.WriteLine($"\n{response.Text}\n  [{response.Usage?.InputTokenCount} tokens in]");
}

// Session 5's record: one FAQ = one row. Similarity, not distance (Session 5, Build 4).
class Faq
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Category { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}
