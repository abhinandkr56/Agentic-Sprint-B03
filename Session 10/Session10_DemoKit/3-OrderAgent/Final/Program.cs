// Build 3 · OrderAgent (FINAL): tonight's assignment, live. Read a ticket, look the facts up, draft a reply.
//   dotnet run      (GROQ_API_KEY)      Paste a ticket from chat; an empty line quits.
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client. No UseFunctionInvocation() tonight: the agent adds the tool loop itself (Build 1).
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

const string Rules = """
    You draft replies to customer support tickets for our SaaS product.
    Look up the facts with your tools before you write. Never invent dates, amounts or refund status.
    If you can't find the order, ask for the order number or the email they ordered with.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """;

// EDIT 1: hand the agent a tool. Session 7's GetOrder, unchanged: the agent runs the loop for us.
List<AITool> tools = [AIFunctionFactory.Create(GetOrder, "GetOrder")];
// EDIT 2: a second tool. Email → orders → order details: the answer to step 1 is the input to step 2.
tools.Add(AIFunctionFactory.Create(FindOrders, "FindOrders"));
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: tools);   // EDIT 1

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    var response = await agent.RunAsync(ticket);

    // Every step the agent took, in order (Session 7: response.Messages holds the hidden conversation)
    var step = 0;
    foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
        Console.WriteLine($"  step {++step}: {call.Name}({string.Join(", ", call.Arguments?.Values ?? [])})");
    Console.WriteLine($"\n{response.Text}\n  [{response.Usage?.InputTokenCount:N0} tokens in, all steps]");
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

// New tonight: find a customer's orders from the email they wrote from. In real life: your CRM or billing system.
[Description("Finds a customer's orders from their email address. Use it when the customer gives an email but no order number.")]
static string FindOrders([Description("The customer's email address")] string email)
{
    Console.WriteLine($"  [tool] FindOrders({email})");
    return $"Orders for {email}: 4411 (Pro plan, annual, cancelled on 2 Oct); 4413 (SMS pack add-on, delivered on 3 Oct).";
}
