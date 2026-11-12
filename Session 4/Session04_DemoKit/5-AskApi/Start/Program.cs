// Build 5 · AskApi + Ollama (START: this is Session 3's AskApi, exactly as we left it)
// Tonight it gets a third provider, and that one lives on this laptop.
using System.ClientModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ▼ EDIT 1 is in appsettings.json: add an "Ollama" block, then:  dotnet run --AI:Provider=Ollama
var provider = builder.Configuration["AI:Provider"]!;
var ai = builder.Configuration.GetSection($"AI:{provider}");
// ▼ EDIT 2: a provider without a key
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"]!)!;

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient();

builder.Services.AddChatClient(client)
    .Use(async (messages, options, next, ct) =>
    {
        var masked = messages.Select(m => new ChatMessage(m.Role, Regex.Replace(m.Text, @"\S+@\S+", "[email]"))).ToList();
        Console.WriteLine($"→ model sees: {masked[^1].Text}");
        var timer = Stopwatch.StartNew();
        await next(masked, options, ct);
        Console.WriteLine($"⏱ {provider}: {timer.ElapsedMilliseconds} ms");
    });

var app = builder.Build();

// Open in a browser:  http://localhost:5080/ask?q=What is a record in C#?
app.MapGet("/ask", async (string q, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(q);
    return $"[{provider} · {response.ModelId}]\n\n{response.Text}";
});

app.Run();

// ▼ EDIT 3 is not code. Cloud and laptop side by side, in two terminals:
//     Terminal A:  dotnet run                                                   → http://localhost:5080/ask?q=…
//     Terminal B:  dotnet run --AI:Provider=Ollama --urls http://localhost:5081  → http://localhost:5081/ask?q=…
