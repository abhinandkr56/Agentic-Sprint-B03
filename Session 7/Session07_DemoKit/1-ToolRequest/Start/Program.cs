// Build 1 · ToolRequest (START: open this before the call)
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

// ▼ EDIT 1: describe GetOrder to the model as a tool

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    List<ChatMessage> messages = [new(ChatRole.User, question)];
    var response = await chat.GetResponseAsync(messages);   // ▼ EDIT 1: pass the options
    Console.WriteLine($"{response.Text}\n  [finish: {response.FinishReason} · {response.Usage?.InputTokenCount} tokens in]");

    // ▼ EDIT 1: what did the model ask for?
    // ▼ EDIT 2: run it ourselves, send the result back, ask again
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
