// ─────────────────────────────────────────────────────────────────────────────
// Session 03 assignment · SOLUTION (TODO 1–6 done, no stretch goals)
//
//   dotnet run                         → Groq (from appsettings.json)
//   dotnet run --AI:Provider=Gemini    → Google Gemini, same code
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using Microsoft.Extensions.AI;
using MyAiApi;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// TODO 1 · the chosen provider's settings (keys stay in environment variables)
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var endpoint = ai["Endpoint"] ?? "";
var model = ai["Model"] ?? "";
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "";
var nativeJsonSchema = ai.GetValue("NativeJsonSchema", true);

// TODO 2 · one IChatClient
IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(model)
    .AsIChatClient();

// TODO 3 · register it
builder.Services.AddChatClient(client);

var app = builder.Build();

app.MapGet("/", () => new { provider, model, keySet = key.Length > 0, clientReady = true });

// TODO 4 · the one endpoint the syllabus asks for
app.MapPost("/ask", async (AskRequest req, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(req.Question);
    return new AskResponse(response.Text, provider, response.ModelId ?? model);
});

// TODO 5 · Session 2's prompt + record, typed in one call
app.MapPost("/triage", async (TriageRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Triage), new(ChatRole.User, req.Text)];
    var response = await chat.GetResponseAsync<TicketTriage>(messages, useJsonSchemaResponseFormat: nativeJsonSchema);
    return response.TryGetResult(out var result) ? Results.Ok(result) : Results.Problem(response.Text);
});

app.Run();

// TODO 6 · swap: dotnet run --AI:Provider=Gemini  (no code change)
