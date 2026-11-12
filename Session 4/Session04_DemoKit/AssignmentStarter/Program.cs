// ─────────────────────────────────────────────────────────────────────────────
// Session 04 assignment starter · Running Models Locally with Ollama
//
// Syllabus: "Get a local model running in Ollama and answering through your existing API.
//            Compare one answer side-by-side with the cloud version."
//
// This is the Session 3 MyAiApi solution. It compiles and runs as-is on Groq.
// Work through TODO 1 → TODO 5, test with MyAiApi.http, then fill in COMPARE.md.
// If you finished Session 3 with your own MyAiApi, you can do the same TODOs in yours instead.
//
//   PowerShell:  $env:GROQ_API_KEY = "gsk_..."      bash/zsh:  export GROQ_API_KEY="gsk_..."
//   dotnet run
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using Microsoft.Extensions.AI;
using MyAiApi;
using OpenAI;

// ── TODO 1 · Install Ollama and pull a small model (terminal, not code) ─────
//   Windows (PowerShell):  irm https://ollama.com/install.ps1 | iex     (or OllamaSetup.exe from ollama.com/download)
//   macOS / Linux:         curl -fsSL https://ollama.com/install.sh | sh  (or the macOS app from ollama.com/download)
//   Then:  ollama pull llama3.2:1b     ollama run llama3.2:1b "Say hello in one line"     ollama ps
// Done when `ollama ps` shows llama3.2:1b loaded.

var builder = WebApplication.CreateBuilder(args);

// ── TODO 2 · Add an "Ollama" block to appsettings.json ──────────────────────
// Endpoint http://localhost:11434/v1, Model llama3.2:1b, NativeJsonSchema true, and NO KeyVariable.
// Then try it:  dotnet run --AI:Provider=Ollama   (it will fail at startup until TODO 3; read the error)
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var endpoint = ai["Endpoint"] ?? "";
var model = ai["Model"] ?? "";

// ── TODO 3 · Make this line work for a provider that has no key ─────────────
// Ollama ignores the key, but ApiKeyCredential refuses an empty string.
// Hint: replace the final  ?? ""  with  ?? "not-needed"
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "";
var nativeJsonSchema = ai.GetValue("NativeJsonSchema", true);

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(model)
    .AsIChatClient();

builder.Services.AddChatClient(client);

var app = builder.Build();

app.MapGet("/", () => new { provider, model, endpoint, keySet = key.Length > 0 });

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

app.Run();

// ── TODO 4 · Cloud and laptop, side by side (no code) ───────────────────────
//   Terminal A:  dotnet run                                                    (Groq on http://localhost:5080)
//   Terminal B:  dotnet run --AI:Provider=Ollama --urls http://localhost:5081  (Ollama on http://localhost:5081)
// Send the same requests to both from MyAiApi.http. Run each local request twice: the first call is the cold one.

// ── TODO 5 · Fill in COMPARE.md ─────────────────────────────────────────────
// One /ask question and one /triage ticket, both providers: the answer, the milliseconds, and your verdict.

// ── STRETCH (pick one or more) ─────────────────────────────────────────────
// S1. One request, both answers: register a second client with
//     builder.Services.AddKeyedChatClient("local", localClient) and add POST /compare that takes
//     IChatClient chat and [FromKeyedServices("local")] IChatClient local, and returns both answers with timings.
// S2. A second local model: ollama pull gemma3:1b, add a block for it, compare three answers.
// S3. Cloud first, laptop fallback (Build 3): if the cloud call throws, answer with the local client.
// S4. Unplugged test: set HTTPS_PROXY=http://127.0.0.1:9 in Terminal B's shell and prove Ollama still answers.
// S5. Cold vs warm: `ollama stop llama3.2:1b`, then time the first and second /ask. Write both numbers down.
