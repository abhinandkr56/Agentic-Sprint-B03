# Session 03 · Microsoft.Extensions.AI: One Interface, Any Provider

*Agentic Sprint for .NET Developers · Milestone 1, Session 3 of 18 · take-home notes*

If you missed the session, this page plus the demo kit is everything you need. Budget 20 minutes to read it and an hour for the assignment.

---

## The one idea

**Write your AI code against one interface, `IChatClient`, and choose the provider in configuration.** Switching from Groq to Google Gemini (or later to a model on your laptop) becomes a restart with a different setting, not a rewrite.

And the caveat that goes with it: **one interface is not one set of capabilities.** Models behind the same interface still differ in features, speed, quality and limits.

## Where we are

| Session | What you built |
| --- | --- |
| 01 · LLM fundamentals | A raw `HttpClient` call to Groq. Tokens, temperature, statelessness, streaming |
| 02 · Prompting + JSON | Clear prompts (RTCCF), few-shot, three levels of JSON, a typed `CodeReview` record |
| **03 · One interface** | **`IChatClient`, typed output in one call, a provider swap by config, a Minimal API with DI** |

That completes **Milestone 1**: the syllabus promised a working AI-connected API by Session 3.

---

## Tonight in five live builds

The session was mostly code: five small programs, each built live from a `Start` folder in a few edits, with your inputs from chat. Every build is in the kit as `Start` + `Final`, so you can replay it.

| # | Build | One idea | What happened |
| --- | --- | --- | --- |
| 1 | `1-HelloChat` | `IChatClient` in 3 lines | Session 2's ~20 lines of HTTP became one `GetResponseAsync` call; `ModelId` and `Usage` are properties; then it streamed |
| 2 | `2-TwoProviders` | Same code, any provider | One extra line and Google Gemini answered the same question; a third line, Groq's gpt-oss |
| 3 | `3-TypedTickets` | A C# object in one call | `GetResponseAsync<Triage>()` crashed with HTTP 400 on Llama; one flag fixed it; `if (t.Priority == High)` paged on-call |
| 4 | `4-AskApi` | Config + DI in a Minimal API | The browser asked, the LLM answered; `dotnet run --AI:Provider=Gemini` swapped the company with zero code change |
| 5 | `4-AskApi` (EDIT 4) | Middleware | The model saw `[email]` instead of the address you typed, and every call was timed |

The sections below explain each idea, then show the code.

---

## 1. Why Microsoft.Extensions.AI exists

Every provider has its own SDK or wire quirks, and every real app needs the same things around a model call: logging, caching, retries, telemetry, tool calling. Six providers × five features = thirty things to write and maintain, and a rewrite whenever you switch.

.NET already solved this for logging (`ILogger`), caching (`IDistributedCache`) and databases (EF Core providers): your code depends on an interface; a provider package supplies the implementation. **Microsoft.Extensions.AI** (M.E.AI) applies the same idea to AI.

```text
            Agent Framework · MCP C# SDK · AI.Evaluation · your app      ← built on top
                                   │
                        Microsoft.Extensions.AI                          ← install (typed output, middleware, DI)
                                   │
                  Microsoft.Extensions.AI.Abstractions                   ← comes along (IChatClient, ChatMessage…)
                                   │
   ┌───────────────────────────────┼──────────────────────────────┐
   Microsoft.Extensions.AI.OpenAI   Azure.AI.OpenAI   OllamaSharp    Anthropic      ← provider adapters
   (Groq, Gemini, OpenAI…)          (Azure OpenAI)    (Session 4)    (Claude)
```

**A short history:** ML.NET (2018) → Semantic Kernel (2023) → M.E.AI preview (October 2024) → M.E.AI generally available and the Microsoft Agent Framework built on it (2025) → version 10.x with .NET 10 (November 2025) → 10.10 today. Everything later in this cohort (tools, MCP, agents, evaluation, tracing) plugs into `IChatClient`.

## 2. IChatClient up close

```csharp
public interface IChatClient : IDisposable
{
    Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default);

    object? GetService(Type serviceType, object? serviceKey = null);   // escape hatch to the provider SDK
}
```

Two ways to ask, one escape hatch. Convenience overloads (a plain string, `GetResponseAsync<T>()`) are extension methods.

### Every wire field has a C# home (Build 1)

