# Session 03 Assignment · One endpoint, one IChatClient, one swap

**Agentic Sprint for .NET · Milestone 1, Session 3 of 18 · due before Session 04**

## The goal (from the syllabus)

Build a one-endpoint Minimal API that answers a question via `IChatClient`. Swap the provider once through configuration and confirm it still works.

**Time:** 45–60 minutes for the core. Stretch goals are optional.
**You need:** .NET 8+ SDK, `GROQ_API_KEY`, a free `GEMINI_API_KEY` (or another OpenAI-compatible provider), the `AssignmentStarter` folder (MyAiApi) from the Session 03 kit. You watched the core of this live in Build 4 (`4-AskApi`). Stuck? `AssignmentSolution/` in the same kit is one finished answer.

---

## Steps

### Step 0 · Restore and run as-is (5 min)

```bash
cd AssignmentStarter
dotnet restore        # first time this cohort that packages are downloaded
dotnet run
```

Open `MyAiApi.http` and send `GET /`. You should see `"clientReady": false` and your provider and model. `POST /ask` replies with a TODO message. That's expected.

### Step 1 · Choose your providers (5 min) · TODO 1

- Groq is already configured in `appsettings.json`.
- Get a free Gemini API key at <https://aistudio.google.com> and set it:

```bash
# PowerShell
$env:GEMINI_API_KEY = "..."
# bash / zsh
export GEMINI_API_KEY="..."
```

- Prefer another provider (OpenAI, Azure OpenAI, OpenRouter…)? Add a block with `Endpoint`, `Model`, `KeyVariable` and `NativeJsonSchema`.
- **Never put a key in `appsettings.json`.** `KeyVariable` is the *name* of the environment variable.

### Step 2 · Build ONE IChatClient from config (5 min) · TODO 2

This is Build 4's EDIT 1–2.

Replace `IChatClient? client = null;` with:

```csharp
IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(model)
    .AsIChatClient();
```

### Step 3 · Register it with DI (2 min) · TODO 3

```csharp
builder.Services.AddChatClient(client);
```

Restart, send `GET /`: `"clientReady": true`.

### Step 4 · The one endpoint (10 min) · TODO 4

Inject `IChatClient` into `/ask`, call the model, and return the answer and the model that replied:

```csharp
app.MapPost("/ask", async (AskRequest req, IChatClient chat) =>
{
    var response = await chat.GetResponseAsync(req.Question);
    return new AskResponse(response.Text, provider, response.ModelId ?? model);
});
```

Send the `/ask` request from `MyAiApi.http`. **This is the syllabus's core requirement.**

### Step 5 · Bring your Session 2 work (15 min) · TODO 5

1. In `Models.cs`, replace `TicketTriage` with your Session 2 record, and `Prompts.Triage` with your best (v3) prompt. Tip: turn fixed-choice strings into enums with `[JsonConverter(typeof(JsonStringEnumConverter))]`, and add `[property: Description("…")]` where a field needs an instruction.
2. Implement `/triage` (rename it if you like):

```csharp
app.MapPost("/triage", async (TriageRequest req, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Prompts.Triage), new(ChatRole.User, req.Text)];
    var response = await chat.GetResponseAsync<TicketTriage>(messages, useJsonSchemaResponseFormat: nativeJsonSchema);
    return response.TryGetResult(out var result) ? Results.Ok(result) : Results.Problem(response.Text);
});
```

3. Try it once with `NativeJsonSchema: true` on Groq Llama to see the HTTP 400, then set it back to `false`.

### Step 6 · Swap the provider, by config only (5 min) · TODO 6

Stop the app and run **one** of:

```bash
dotnet run --AI:Provider=Gemini
AI__Provider=Gemini dotnet run                 # bash / zsh
$env:AI__Provider = "Gemini"; dotnet run       # PowerShell
```

Send `GET /` (it should say Gemini), then the same `/ask` and `/triage` requests. **You should not change a single line of `Program.cs`.**

### Step 7 · Fill in NOTES.md (10 min)

The same-request-two-providers table, how many lines you changed for the swap (target: zero), and the four reflections.

### Step 8 · Share

Post your NOTES.md table in the community channel.

---

## Done when

- [ ] `POST /ask` answers a real question through an injected `IChatClient`
- [ ] Provider settings come from `appsettings.json`; keys come from environment variables
- [ ] The same requests work after `--AI:Provider=Gemini` (or your second provider), with no code change
- [ ] Your Session 2 record comes back typed from `GetResponseAsync<T>()`
- [ ] `NOTES.md` filled in and posted

---

## Stretch goals (pick one or more)

| # | Stretch | Hint |
| --- | --- | --- |
| S1 | **Build 5's middleware** in your API: mask e-mails and log the ms for every call | Copy the `.Use(async (messages, options, next, ct) => { … })` block from `4-AskApi/Final`. Harder: log tokens too, with `Use(getResponseFunc: …, getStreamingResponseFunc: null)`, which can see the response |
| S2 | **Both providers at once** | `AddKeyedChatClient("groq", …)`, `AddKeyedChatClient("gemini", …)`, then `[FromKeyedServices("gemini")] IChatClient chat`. Add `?provider=` to choose per request |
| S3 | **Streaming endpoint** | `await foreach (var u in chat.GetStreamingResponseAsync(q)) await ctx.Response.WriteAsync(u.Text);` Try it with `curl -N` |
| S4 | **Fallback** | Catch `ClientResultException` with `Status == 429` or `>= 500` on the first provider and retry the request on the second |
| S5 | **A third provider** | OpenAI, Azure OpenAI, or Ollama if you want a head start on Session 4 |
| S6 | **See the wire** | `.UseLogging()` and `"Microsoft.Extensions.AI": "Trace"` in appsettings.json. Find the schema `GetResponseAsync<T>` sent |

---

## Common blockers

| Symptom | Fix |
| --- | --- |
| `NU1101` / restore hangs | NuGet blocked (corporate proxy?). Try another network, or check `dotnet nuget list source` |
| `CS0104 'ChatMessage' is an ambiguous reference` | Remove `using OpenAI.Chat;`. Keep `using OpenAI;` |
| `CS1061 … does not contain a definition for 'AsIChatClient'` | Missing `using Microsoft.Extensions.AI;` |
| `ArgumentNullException` for the key, or `"keySet": false` | Env var not set in *this* terminal (or restart your IDE after setting it) |
| `HTTP 401` | Wrong key for that provider. Gemini keys come from AI Studio |
| `HTTP 400 … json_schema` | Model has no native schema support: `NativeJsonSchema: false` for that provider |
| `HTTP 404` | Model id not valid for that provider. Check its model list |
| `HTTP 429` | Free-tier rate limit. Wait a minute, or use a smaller model |
| `GET /` still says Groq after the swap | Typo in `--AI:Provider=`, or the section name in appsettings.json doesn't match |
| `/triage` returns a problem with the raw text | The reply didn't fit your record. Check property names in your prompt, or try `NativeJsonSchema: true` on a model that supports it |
| Port 5080 in use | `dotnet run --urls http://localhost:5081` and update `@api` in the .http file |

---

## Why this matters for Session 04

Keep this API. Next session you add **Ollama** as a third provider and compare a local model's answer side by side with the cloud version, still through `IChatClient`. Install Ollama before class.
