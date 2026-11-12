// Build 1 · LocalHello (START: open this before the call)
// Session 3's HelloChat, unchanged: a 70B Llama in Groq's data centre.
// Tonight: the same code, answering from this laptop.
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;

// ▼ EDIT 1: point it at Ollama (three strings change)
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient();

Console.Write("Question> ");
var question = Console.ReadLine()!;

// ▼ EDIT 2: ask twice and time it (cold vs warm)
var response = await chat.GetResponseAsync(question);
Console.WriteLine(response.Text);
Console.WriteLine($"\n[{response.ModelId} · {response.Usage?.InputTokenCount} tokens in · {response.Usage?.OutputTokenCount} out]");
