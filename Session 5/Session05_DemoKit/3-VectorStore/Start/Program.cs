// Build 3 · VectorStore (START: open this before the call)
// Build 2's loop, handed to a library: Microsoft.Extensions.VectorData.
// One record class, one store, one collection. Tonight's store lives in memory.
using System.ClientModel;
using CommunityToolkit.VectorData.InMemory;
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

// ▼ EDIT 1: a store, a collection, and the 10 records in it

while (true)
{
    Console.Write("\nSearch> ");
    var query = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(query)) break;

    // ▼ EDIT 2: search the collection (the library embeds the query for us)
    // ▼ EDIT 3: only look at Billing FAQs
}

// One record = one row. The attributes tell the store which property is which.
class Faq
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Category { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";

    // 384 = how many numbers all-minilm returns. The store embeds Text for us, using the store's embedder.
    [VectorStoreVector(384)] public string Embedding => Text;
}
