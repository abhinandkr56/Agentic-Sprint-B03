// ─────────────────────────────────────────────────────────────────────────────
// Session 03 assignment starter · Microsoft.Extensions.AI: One Interface, Any Provider
//
// Syllabus: "Build a one-endpoint Minimal API that answers a question via IChatClient.
//            Swap the provider once through configuration and confirm it still works."
//
// It compiles and runs as-is: /ask replies with a TODO message until you finish TODO 2–4.
// Work through TODO 1 → TODO 6, test with MyAiApi.http, then fill in NOTES.md.
//
//   PowerShell:  $env:GROQ_API_KEY = "gsk_..."      bash/zsh:  export GROQ_API_KEY="gsk_..."
//   dotnet run
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using Microsoft.Extensions.AI;
using MyAiApi;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// ── TODO 1 · Choose your providers in appsettings.json ──────────────────────
// Groq is set up already. Get a free Gemini key (aistudio.google.com) and set GEMINI_API_KEY,
// or add a section for another OpenAI-compatible provider you have access to.
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var endpoint = ai["Endpoint"] ?? "";
var model = ai["Model"] ?? "";
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "";
var nativeJsonSchema = ai.GetValue("NativeJsonSchema", true);

// ── TODO 2 · Build ONE IChatClient from the settings above ──────────────────
// Hint (3 lines):
//   new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
//       .GetChatClient(model)
//       .AsIChatClient();
IChatClient? client = null;

// ── TODO 3 · Register it with dependency injection ──────────────────────────
// Hint: builder.Services.AddChatClient(client);
//       Stretch: add .UseLogging() after it and set "Microsoft.Extensions.AI": "Trace" in appsettings.json

var app = builder.Build();

app.MapGet("/", () => new { provider, model, keySet = key.Length > 0, clientReady = client is not null });

// ── TODO 4 · The one endpoint the syllabus asks for ─────────────────────────
// 1. Add IChatClient as a second parameter:  async (AskRequest req, IChatClient chat) => ...
// 2. var response = await chat.GetResponseAsync(req.Question);
// 3. return new AskResponse(response.Text, provider, response.ModelId ?? model);
app.MapPost("/ask", (AskRequest req) =>
{
    return new AskResponse($"TODO 4: send \"{req.Question}\" to the model", provider, model);
});

// ── TODO 5 · Bring your Session 2 prompt + record ───────────────────────────
// Replace Prompts.Triage and the TicketTriage record (Models.cs) with YOUR prompt and record.
// Then, in this endpoint (inject IChatClient like TODO 4):
//   List<ChatMessage> messages = [ new(ChatRole.System, Prompts.Triage), new(ChatRole.User, req.Text) ];
//   var response = await chat.GetResponseAsync<TicketTriage>(messages, useJsonSchemaResponseFormat: nativeJsonSchema);
//   return response.TryGetResult(out var result) ? Results.Ok(result) : Results.Problem(response.Text);
app.MapPost("/triage", (TriageRequest req) =>
{
    return Results.Ok(new { todo = "TODO 5: return a typed TicketTriage for this input", input = req.Text });
});

app.Run();

// ── TODO 6 · Swap the provider without touching this file ───────────────────
// Stop the app, then run ONE of these and repeat your requests from MyAiApi.http:
//   dotnet run --AI:Provider=Gemini
//   PowerShell: $env:AI__Provider = "Gemini"; dotnet run      bash: AI__Provider=Gemini dotnet run
// Record what changed (answers, speed, JSON behaviour) in NOTES.md.

// ── STRETCH (pick one or more) ─────────────────────────────────────────────
// S1. Middleware: .AsBuilder().Use(...) that logs elapsed ms and tokens for every call (see Build 5 in 4-AskApi/Final).
// S2. Both providers at once: AddKeyedChatClient("groq", ...) and AddKeyedChatClient("gemini", ...),
//     then pick per request with [FromKeyedServices("gemini")] IChatClient chat.
// S3. Streaming: an endpoint that returns chat.GetStreamingResponseAsync(...) as text, token by token.
// S4. Fallback: if the first provider throws (429, 5xx), retry the same request on the second one.
// S5. A third provider: OpenAI, Azure OpenAI, or Ollama if you want a head start on Session 4.
