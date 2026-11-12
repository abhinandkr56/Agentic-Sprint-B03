// ─────────────────────────────────────────────────────────────────────────────
// Session 04 assignment · SOLUTION (TODO 1–5 done, plus stretch S1: POST /compare)
//
//   dotnet run                                                    → Groq on http://localhost:5080
//   dotnet run --AI:Provider=Ollama --urls http://localhost:5081  → Ollama on http://localhost:5081
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using MyAiApi;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// TODO 2 · the "Ollama" block lives in appsettings.json (no KeyVariable)
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var endpoint = ai["Endpoint"] ?? "";
var model = ai["Model"] ?? "";

// TODO 3 · no KeyVariable → a placeholder key. Ollama ignores it; ApiKeyCredential just needs a non-empty string.
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "not-needed";
var nativeJsonSchema = ai.GetValue("NativeJsonSchema", true);

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(model)
    .AsIChatClient();

builder.Services.AddChatClient(client);

// STRETCH S1 · a second, always-local client, registered under a key
var local = builder.Configuration.GetSection("AI:Ollama");
IChatClient localClient = new OpenAIClient(new ApiKeyCredential("not-needed"),
        new OpenAIClientOptions { Endpoint = new Uri(local["Endpoint"]!) })
    .GetChatClient(local["Model"]!)
    .AsIChatClient();
builder.Services.AddKeyedChatClient("local", localClient);

var app = builder.Build();

app.MapGet("/", () => new { provider, model, endpoint, keySet = key != "not-needed" });

app.MapPost("/ask", async (AskRequest req, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(req.Question);
    return new AskResponse(response.Text, provider, response.ModelId ?? model);
});

app.MapPost("/triage", async (TriageRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Triage), new(ChatRole.User, req.Text)];
    var response = await chat.GetResponseAsync<TicketTriage>(messages, useJsonSchemaResponseFormat: nativeJsonSchema);
    return response.TryGetResult(out var result) ? Results.Ok(result) : Results.Problem(response.Text);
});

// STRETCH S1 · one question, two answers: the configured provider and the laptop, with timings
app.MapPost("/compare", async (AskRequest req, IChatClient chat, [FromKeyedServices("local")] IChatClient laptop) =>
{
    var timer = Stopwatch.StartNew();
    var a = await chat.GetResponseAsync(req.Question);
    var cloudMs = timer.ElapsedMilliseconds;

    timer.Restart();
    var b = await laptop.GetResponseAsync(req.Question);
    var localMs = timer.ElapsedMilliseconds;

    return new
    {
        cloud = new { provider, model = a.ModelId, ms = cloudMs, answer = a.Text },
        laptop = new { provider = "Ollama", model = b.ModelId, ms = localMs, answer = b.Text },
    };
});

app.Run();

// TODO 4 · side by side: run the two commands at the top in two terminals, then use MyAiApi.http
// TODO 5 · fill in COMPARE.md
