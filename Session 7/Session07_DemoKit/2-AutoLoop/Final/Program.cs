// Build 2 · AutoLoop (FINAL, was START: Build 1's tool, without Build 1's loop)
// Build 1 ran the tool loop by hand. Microsoft.Extensions.AI can run it for us.
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Extensions.AI;
using OpenAI;

// EDIT 1: let M.E.AI run the tool loop. FunctionInvokingChatClient does Build 1's EDIT 2 for us, as often as needed.
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var getOrder = AIFunctionFactory.Create(GetOrder, "GetOrder");
var options = new ChatOptions { Tools = [getOrder] };

// EDIT 2: this is ALL the model knows about our C# method: a name, a description and a JSON schema
Console.WriteLine($"Tool: {getOrder.Name} · \"{getOrder.Description}\"\n  {getOrder.JsonSchema}");

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    var response = await chat.GetResponseAsync(question, options);
    Console.WriteLine(response.Text.Length > 0 ? response.Text : "(no text: the model asked for a tool, and nobody ran it)");
    Console.WriteLine($"  [{response.Usage?.InputTokenCount} tokens in]");
}

// EDIT 3: describe the tool: what it does, when to use it, what the parameter means
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
