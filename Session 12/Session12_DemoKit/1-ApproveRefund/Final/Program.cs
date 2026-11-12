// Build 1 · ApproveRefund (FINAL): the agent can look up an order AND refund it. Who said yes?
//   dotnet run      (GROQ_API_KEY)      Paste a ticket from chat; an empty line quits.
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client. One client, many agents: each agent adds its own instructions on top.
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// Session 10's agent, with its write tool switched ON (Session 10's middleware blocked it)
const string Rules = """
    You handle refund requests for our SaaS product.
    Look up the order with GetOrder first. If the customer asks for a refund, issue it with IssueRefund.
    Reply in 2-3 sentences and sign as "Asha, Support".
    """;

List<AITool> tools =
[
    AIFunctionFactory.Create(GetOrder, "GetOrder"),         // reads: runs straight away
    new ApprovalRequiredAIFunction(AIFunctionFactory.Create(IssueRefund, "IssueRefund")),   // EDIT 1: writes wait for a human
];
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: tools);

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    AgentSession session = await agent.CreateSessionAsync();   // one per ticket: it also holds anything waiting for a human
    var response = await agent.RunAsync(ticket, session);

    // Did the agent stop to ask a human?
    var requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
    Console.WriteLine($"  [{requests.Count} approval request(s)]");
    // EDIT 1: the run STOPPED here. Nothing was refunded. This is what the agent wants to do:
    foreach (var request in requests)
        if (request.ToolCall is FunctionCallContent call)
            Console.WriteLine($"  waiting for a human: {call.Name}({string.Join(", ", call.Arguments?.Values ?? [])})");
    // EDIT 2: a human answers each request. The answers go back IN THE SAME SESSION, and the run carries on.
    while (requests.Count > 0)
    {
        List<ChatMessage> answers = [];
        foreach (var request in requests)
        {
            Console.Write("  Approve? (y/n) ");
            var yes = Console.ReadLine()?.Trim().ToLower() == "y";
            // EDIT 3: a "no" can carry a reason. The model reads it as the tool's result.
            string? reason = null;
            if (!yes) { Console.Write("  Why not? "); reason = Console.ReadLine(); }
            answers.Add(new ChatMessage(ChatRole.User, [request.CreateResponse(yes, reason)]));
        }
        response = await agent.RunAsync(answers, session);
        requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
    }

    Console.WriteLine($"\n{response.Text}");
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

// Session 10's write tool: it MOVES MONEY. A stub: in real life, your payment provider's refund API.
[Description("Issues a full refund for an order to the customer's original payment method.")]
static string IssueRefund([Description("The order number")] int orderId)
{
    Console.WriteLine($"  [tool] IssueRefund({orderId})  >>> REFUND ISSUED <<<");
    return $"Refund issued for order {orderId}. Reference RF-{orderId}-01.";
}
