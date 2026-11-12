// Build 1 · HelloChat (FINAL: where you end up after the edits)
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

Console.Write("Question> ");
var question = Console.ReadLine()!;

// EDIT 1: an IChatClient for Groq
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient();

// EDIT 2: one line to ask. Everything Session 1 parsed by hand is now a property.
var response = await chat.GetResponseAsync(question);
Console.WriteLine(response.Text);
Console.WriteLine($"\n[{response.ModelId} · {response.Usage?.InputTokenCount} tokens in · {response.Usage?.OutputTokenCount} out]");

// EDIT 3 (optional): the same question, streamed
Console.WriteLine("\n── streamed ──");
await foreach (var update in chat.GetStreamingResponseAsync(question))
    Console.Write(update.Text);
Console.WriteLine();
