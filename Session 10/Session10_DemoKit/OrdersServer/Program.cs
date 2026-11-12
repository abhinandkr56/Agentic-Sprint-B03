// Session 8 · Build 5 · OrdersServer (FINAL): Session 7's GetOrder, served over MCP, so ANY MCP client can use it.
// Session 9 connected to it from C#; tonight Build 4 and the assignment hand its tools to an AGENT. Unchanged since Session 8.
// You don't run it yourself: the client starts it with   dotnet run --project <this folder>
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);
// stdout belongs to the protocol, so every log line goes to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

var mcp = builder.Services.AddMcpServer().WithStdioServerTransport();

// EDIT 1: serve GetOrder as an MCP tool. Same method as Session 7; we name it (local functions get mangled names).
mcp.WithTools([McpServerTool.Create(GetOrder, new() { Name = "GetOrder" })]);

// EDIT 3: serve the refund policy as an MCP resource: data the APP chooses to attach, not a tool the model calls.
mcp.WithResources([McpServerResource.Create(RefundPolicy, new()
{
    UriTemplate = "policy://refunds", Name = "refund-policy", MimeType = "text/plain",
    Description = "Our cancellation and refund policy.",
})]);

await builder.Build().RunAsync();

// Session 7's tool, unchanged: a stub order system
[Description("Gets an order's status, amount and refund details. Only call it when the customer gives an order number; otherwise ask for it.")]
static string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.Error.WriteLine($"  [tool] GetOrder({orderId})");   // EDIT 2: stdout IS the protocol. Logs go to stderr.
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}

// Session 6's FAQ [9] and [10]: our refund policy, as plain text
static string RefundPolicy() => "You can cancel your subscription anytime. You keep access until the end of the billing cycle. Refunds for annual plans are pro-rated and reach your account within 7 working days.";
