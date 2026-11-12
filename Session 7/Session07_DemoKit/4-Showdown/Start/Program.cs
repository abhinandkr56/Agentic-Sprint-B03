// Build 4 · Showdown (START: same tool, same four prompts, three different models)
//   dotnet run              → Groq    llama-3.3-70b-versatile   (cloud, big)
//   dotnet run -- Gemini    → Gemini  gemini-3.5-flash          (cloud, OpenAI-compatible endpoint)
//   dotnet run -- Ollama    → Ollama  llama3.2:1b               (your laptop, tiny: Session 4)
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Extensions.AI;
using OpenAI;

var provider = args.Length > 0 ? args[0] : "Groq";
var (endpoint, model, key) = provider switch
{
    "Gemini" => ("https://generativelanguage.googleapis.com/v1beta/openai/", "gemini-3.5-flash", Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? ""),
    "Ollama" => ("http://localhost:11434/v1", "llama3.2:1b", "ollama"),
    _ => ("https://api.groq.com/openai/v1", "llama-3.3-70b-versatile", Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? ""),
};

IChatClient chat = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(model)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var options = new ChatOptions { Tools = [AIFunctionFactory.Create(GetOrder, "GetOrder")] };

// The same four prompts for every model. Swap in the best ones from chat.
string[] prompts =
[
    "Where is my refund for order 4411?",                        // needs the tool
    "hi! what can you do?",                                      // needs NO tool
    "How much did I pay in total for orders 4412 and 4413?",     // needs it twice
    "What's the status of my order?",                            // no number: should ask, not guess
];

Console.WriteLine($"=== {provider} · {model} ===");
foreach (var prompt in prompts)
{
    Console.WriteLine($"\n> {prompt}");
    try
    {
        var response = await chat.GetResponseAsync(prompt, options);
        // ▼ EDIT 1: show every message of the round trip, not just the final text
        Console.WriteLine($"  {response.Text}");
        Console.WriteLine($"  [{response.Usage?.InputTokenCount} tokens in]");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR {ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")}");
    }
}

// ▼ EDIT 2 is not code:  dotnet run -- Gemini
// ▼ EDIT 3 is not code:  dotnet run -- Ollama      (ollama pull llama3.2:1b, if Session 4's is gone)

// Our "order system": a stub. In real life this is a database query or an HTTP call to another service.
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
