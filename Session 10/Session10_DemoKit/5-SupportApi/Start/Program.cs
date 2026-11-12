// Build 5 · SupportApi (START): the assignment, live. Session 3's Minimal API, with one endpoint that's an agent.
//   dotnet run   →   POST http://localhost:5080/reply      (use SupportApi.http)
using System.ClientModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Sessions 3–4: the provider comes from config. Groq by default; --AI:Provider=Gemini to swap.
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "not-needed";

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient();

const string Rules = """
    You draft replies to customer support tickets for our SaaS product.
    Look up the facts with your tools before you write. Never invent dates, amounts or refund status.
    If you can't find the order, ask for the order number or the email they ordered with.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """;

// Build 3's tools
List<AITool> tools = [AIFunctionFactory.Create(FindOrders, "FindOrders"), AIFunctionFactory.Create(GetOrder, "GetOrder")];

// ▼ EDIT 1: one agent for the whole app
// ▼ EDIT 3: one session per conversation

var app = builder.Build();

app.MapGet("/", () => new { provider, model = ai["Model"], tools = tools.Select(t => t.Name) });

// POST /reply  { "ticket": "...", "conversationId": "T-1001" }  →  a draft reply
app.MapPost("/reply", async (TicketRequest req) =>
{
    // ▼ EDIT 3: find or start this conversation's session
    await Task.CompletedTask;                                             // ▼ EDIT 1: let the agent do the job
    return new { draft = "(no agent yet)", provider };
});

app.Run();

// New tonight: find a customer's orders from the email they wrote from. In real life: your CRM or billing system.
[Description("Finds a customer's orders from their email address. Use it when the customer gives an email but no order number.")]
static string FindOrders([Description("The customer's email address")] string email)
{
    Console.WriteLine($"  [tool] FindOrders({email})");
    return $"Orders for {email}: 4411 (Pro plan, annual, cancelled on 2 Oct); 4413 (SMS pack add-on, delivered on 3 Oct).";
}

// Session 7's tool, unchanged: a stub order system
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

record TicketRequest(string Ticket, string? ConversationId = null);
