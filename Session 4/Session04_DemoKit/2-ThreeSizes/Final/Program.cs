// Build 2 · ThreeSizes (FINAL: where you end up after the edits)
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;

Console.Write("Question> ");
var question = Console.ReadLine()!;

var groqKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")!;

await Ask("☁ Groq · 70B", "https://api.groq.com/openai/v1", groqKey, "qwen/qwen3.8-27b");
await Ask("☁ Groq · 8B", "https://api.groq.com/openai/v1", groqKey, "openai/gpt-oss-120b");   // EDIT 2
await Ask("💻 Laptop · 1B", "http://localhost:11434/v1", "ollama", "llama3.2:1b");               // EDIT 1

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
