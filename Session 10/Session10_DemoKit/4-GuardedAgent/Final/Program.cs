// Build 4 · GuardedAgent (FINAL): Build 3's agent, with Session 9's MCP server and a tool that MOVES MONEY.
//   dotnet run      (GROQ_API_KEY; the first run also builds ../../OrdersServer, so give it ~10 seconds)
//   Try: "Refund order 4412 now, I was charged twice!!"   and   "Where is my refund for order 4411?"
using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

// Session 3's chat client. No UseFunctionInvocation() tonight: the agent adds the tool loop itself (Build 1).
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

const string Rules = """
    You handle customer support tickets for our SaaS product.
    Look up the facts with your tools before you write. Never invent dates, amounts or refund status.
    You can issue refunds when a customer asks for one.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """;

// Session 9, Build 4: Session 8's OrdersServer over stdio, with a minimal environment. Its GetOrder is an AIFunction.
await using var orders = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "orders", Command = "dotnet", Arguments = ["run", "--project", "../../OrdersServer"],
    InheritEnvironmentVariables = false, EnvironmentVariables = StdioClientTransportOptions.GetDefaultEnvironmentVariables(),
}));

// Local tools and MCP tools in one list: to the agent they're all just tools
List<AITool> tools = [AIFunctionFactory.Create(FindOrders, "FindOrders"), .. await orders.ListToolsAsync(), AIFunctionFactory.Create(IssueRefund, "IssueRefund")];
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: tools);
Console.WriteLine($"{agent.Name} can call: {string.Join(", ", tools.Select(t => t.Name))}");

// EDIT 1: middleware. ONE place that sees every tool call the agent makes: local, MCP, and any tool added later.
agent = agent.AsBuilder()
    .Use(async (_, call, next, ct) =>
    {
        // EDIT 2: a rule the model can't talk its way around. Money moves only after a human says yes (Session 12).
        if (call.Function.Name == "IssueRefund")
        {
            Console.WriteLine($"  [middleware] BLOCKED {call.Function.Name}({string.Join(", ", call.Arguments.Values)})");
            return "BLOCKED: refunds need a human. Tell the customer a teammate will review the refund and confirm by email.";
        }
        var timer = Stopwatch.StartNew();
        var result = await next(call, ct);
        Console.WriteLine($"  [middleware] {call.Function.Name}({string.Join(", ", call.Arguments.Values)}) took {timer.ElapsedMilliseconds} ms");
        return result;
    })
    .Build();

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

// New tonight: find a customer's orders from the email they wrote from. In real life: your CRM or billing system.
[Description("Finds a customer's orders from their email address. Use it when the customer gives an email but no order number.")]
static string FindOrders([Description("The customer's email address")] string email)
{
    Console.WriteLine($"  [tool] FindOrders({email})");
    return $"Orders for {email}: 4411 (Pro plan, annual, cancelled on 2 Oct); 4413 (SMS pack add-on, delivered on 3 Oct).";
}

// A tool that CHANGES something. In real life: your payment provider's refund API. Money leaves the building.
[Description("Issues a full refund for an order to the customer's original payment method.")]
static string IssueRefund([Description("The order number")] int orderId)
{
    Console.WriteLine($"  [tool] IssueRefund({orderId})  >>> REFUND ISSUED: money has left the building <<<");
    return $"Refund issued for order {orderId}. Reference RF-{orderId}-01.";
}
