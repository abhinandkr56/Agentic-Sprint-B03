// Build 4 + 5 · AskApi (FINAL: where you end up after the edits)
using System.ClientModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// EDIT 2: which provider? Configuration decides, not code
var provider = builder.Configuration["AI:Provider"]!;
var ai = builder.Configuration.GetSection($"AI:{provider}");
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"]!)!;

// EDIT 1: one IChatClient, built once
IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient();

builder.Services.AddChatClient(client)                      // EDIT 2: register it
    .Use(async (messages, options, next, ct) =>             // EDIT 4: middleware around every call
    {
        var masked = messages.Select(m => new ChatMessage(m.Role, Regex.Replace(m.Text, @"\S+@\S+", "[email]"))).ToList();
        Console.WriteLine($"→ model sees: {masked[^1].Text}");
        var timer = Stopwatch.StartNew();
        await next(masked, options, ct);
        Console.WriteLine($"⏱ {provider}: {timer.ElapsedMilliseconds} ms");
    });

var app = builder.Build();

// Open in a browser:  http://localhost:5080/ask?q=What is a record in C#?
app.MapGet("/ask", async (string q, IChatClient chat) =>    // EDIT 2: injected
{
    var response = await chat.GetResponseAsync(q);          // EDIT 1: the model answers
    return $"[{provider} · {response.ModelId}]\n\n{response.Text}";
});

app.Run();

// EDIT 3 is not code. Stop the app (Ctrl+C), then:  dotnet run --AI:Provider=Gemini
