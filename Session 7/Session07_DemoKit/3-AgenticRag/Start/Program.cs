// Build 3 · AgenticRag (START: Session 6's FAQ store + Build 2's GetOrder tool)
// In Session 6 WE searched on every question. Now the search becomes a tool, and the model decides when to use it.
using System.ClientModel;
using System.ComponentModel;
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

// Build 2's chat client, with the tool loop on
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

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

// INGEST (Session 5) + the gate (Session 6, Build 3)
var store = new InMemoryVectorStore(new() { EmbeddingGenerator = embedder });
var collection = store.GetCollection<int, Faq>("faqs");
await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(faqs);
var gate = new VectorSearchOptions<Faq> { ScoreThreshold = 0.3 };
Console.WriteLine($"Ingested {faqs.Count} FAQs");

// ▼ EDIT 1: Session 6's search, as a tool

var options = new ChatOptions { Tools = [AIFunctionFactory.Create(GetOrder, "GetOrder")] };   // ▼ EDIT 1: + SearchFaqs

// ▼ EDIT 2: the rules: use the tools, answer only from what they return, cite

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    List<ChatMessage> messages = [new(ChatRole.User, question)];   // ▼ EDIT 2: rules first
    var response = await chat.GetResponseAsync(messages, options);
    Console.WriteLine($"\n{response.Text}\n  [{response.Usage?.InputTokenCount} tokens in, all round trips]");
}

// Our "order system": a stub. In real life this is a database query or an HTTP call to another service.
[Description("Gets an order's status, amount and refund details. Only call it when the customer gives an order number; otherwise ask for it.")]
static string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.WriteLine($"  [tool] GetOrder({orderId})");
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}

// Session 5's record: one FAQ = one row. Similarity, not distance (Session 5, Build 4).
class Faq
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Category { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}