| Session 1 JSON | Today |
| --- | --- |
| `"messages": [ … ]` | `List<ChatMessage>` |
| `"role": "system"` | `ChatRole.System` |
| `"temperature": 0.2` | `new ChatOptions { Temperature = 0.2f }` |
| `"max_tokens": 500` | `options.MaxOutputTokens = 500` |
| `"response_format"` | `options.ResponseFormat` |
| `choices[0].message.content` | `response.Text` |
| `"usage"` | `response.Usage` (`InputTokenCount`, `OutputTokenCount`) |
| `"finish_reason"` | `response.FinishReason` |
| `"model"` | `response.ModelId` |

**Input → `ChatMessage` · knobs → `ChatOptions` · output → `ChatResponse`.**

### Session 2's `Ask()` vs today

```csharp
// Session 02: ~20 lines of JsonObject, PostAsync, status checks and JsonNode walking.
// Session 03:
var response = await chat.GetResponseAsync(
    [new(ChatRole.System, system), new(ChatRole.User, user)],
    new ChatOptions { Temperature = 0.7f });
return response.Text;
```

Errors still happen: a non-success HTTP status becomes a `System.ClientModel.ClientResultException` with a `.Status` property.

## 3. Typed output in one call (Build 3)

Session 2 ended with a promise. Here it is, as Build 3 wrote it with the support-ticket example:

```csharp
enum Category { Billing, Bug, HowTo, FeatureRequest, Cancellation }
enum Priority { Low, Medium, High }
record Triage(Category Category, Priority Priority, string Summary);

var response = await chat.GetResponseAsync<Triage>($"{Rules}\n\nTicket: {ticket}",
    useJsonSchemaResponseFormat: false);           // needed on Groq Llama, see below
var t = response.Result;
Console.WriteLine($"→ {t.Category} | {t.Priority} | {t.Summary}");
if (t.Priority == Priority.High) Console.WriteLine("🚨 Paging the on-call engineer!");
```

```text
Ticket> UPI payment failed but the money was deducted!!
→ Billing | High | UPI payment failed, money deducted
🚨 Paging the on-call engineer!
```

It works the same with Session 2's `CodeReview` record:

```csharp
var r = await chat.GetResponseAsync<CodeReview>(messages);

if (r.TryGetResult(out var review) && review.Severity >= Severity.High)
    BlockMerge(review.Issues);
```

`GetResponseAsync<T>()` does three things:

