// Build 1 · ToolRequest (FINAL: where you end up after the edits)
// The model can't see our orders. Tonight we hand it a C# method... and find out what "calling a function" really means.
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client: Llama 3.3 70B on Groq
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient();

// EDIT 1: describe GetOrder to the model as a tool. (We name it: local functions get mangled names like _Main_g_GetOrder_0_0.)
var getOrder = AIFunctionFactory.Create(GetOrder, "GetOrder");
var options = new ChatOptions { Tools = [getOrder] };

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    List<ChatMessage> messages = [new(ChatRole.User, question)];
    var response = await chat.GetResponseAsync(messages, options);   // EDIT 1: pass the options
    Console.WriteLine($"{response.Text}\n  [finish: {response.FinishReason} · {response.Usage?.InputTokenCount} tokens in]");

    // EDIT 1: what did the model ask for? (Notice: nothing printed "[tool] ... is running". Nothing ran.)
    var calls = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ToList();
    foreach (var call in calls)
        Console.WriteLine($"  model asks: {call.Name}({string.Join(", ", call.Arguments!)})");
    // EDIT 2: run it ourselves, send the result back, ask again. That's the whole "tool loop".
    if (calls.Count == 0) continue;
    messages.AddMessages(response);                                   // 1. the model's request goes into the history
    foreach (var call in calls)
    {
        var result = await getOrder.InvokeAsync(new(call.Arguments));     // 2. WE run the C# method
        messages.Add(new(ChatRole.Tool, [new FunctionResultContent(call.CallId, result)]));   // 3. its result goes back
    }
    var final = await chat.GetResponseAsync(messages, options);          // 4. ask again, with the result
    Console.WriteLine($"\n{final.Text}\n  [finish: {final.FinishReason} · {final.Usage?.InputTokenCount} tokens in]");
}

// Our "order system": a stub. In real life this is a database query or an HTTP call to another service.
[Description("Gets an order's status, amount and refund details by its order number.")]
static string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.WriteLine($"  [tool] GetOrder({orderId}) is running");
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}
