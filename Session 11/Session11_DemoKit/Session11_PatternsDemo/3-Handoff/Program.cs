// Pattern 3 · HANDOFF: an agent passes the whole conversation to a specialist, and the specialist takes over.
// Use it for routing: a front desk that sends each customer to the right team.
//   dotnet run      (GROQ_API_KEY)
//
//                      ┌─▶ BillingAgent (has GetOrder)
//   ticket ─▶ TriageAgent
//                      └─▶ TechAgent
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// The DESCRIPTION is what the triage agent reads when it decides who to hand off to
AIAgent triage = chat.AsAIAgent(name: "TriageAgent", description: "The front desk: routes customers to the right specialist",
    instructions: "You are the first contact for customer support. Don't answer questions yourself: hand off to the right specialist.");

AIAgent billing = chat.AsAIAgent(name: "BillingAgent", description: "Refunds, charges, invoices and payments",
    instructions: "You handle billing questions. Look up the order with GetOrder before you answer. Reply in 2-3 sentences and sign as \"Asha, Billing\".",
    tools: [AIFunctionFactory.Create(GetOrder, "GetOrder")]);

AIAgent tech = chat.AsAIAgent(name: "TechAgent", description: "Login problems, errors, bugs and how-to questions",
    instructions: "You handle technical questions. Reply in 2-3 sentences and sign as \"Ravi, Tech Support\".");

// THE PATTERN: who may hand off to whom. Behind the scenes each agent gets a tool named handoff_to_<agent id>.
Workflow workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triage)
    .WithHandoffs(triage, [billing, tech])     // the front desk can send you to either specialist
    .WithHandoffs([billing, tech], triage)     // a specialist can send you back if it's not their area
    .Build();
AIAgent desk = workflow.AsAIAgent(name: "SupportDesk");

string[] tickets =
[
    "I was charged twice for order 4412, I want my money back!",
    "I can't log in since yesterday and the password reset email never arrives.",
];

foreach (var ticket in tickets)
{
    Console.WriteLine($"\n════ Customer: {ticket}");
    var response = await desk.RunAsync(ticket);   // a new run per customer
    foreach (var message in response.Messages.Where(m => !string.IsNullOrWhiteSpace(m.Text)))
        Console.WriteLine($"\n[{message.AuthorName}]\n{message.Text}");
}

// Session 7's tool, unchanged: a stub order system
[Description("Gets an order's status, amount and refund details. Only call it when the customer gives an order number; otherwise ask for it.")]
static string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.WriteLine($"  [tool] GetOrder({orderId})");
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active. Payment log: one charge of Rs 499 on 1 Oct.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}