1. **Builds a JSON Schema from your record.** Enums become `"enum"` lists, `int?` becomes `["integer", "null"]`, `[Description("…")]` attributes become descriptions the model reads.
2. **Sends it as `response_format: json_schema`** (Session 2's Level 3).
3. **Deserializes the reply.** `r.Result` throws if it doesn't fit; `r.TryGetResult(out …)` returns false instead.

The schema it generates for `CodeReview` is almost identical to the one you wrote by hand in Session 2: every property required, `additionalProperties: false`, a nullable `line`.

### One flag, two levels

| `useJsonSchemaResponseFormat` | What M.E.AI sends | Session 2 level | When |
| --- | --- | --- | --- |
| `true` (default) | `response_format: json_schema` | Level 3 | The model supports native structured output |
| `false` | `response_format: json_object` + an extra user message with the schema | Level 2 | Any model. Validate the result |

On Groq, **Llama 3.3 70B does not accept `json_schema`**, so the default call returns **HTTP 400**. gpt-oss and Gemini do accept it. The fix is one argument:

```csharp
var r = await llama.GetResponseAsync<CodeReview>(messages, useJsonSchemaResponseFormat: false);
```

That's why our `appsettings.json` has a `NativeJsonSchema` setting per provider.

## 4. One adapter, many endpoints (Build 2)

Groq and Gemini both publish OpenAI-compatible endpoints, so one package (`Microsoft.Extensions.AI.OpenAI`) covers both. Only three values change:

```csharp
IChatClient chat = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(model)
    .AsIChatClient();
```

| Provider | Endpoint | Key variable | Model |
| --- | --- | --- | --- |
| Groq | `https://api.groq.com/openai/v1` | `GROQ_API_KEY` | `llama-3.3-70b-versatile` |
| Google Gemini | `https://generativelanguage.googleapis.com/v1beta/openai/` | `GEMINI_API_KEY` | `gemini-3.5-flash` |
| OpenAI | (leave out) | `OPENAI_API_KEY` | e.g. `gpt-…` |

Other providers have their own adapters but still give you an `IChatClient`: Azure OpenAI (`AzureOpenAIClient(...).GetChatClient(deployment).AsIChatClient()`), Ollama (`new OllamaApiClient(uri, model)`, Session 4), Anthropic (`new AnthropicClient().AsIChatClient(model)`).

> **Gotcha:** don't write `using OpenAI.Chat;` next to `using Microsoft.Extensions.AI;`. Both define `ChatMessage`, and you'll get `CS0104 … is an ambiguous reference`. Use `using OpenAI;` only.

## 5. Swap by configuration (Build 4)

```json
"AI": {
  "Provider": "Groq",
  "Groq":   { "Endpoint": "https://api.groq.com/openai/v1", "Model": "llama-3.3-70b-versatile",
              "KeyVariable": "GROQ_API_KEY", "NativeJsonSchema": false },
  "Gemini": { "Endpoint": "https://generativelanguage.googleapis.com/v1beta/openai/", "Model": "gemini-3.5-flash",
              "KeyVariable": "GEMINI_API_KEY", "NativeJsonSchema": true }
}
```

`KeyVariable` holds the *name* of the environment variable, never the key itself, so this file is safe to commit.

ASP.NET Core configuration sources stack, and **later wins**:

```text
appsettings.json  →  appsettings.{Environment}.json  →  user secrets (Dev)  →  environment variables  →  command line
```

So any of these switches to Gemini without touching C#:

```bash
dotnet run --AI:Provider=Gemini                  # command line
AI__Provider=Gemini dotnet run                   # bash / zsh  (double underscore = colon)
$env:AI__Provider = "Gemini"; dotnet run         # PowerShell
```

### Swapping is cheap; testing isn't optional

| | Groq · Llama 3.3 70B | Groq · gpt-oss-20b | Google · Gemini Flash |
| --- | --- | --- | --- |
| Speed | very fast | very fast | fast |
| Native JSON Schema | no | yes | yes |
| Reasoning model | no | yes (hidden thinking tokens) | yes |
| Free tier | yes, rate-limited | yes, rate-limited | yes, per-model limits |

After every swap, rerun your 3–5 real examples (Session 2's habit). Quality, speed and limits all change.

## 6. Middleware: a pipeline around every call (Build 5)

`ChatClientBuilder` does for model calls what ASP.NET Core middleware does for requests: each layer wraps the next.

```text
your endpoint
  └─ .UseLogging()                  prompts + replies (Trace level)
      └─ .Use(…) your own           timing · PII masking · cost guard
          └─ .UseFunctionInvocation()   runs C# tools for the model   (Session 7)
              └─ .UseOpenTelemetry()      traces and token metrics      (Session 14)
                  └─ provider client      Groq · Gemini · Ollama…
```

Build 5 added this to AskApi, masking e-mail addresses before they leave your process and timing every call:

```csharp
builder.Services.AddChatClient(client)
    .Use(async (messages, options, next, ct) =>
    {
        var masked = messages.Select(m => new ChatMessage(m.Role, Regex.Replace(m.Text, @"\S+@\S+", "[email]"))).ToList();
        Console.WriteLine($"→ model sees: {masked[^1].Text}");
        var timer = Stopwatch.StartNew();
        await next(masked, options, ct);
        Console.WriteLine($"⏱ {provider}: {timer.ElapsedMilliseconds} ms");
    });
```

```text
→ model sees: Write a one-line reply to [email] about her refund
⏱ Groq: 431 ms
```

Written once, it works for every provider, because it only depends on `IChatClient`. The first middleware you add is the outermost.

## 7. Wire it into ASP.NET Core

```csharp
var builder = WebApplication.CreateBuilder(args);

IChatClient client = /* built from config, section 5 */;
builder.Services.AddChatClient(client)      // registers IChatClient (singleton)
    .UseLogging();                          // …and returns a builder for middleware

var app = builder.Build();

app.MapPost("/ask", async (AskRequest req, IChatClient chat) =>   // injected, like ILogger
{
    var r = await chat.GetResponseAsync(req.Question);
    return new AskResponse(r.Text, r.ModelId);
});

app.Run();
```

The endpoint never knows which provider it's talking to. Need two providers at once? `AddKeyedChatClient("gemini", …)` plus `[FromKeyedServices("gemini")] IChatClient chat` (stretch goal).

## 8. Builds 4 + 5 in full: AskApi

Build 4 before the middleware was added, the whole assignment in about 30 lines (`4-AskApi/Final` also contains Build 5's `.Use(…)` block from section 6):

```csharp
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration["AI:Provider"]!;
var ai = builder.Configuration.GetSection($"AI:{provider}");
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"]!)!;

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient();

builder.Services.AddChatClient(client);

var app = builder.Build();

// http://localhost:5080/ask?q=What is a record in C#?
app.MapGet("/ask", async (string q, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(q);
    return $"[{provider} · {response.ModelId}]\n\n{response.Text}";
});

app.Run();
```

Run it with `dotnet run`, then with `dotnet run --AI:Provider=Gemini`: the first line of the answer changes from `[Groq · llama-3.3-70b-versatile]` to `[Gemini · gemini-3.5-flash]`, and the code doesn't.

## 9. The demo kit

| Folder | What it is |
| --- | --- |
| `1-HelloChat/Start` → `Final` | Build 1: create an `IChatClient`, ask, print `ModelId` and `Usage`, stream |
| `2-TwoProviders/Start` → `Final` | Build 2: the same question to Groq Llama, Google Gemini and Groq gpt-oss |
| `3-TypedTickets/Start` → `Final` | Build 3: `GetResponseAsync<Triage>()`, the HTTP 400, the flag, the `if` |
| `4-AskApi/Start` → `Final` | Builds 4 + 5: Minimal API, config, DI, the swap, middleware |
| `AssignmentStarter/` | MyAiApi: your assignment, 6 TODOs, `MyAiApi.http`, `NOTES.md` |
| `AssignmentSolution/` | One finished answer, for when you're stuck |

Setup: `GROQ_API_KEY` and `GEMINI_API_KEY` in your environment, .NET 8+, and `dotnet restore` in each folder (first session with NuGet packages: `Microsoft.Extensions.AI` 10.10.0, `Microsoft.Extensions.AI.OpenAI` 10.10.1). Replay a build: `cd 3-TypedTickets/Final && dotnet run`.

## 10. Which layer when?

| Option | Use it for |
| --- | --- |
| Raw `HttpClient` | Learning, debugging the wire, a provider with no SDK (Sessions 1–2) |
| Provider SDK | A feature only that vendor has, not yet in the abstraction |
| **`IChatClient`** | **Default for application code: swappable, testable, middleware, DI** |
| Microsoft Agent Framework | Agents and workflows (Session 10), built on `IChatClient` |

They layer: `chat.GetService<OpenAI.Chat.ChatClient>()` hands you the SDK client when you need it.

## 11. Six gotchas

1. **Two `ChatMessage` types.** Don't import `OpenAI.Chat`.
2. **Model names don't travel.** `llama-3.3-70b-versatile` means nothing to Gemini. Keep models in config.
3. **Features don't travel either.** `json_schema`, tools, streaming options: check per model.
4. **Limits differ.** A 429 on a free tier is expected, not a bug.
5. **Keys in the wrong place.** Never in `appsettings.json`. Environment variables, user-secrets or Key Vault.
6. **"It compiled" ≠ "it works".** After a swap, rerun your examples.

## 12. Cheat sheet

```bash
dotnet add package Microsoft.Extensions.AI
dotnet add package Microsoft.Extensions.AI.OpenAI
```

```csharp
IChatClient chat = new OpenAIClient(cred, opts).GetChatClient(model).AsIChatClient();   // create
var r = await chat.GetResponseAsync("hi");                     r.Text                   // ask
var t = await chat.GetResponseAsync<T>(messages);              t.Result                 // typed
await foreach (var u in chat.GetStreamingResponseAsync(messages)) Console.Write(u.Text); // stream
history.AddMessages(r);                                                                 // memory
builder.Services.AddChatClient(chat).UseLogging();                                      // DI
```

| Want to… | Use |
| --- | --- |
| set temperature / max tokens | `ChatOptions` |
| keep a conversation | `List<ChatMessage>` + `AddMessages` |
| JSON on a model without `json_schema` | `useJsonSchemaResponseFormat: false` |
| swap provider | `AI:Provider` in config |
| log / time / mask every call | `.AsBuilder().Use(…).Build()` |
| reach the raw SDK | `chat.GetService<T>()` |

## 13. Quiz answers

1. **You switch from Groq to Gemini. What changes in your endpoint code?** Nothing. Only config: endpoint, key variable, model (Build 4).
2. **`GetResponseAsync<CodeReview>()` returns HTTP 400 on one model. Why, and the fix?** The model has no native `json_schema`. Pass `useJsonSchemaResponseFormat: false`, or use a model that supports it.
3. **Where does the API key live?** In an environment variable (or user-secrets / Key Vault). Never in `appsettings.json`.
4. **You want to log token usage for every call, for all providers. Where?** In middleware: `.AsBuilder().Use(…)`, or `UseOpenTelemetry()` (Session 14).

## 14. Your assignment (before Session 04)

**Syllabus:** *Build a one-endpoint Minimal API that answers a question via `IChatClient`. Swap the provider once through configuration and confirm it still works.*

You watched it in Build 4; now do it yourself. In `AssignmentStarter` (MyAiApi): get a Gemini key → build the client from config (TODO 2–3) → `POST /ask` answers a question (TODO 4) → bring your Session 2 prompt + record into `POST /triage` with `GetResponseAsync<T>()` (TODO 5) → swap once with `--AI:Provider=Gemini` and fill in `NOTES.md` (TODO 6). Stuck? `AssignmentSolution/` is one finished answer. Full steps and stretch goals: `session-03-assignment`.

**Before Session 04:** install Ollama from <https://ollama.com>. The model to pull will be in the recap message; downloads are a few GB.

## 15. Glossary

| Term | Meaning |
| --- | --- |
| **Microsoft.Extensions.AI (M.E.AI)** | Microsoft's .NET library of AI abstractions and building blocks |
| **Abstraction** | An interface your code depends on, implemented by interchangeable providers |
| **Provider / adapter** | A package that implements `IChatClient` for a specific service (OpenAI-compatible, Azure, Ollama, Anthropic) |
| **`IChatClient`** | The interface for sending messages to a chat model and getting responses |
| **`ChatMessage` / `ChatRole`** | One message and who sent it: System, User, Assistant, Tool |
| **`ChatOptions`** | Per-call settings: temperature, max tokens, response format, model, tools |
| **`ChatResponse`** | The reply: `Text`, `Messages`, `Usage`, `FinishReason`, `ModelId` |
| **`GetResponseAsync<T>()`** | Extension that builds a JSON Schema from `T`, requests structured output and deserializes the reply |
| **`useJsonSchemaResponseFormat`** | `true` = native `json_schema`; `false` = JSON mode + schema in the prompt |
| **OpenAI-compatible endpoint** | An API that accepts the OpenAI chat-completions format (Groq, Gemini's compatibility endpoint, many others) |
| **`AsIChatClient()`** | Wraps a provider SDK client as an `IChatClient` |
| **`ChatClientBuilder`** | Builds a pipeline of middleware around an `IChatClient` |
| **Middleware** | Code that wraps every call: logging, telemetry, caching, masking, tool invocation |
| **`AddChatClient`** | Registers an `IChatClient` in dependency injection and returns a builder |
| **Keyed services** | .NET 8 DI feature to register several implementations under names |
| **Configuration precedence** | Later sources override earlier ones: appsettings → environment variables → command line |
| **`ClientResultException`** | The OpenAI SDK's exception for non-success HTTP responses (`.Status`) |

## 16. Going deeper

- Microsoft.Extensions.AI overview: <https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai>
- Using `IChatClient` (history, streaming, DI, middleware): <https://learn.microsoft.com/dotnet/ai/ichatclient>
- Structured output quickstart: <https://learn.microsoft.com/dotnet/ai/quickstarts/structured-output>
- The original announcement: <https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/>
- Source code: <https://github.com/dotnet/extensions/tree/main/src/Libraries>
- Configuration in ASP.NET Core: <https://learn.microsoft.com/aspnet/core/fundamentals/configuration/>
- Groq structured outputs (which models support `json_schema`): <https://console.groq.com/docs/structured-outputs>
- Gemini's OpenAI-compatible endpoint: <https://ai.google.dev/gemini-api/docs/openai>
- Anthropic's C# SDK and `IChatClient`: <https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/csharp>
