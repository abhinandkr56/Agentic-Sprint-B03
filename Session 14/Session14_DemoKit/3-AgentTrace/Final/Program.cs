// Build 3 · AgentTrace (FINAL): Session 10's agent. One question in, one answer out. What happened in between?
//   Dashboard running · dotnet run (GROQ_API_KEY) · a ticket from chat, or Enter
using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Build 2's tracing, to the dashboard: the Agent Framework's spans, and ours
using var tracing = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SupportAgent"))
    .AddSource("Experimental.Microsoft.Agents.AI", "SupportDesk")
    .AddOtlpExporter()
    .Build();
var desk = new ActivitySource("SupportDesk");

// Session 3's chat client
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// Session 10's agent, read-only: one tool
const string Rules = """
    You answer customers of our SaaS product. Look up the order with GetOrder before you answer.
    Never invent dates, amounts or refund status. If there's no order number, ask for it.
    Reply in 1-2 sentences and sign as "Asha, Support".
    """;
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: [AIFunctionFactory.Create(GetOrder, "GetOrder")]);
agent = agent.AsBuilder().UseOpenTelemetry().Build();   // EDIT 1: invoke_agent → chat → execute_tool → chat, with tokens

Console.Write("Ticket> ");
var ticket = Console.ReadLine() is { Length: > 0 } typed ? typed : "Where is my refund for order 4411?";
var response = await agent.RunAsync(ticket);
Console.WriteLine($"\n{response.Text}");

// Session 7's tool: a stub order system (with a real database's speed). It only READS.
[Description("Gets an order's status, amount and refund details. Only call it when the customer gives an order number; otherwise ask for it.")]
string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.WriteLine($"  [tool] GetOrder({orderId})");
    using var span = desk.StartActivity("orders DB query");   // EDIT 3: our span, a child of the framework's execute_tool
    span?.SetTag("order.id", orderId);
    Thread.Sleep(150);
    if (orderId > 9000) throw new TimeoutException("orders database timed out after 2000 ms");   // EDIT 2: a hidden failure
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}
