// Build 5 · ToolsApi (FINAL, was START: Session 6's RagApi, plus tools waiting to be plugged in)
// Session 4's config + Session 3's middleware + Session 6's pgvector FAQs. New: GET /chat, where the model picks the tools.
using System.ClientModel;
using System.ComponentModel;
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

// Session 3's middleware, made tool-safe: it only masks USER messages now. (Rebuilding every message from its
// text, like in Session 3, would silently delete tool calls and tool results.) It prints every round trip.
// EDIT 1: turn on the tool loop here, BEFORE the middleware, so the middleware sees every round trip
builder.Services.AddChatClient(client)
    .UseFunctionInvocation()
    .Use(async (messages, options, next, ct) =>
    {
        var masked = messages.Select(m => m.Role == ChatRole.User ? new ChatMessage(m.Role, Regex.Replace(m.Text, @"\S+@\S+", "[email]")) : m).ToList();
        var last = masked[^1];
        var shown = last.Text.Length > 0 ? last.Text : string.Join(" | ", last.Contents.OfType<FunctionResultContent>().Select(r => r.Result));
        Console.WriteLine($"→ model sees ({last.Role}): {shown}");
        var timer = Stopwatch.StartNew();
        await next(masked, options, ct);
        Console.WriteLine($"⏱ {provider}: {timer.ElapsedMilliseconds} ms");
    });

// Session 6: the FAQs in pgvector, embedded by all-minilm on Ollama. Connection string from config, local Docker as fallback.
IEmbeddingGenerator<string, Embedding<float>> embedder = new OpenAIClient(
        new ApiKeyCredential("ollama"),
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetEmbeddingClient("all-minilm")
    .AsIEmbeddingGenerator();
var connection = builder.Configuration.GetConnectionString("Vectors") ?? "Host=localhost;Username=postgres;Password=postgres";
VectorStoreCollection<int, Faq> collection = new PostgresVectorStore(connection, new() { EmbeddingGenerator = embedder }).GetCollection<int, Faq>("faqs");
var gate = new VectorSearchOptions<Faq> { ScoreThreshold = 0.3 };

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

const string Rules = """
    You are the help-centre assistant for our SaaS product.
    Use GetOrder for questions about a specific order and SearchFaqs for policies and how-to questions.
    Answer ONLY from what the tools return. Cite FAQs like [10]. If the tools don't have the answer, reply:
    "I don't know. Please contact support."
    """;

// Build 3's tools (the methods are at the bottom of this file)
// EDIT 2: + CancelOrder        EDIT 3: ...wrapped so it needs approval. The loop stops and asks instead of running it.
List<AITool> tools = [AIFunctionFactory.Create(GetOrder, "GetOrder"), AIFunctionFactory.Create(SearchFaqs, "SearchFaqs"),
    new ApprovalRequiredAIFunction(AIFunctionFactory.Create(CancelOrder, "CancelOrder"))];

var app = builder.Build();

await collection.EnsureCollectionExistsAsync();
await collection.UpsertAsync(seed);
Console.WriteLine($"Ingested {seed.Count} FAQs into pgvector");

app.MapGet("/", () => new { provider, model = ai["Model"], tools = tools.Select(t => t.Name) });

// Open in a browser:  http://localhost:5080/chat?q=where is my refund for order 4411
// EDIT 1: hand /chat the tools
app.MapGet("/chat", async (string q, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Rules), new(ChatRole.User, q)];
    var response = await chat.GetResponseAsync(messages, new ChatOptions { Tools = tools });
    return new
    {
        answer = response.Text,
        provider,
        toolCalls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
            .Select(c => $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})"),
        // EDIT 3: anything waiting for a human? (Session 12 sends the yes/no back. Tonight we only look.)
        needsApproval = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>()
            .Select(a => a.ToolCall is FunctionCallContent c ? $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})" : "?"),
    };
});

app.Run();

// ── The tools: plain C# methods ──
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

[Description("Searches the help-centre FAQs: billing, payments, refunds, invoices, accounts and how-to. Returns numbered FAQs.")]
async Task<string> SearchFaqs([Description("What to look for, in a few words")] string query)
{
    var hits = await collection.SearchAsync(query, top: 3, gate).ToListAsync();
    Console.WriteLine($"  [tool] SearchFaqs(\"{query}\") -> {hits.Count} hit(s)");
    return hits.Count == 0 ? "No matching FAQ." : string.Join("\n", hits.Select(h => $"[{h.Record.Id}] {h.Record.Text}"));
}

// EDIT 2: a tool that DOES something. Reading is safe; this one changes the world.
[Description("Cancels an order.")]
static string CancelOrder([Description("The order number")] int orderId)
{
    Console.WriteLine($"  [tool] !!! CancelOrder({orderId}): order cancelled !!!");
    return $"Order {orderId} has been cancelled.";
}

// Session 5's record: one FAQ = one row. Similarity, not distance (Session 5, Build 4).
class Faq
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Category { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}
