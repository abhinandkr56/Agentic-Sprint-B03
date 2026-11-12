// Build 2 · RagLoop (START: open this before the call)
// Session 5's FAQ store + Session 3's chat client. Tonight they meet:
// RETRIEVE the closest FAQs, AUGMENT the prompt with them, GENERATE the answer.
using System.ClientModel;
using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

// Session 5's embedder: all-minilm on Ollama (384 numbers per text)
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();

// Session 3's chat client: Llama 3.3 70B on Groq
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient();

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

// INGEST (Session 5, Build 3): embed the FAQs once and keep them in a store
var store = new InMemoryVectorStore(new() { EmbeddingGenerator = embedder });
var collection = store.GetCollection<int, Faq>("faqs");
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(faqs);
Console.WriteLine($"Ingested {faqs.Count} FAQs");

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    // ▼ EDIT 1 · RETRIEVE: find the 3 FAQs closest in meaning, and show them
    // ▼ EDIT 2 · AUGMENT + GENERATE: put them in the prompt, ask the model
    // ▼ EDIT 3 · number the FAQs and ask for citations
}

// Session 5's record: one FAQ = one row. Similarity, not distance (Session 5, Build 4).
class Faq
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Category { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}
