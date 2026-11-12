// Build 2 · TwoProviders (FINAL: where you end up after the edits)
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;

Console.Write("Question> ");
var question = Console.ReadLine()!;

await Ask("Groq", "https://api.groq.com/openai/v1", "GROQ_API_KEY", "llama-3.3-70b-versatile");
await Ask("Gemini", "https://generativelanguage.googleapis.com/v1beta/openai/", "GEMINI_API_KEY", "gemini-3.5-flash"); // EDIT 1
await Ask("Groq", "https://api.groq.com/openai/v1", "GROQ_API_KEY", "openai/gpt-oss-20b");                              // EDIT 2

// Build 1's three lines, with the provider details passed in
async Task Ask(string name, string endpoint, string keyVariable, string model)
{
    IChatClient chat = new OpenAIClient(
            new ApiKeyCredential(Environment.GetEnvironmentVariable(keyVariable)!),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
        .GetChatClient(model)
        .AsIChatClient();

    var timer = Stopwatch.StartNew();
    var response = await chat.GetResponseAsync(question);

    Console.WriteLine($"\n── {name} · {response.ModelId} · {timer.ElapsedMilliseconds} ms");
    Console.WriteLine(response.Text);
}
