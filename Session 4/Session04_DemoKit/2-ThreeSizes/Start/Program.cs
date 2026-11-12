// Build 2 · ThreeSizes (START: open this before the call)
// One family (Meta's Llama), three sizes: 70B and 8B in Groq's data centre, 1B on this laptop.
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;

Console.Write("Question> ");
var question = Console.ReadLine()!;

var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")!;

await Ask("☁ Groq · 70B", "https://api.groq.com/openai/v1", groqKey, "llama-3.3-70b-versatile");
// ▼ EDIT 2: the middle size, 8B on Groq (one line, goes here)
// ▼ EDIT 1: the smallest, 1B on this laptop (one line)

// Session 3's Ask(), with one change: it takes the key itself, because Ollama has none
async Task Ask(string name, string endpoint, string key, string model)
{
    IChatClient chat = new OpenAIClient(
            new ApiKeyCredential(key),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
        .GetChatClient(model)
        .AsIChatClient();

    var timer = Stopwatch.StartNew();
    var response = await chat.GetResponseAsync(question);

    Console.WriteLine($"\n── {name} · {response.ModelId} · {timer.ElapsedMilliseconds} ms · {response.Usage?.OutputTokenCount} tokens out");
    Console.WriteLine(response.Text);
}
