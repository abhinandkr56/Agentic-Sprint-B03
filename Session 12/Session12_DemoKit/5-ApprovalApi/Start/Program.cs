// Build 5 · ApprovalApi (START): Session 11's ReviewedReplyApi. Reviewer approved = sent to the customer.
// Tonight a person decides.
//   dotnet run   →   POST http://localhost:5080/reply      (use ApprovalApi.http)
using System.ClientModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;
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

// Session 10's writer: tools first, never invent
const string Rules = """
    You draft replies to customer support tickets for our SaaS product.
    Look up the facts with your tools before you write. Never invent dates, amounts or refund status.
    If you can't find the order, ask for the order number or the email they ordered with.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """;

// Session 11's reviewer: a checklist
const string ReviewRules = """
    You review draft replies to customer support tickets before a customer sees them.
    You get the TICKET, the FACTS our systems returned, and the DRAFT.
    Check: 1) every date, time frame, amount and refund status in the draft appears in FACTS;
    2) it promises nothing FACTS doesn't back up; 3) the tone is warm and never blames the customer;
    4) it is signed "Asha, Support".
    List each problem as one short sentence.
    """;

// Session 11: both agents created once, shared by every request
List<AITool> tools = [AIFunctionFactory.Create(FindOrders, "FindOrders"), AIFunctionFactory.Create(GetOrder, "GetOrder")];
AIAgent writer = client.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: tools);
AIAgent reviewer = client.AsAIAgent(name: "ReviewerAgent", instructions: ReviewRules);

var app = builder.Build();
// ▼ EDIT 1: replies waiting for a person

app.MapGet("/", () => new { provider, model = ai["Model"], tools = tools.Select(t => t.Name) });

// POST /reply  { "ticket": "..." }  →  Session 11: draft → evidence → review
app.MapPost("/reply", async (TicketRequest req) =>
{
    AgentSession session = await writer.CreateSessionAsync();
    var draft = await writer.RunAsync(req.Ticket, session);
    var steps = draft.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>()
        .Select(c => $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})").ToList();

    var facts = string.Join("\n", draft.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Select(r => $"{r.Result}"));
    var evidence = $"TICKET:\n{req.Ticket}\n\nFACTS:\n{(facts.Length > 0 ? facts : "(none: no tool was called)")}";
    var review = await reviewer.RunAsync<Review>($"{evidence}\n\nDRAFT:\n{draft.Text}");
    var to = Regex.Match(req.Ticket, @"[\w.+-]+@[\w-]+\.[\w.]+").Value is { Length: > 0 } email ? email : "the order's account email";

    // The final action. Reviewer approved = sent.                                        ▼ EDIT 1: park it for a person
    if (review.Result.Approved) SendReply(to, draft.Text);
    return new { draft = draft.Text, approved = review.Result.Approved, problems = review.Result.Problems, sent = review.Result.Approved, steps, provider };
});
// ▼ EDIT 2: a person approves or rejects

app.Run();

// Session 10's second tool: find a customer's orders from the email they wrote from
[Description("Finds a customer's orders from their email address. Use it when the customer gives an email but no order number.")]
static string FindOrders([Description("The customer's email address")] string email)
{
    Console.WriteLine($"  [tool] FindOrders({email})");
    return $"Orders for {email}: 4411 (Pro plan, annual, cancelled on 2 Oct); 4413 (SMS pack add-on, delivered on 3 Oct).";
}

// Session 7's tool, unchanged: a stub order system. It only READS.
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

// The final action: an email to a real customer. It can't be unsent. A stub: in real life, SendGrid, SES or your CRM.
static void SendReply(string to, string body) => Console.WriteLine($"\n  >>> EMAIL SENT to {to} <<<\n  {body.Split('\n')[0]}");

record TicketRequest(string Ticket);
record Review(bool Approved, List<string> Problems);
// ▼ EDIT 1: what a waiting reply keeps
