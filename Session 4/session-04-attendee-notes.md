# Session 04 · Running Models Locally with Ollama

*Agentic Sprint for .NET Developers · Milestone 2: Local Models & RAG · Session 4 of 18*

These notes are self-contained: if you missed the session, you can follow everything here, using the `Session04_DemoKit` (each build has a `Start` and a `Final` folder).

---

## The one idea

**A local model is just another endpoint.** Session 3's `IChatClient` talks to a model on your own laptop by changing three strings. It's smaller, slower and sometimes wrong, but private, free per call, and it works offline. Pick it on purpose.

```text
 Your C# app ──HTTP──▶ Ollama server (localhost:11434)
  IChatClient            ├─ /v1/chat/completions   ← OpenAI-shaped: same JSON as Groq
                         ├─ /api/chat              ← Ollama's own API
                         └─ engine (llama.cpp / MLX) ──▶ model file on disk (llama3.2:1b, 1.3 GB)
                                                          loaded into RAM/GPU on first use, kept 5 min
```

---

## Setup

### Install Ollama

| OS | Command (or the installer from <https://ollama.com/download>) |
| --- | --- |
| Windows 10+ (PowerShell) | `irm https://ollama.com/install.ps1 \| iex` |
| macOS 14+ | `curl -fsSL https://ollama.com/install.sh \| sh`, or the macOS app |
| Linux | `curl -fsSL https://ollama.com/install.sh \| sh` |

Then, in a **new** terminal:

```bash
ollama pull llama3.2:1b          # 1.3 GB
ollama run llama3.2:1b           # chat in the terminal; /bye to leave
ollama ps                        # what's loaded, how big, CPU or GPU
```

Slow network? `gemma3:1b` is 815 MB. Change the model name in the code and config to match.

### Keys

Same as Session 3: `GROQ_API_KEY` in an environment variable. Ollama needs no key. Packages are unchanged (`Microsoft.Extensions.AI` 10.10.0, `Microsoft.Extensions.AI.OpenAI` 10.10.1), so `dotnet restore` in each kit folder is quick.

---

## Why run a model locally?

| Reason | What it means |
| --- | --- |
| **Cost** | No per-token bill. You pay in hardware and electricity instead |
| **Privacy** | The prompt never leaves your machine |
| **Offline** | Planes, trains, air-gapped networks, flaky Wi-Fi |
| **Control** | You pin the model file. Nobody retires or changes it under you |
| **The price** | Smaller models, slower on a laptop, and you are the ops team |

### What fits on my laptop?

| Model tag | Parameters | Download | Weights | Comfortable on |
| --- | --- | --- | --- | --- |
| `llama3.2:1b` (tonight) | 1.2 B | 1.3 GB | 8-bit | any laptop |
| `llama3.1:8b` (Groq's 8B) | 8 B | 4.9 GB | 4-bit | 16 GB RAM, or a GPU |
| `llama3.3:70b` (Groq's 70B) | 70 B | 43 GB | 4-bit | a workstation |

**Rule of thumb:** 4-bit ≈ 0.6 GB per billion parameters, 8-bit ≈ 1.1 GB, plus room for your OS, IDE and the context window. *Quantized* means the weights are stored in fewer bits: smaller and faster, a little less precise.

---

## Build 1 · LocalHello: same code, on this laptop

**Idea:** a local model is just another endpoint.

Session 3's HelloChat, with three strings changed:

```csharp
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential("ollama"),                                       // any non-empty string
        new OpenAIClientOptions { Endpoint = new Uri("http://localhost:11434/v1") })
    .GetChatClient("llama3.2:1b")                                             // name:tag, with a colon
    .AsIChatClient();
```

| | endpoint | key | model |
| --- | --- | --- | --- |
| Groq | `https://api.groq.com/openai/v1` | `GROQ_API_KEY` | `llama-3.3-70b-versatile` |
| Ollama | `http://localhost:11434/v1` | `"ollama"` (any string) | `llama3.2:1b` |

The key is fake: Ollama ignores it, but the OpenAI SDK refuses an empty one (`ArgumentException … (Parameter 'key')`).

Then we asked the same question twice and timed it:

```csharp
for (var i = 1; i <= 2; i++)
{
    var timer = Stopwatch.StartNew();
    var response = await chat.GetResponseAsync(question);
    var seconds = timer.Elapsed.TotalSeconds;
    Console.WriteLine($"\n── call {i} · {response.ModelId} · {timer.ElapsedMilliseconds} ms · ≈{response.Usage?.OutputTokenCount / seconds:F0} tokens/s");
    Console.WriteLine(response.Text);
}
```

**What you saw:** call 1 is slow (Ollama loads 1.3 GB from disk into memory), call 2 is much faster. The model stays loaded for 5 minutes after the last call. `ollama ps` shows it; `ollama stop llama3.2:1b` unloads it. Your numbers depend on your CPU, GPU and RAM. **Post your tokens/s and RAM in the channel!**

---

## Build 2 · ThreeSizes: one family, three sizes

**Idea:** size is a quality dial.

The same question to Meta's Llama at three sizes, using Session 3's `Ask()` (now taking the key itself, because Ollama has none):

```csharp
await Ask("☁ Groq · 70B", "https://api.groq.com/openai/v1", groqKey, "llama-3.3-70b-versatile");
await Ask("☁ Groq · 8B", "https://api.groq.com/openai/v1", groqKey, "llama-3.1-8b-instant");
await Ask("💻 Laptop · 1B", "http://localhost:11434/v1", "ollama", "llama3.2:1b");
```

| Size | Good at |
| --- | --- |
| 70B | Reasoning, nuance, long answers, code |
| 8B | Summaries, rewrites, simple Q&A |
| 1B | Classify, extract, rephrase, with clear prompts |

**What you saw:**

- **Small models fail quietly.** Wrong answers arrive in fluent, confident English. No error, no warning. Only a test set catches them (Session 13).
- **Size doesn't fix a knowledge cutoff.** All three Llamas learned from data up to the end of 2023. Ask about .NET 10 and they guess. Retrieval (Session 6's RAG) fixes that, not a bigger model.

---

## Build 3 · Unplugged: cloud first, laptop as the backup

**Idea:** a local model keeps working when the network doesn't.

We simulated "offline" in one terminal only, so the call stayed up:

```powershell
$env:HTTPS_PROXY = "http://127.0.0.1:9"     # bash/zsh: export HTTPS_PROXY=http://127.0.0.1:9
dotnet run --no-restore
```

All HTTPS traffic from that process now goes to a port where nothing listens. Ollama is plain `http://localhost`, so it isn't affected. The cloud call crashed:

```text
Unhandled exception. System.AggregateException: Retry failed after 4 tries. (Connection refused (127.0.0.1:9)) ...
```

The SDK retried four times, then threw an **`AggregateException`**, not the `ClientResultException` you get for HTTP errors like 401 or 429. So the fallback catches `Exception`:

```csharp
ChatResponse response;
try
{
    response = await cloud.GetResponseAsync(question);
}
catch (Exception ex)
{
    Console.WriteLine($"☁ cloud failed ({ex.GetType().Name}) → answering on this laptop");
    response = await local.GetResponseAsync(question);
}
Console.WriteLine($"[{response.ModelId}] {response.Text}");
```

Back online: `Remove-Item Env:HTTPS_PROXY` (bash: `unset HTTPS_PROXY`).

**Takeaway:** a degraded answer beats an error page. Tell the user which one they got.

---

## Build 4 · LocalTickets: typed output on a 1B model

**Idea:** the server sets the capabilities, not the model size.

In Session 3, `GetResponseAsync<Triage>()` on Groq's 70B Llama failed with **HTTP 400** until we added `useJsonSchemaResponseFormat: false`. Tonight, the same call on the 1B laptop model, **with no flag**, just worked:

```csharp
IChatClient local = Client("http://localhost:11434/v1", "ollama", "llama3.2:1b");

var c = (await cloud.GetResponseAsync<Triage>($"{Rules}\n\nTicket: {ticket}", useJsonSchemaResponseFormat: false)).Result;
var l = (await local.GetResponseAsync<Triage>($"{Rules}\n\nTicket: {ticket}")).Result;   // no flag

enum Category { Billing, Bug, HowTo, FeatureRequest, Cancellation }
enum Priority { Low, Medium, High }
record Triage(Category Category, Priority Priority, string Summary);
```

| Server · model | `json_schema` | Why |
| --- | --- | --- |
| Groq · Llama 3.3 70B | ✗ HTTP 400 | Groq's server doesn't offer it for Llama models |
| Ollama · Llama 3.2 1B | ✓ works | Ollama constrains every generated token to fit your schema |

**Valid ≠ correct.** The grammar guarantees the *shape* (every field, only enum values). It can't guarantee `Priority.High` is the right priority. The 1B often disagreed with the 70B. Adding Session 2's few-shot examples to the prompt helped:

```csharp
const string Rules = """
    Triage this support ticket for an Indian SaaS company.
    Priority: High = money lost or the app is down. Medium = broken, but there is a workaround. Low = questions and ideas.
    Examples:
    "UPI payment failed but the money was deducted" → Billing, High
    "The app crashes when I upload a photo, I can use the website instead" → Bug, Medium
    "How do I export my invoices to Excel?" → HowTo, Low
    "Please add dark mode" → FeatureRequest, Low
    """;
```

Clear rules and examples help every model, and small models the most. The prompt is part of the model choice.

---

## Build 5 · AskApi + Ollama: your Session 3 API, now local too

**Idea:** one config block and one line of code make your API run on a local model, side by side with the cloud.

**1. One more block in `appsettings.json`** (no `KeyVariable`: there is no key):

```json
"AI": {
  "Provider": "Groq",
  "Groq":   { "Endpoint": "https://api.groq.com/openai/v1", "Model": "llama-3.3-70b-versatile", "KeyVariable": "GROQ_API_KEY" },
  "Gemini": { "Endpoint": "https://generativelanguage.googleapis.com/v1beta/openai/", "Model": "gemini-3.5-flash", "KeyVariable": "GEMINI_API_KEY" },
  "Ollama": { "Endpoint": "http://localhost:11434/v1", "Model": "llama3.2:1b" }
}
```

**2. `dotnet run --AI:Provider=Ollama` crashed** at startup: `ArgumentNullException: Value cannot be null. (Parameter 'variable')`. Session 3's key line assumed every provider has a key variable.

**3. The one-line fix:**

```csharp
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "not-needed";
```

No variable name → a placeholder key, which Ollama ignores. Session 3's middleware (e-mail masking and timing) kept working for the new provider without changes.

**4. Side by side**, two terminals, one folder:

```bash
dotnet run                                                     # Groq   → http://localhost:5080/ask?q=…
dotnet run --AI:Provider=Ollama --urls http://localhost:5081   # Ollama → http://localhost:5081/ask?q=…
```

(On Windows, if the second one can't build because the first is running, add `--no-build`.)

Config chose the provider. The command line chose the port. Program.cs changed once, for keyless providers.

---

## Local or cloud?

| 💻 Local when… | ☁ Cloud when… |
| --- | --- |
| Data must not leave the machine or network | Quality and reasoning matter most |
| It must work offline | Long documents, big context windows |
| The task is narrow: classify, extract, rewrite | Your users' laptops can't run it |
| You call it a lot and cost matters | Many users at once, no GPUs to manage |
| Dev, tests and demos without a key | You need the newest models |

Most real apps use both: Build 3's fallback, or local for drafts and cloud for the final answer.

---

## Six gotchas

1. **The first call is slow.** Model loading. Warm it up at app startup, or keep it loaded longer (`OLLAMA_KEEP_ALIVE`).
2. **4K context by default** on most laptops (Ollama sizes it by GPU memory). Long conversations quietly lose their oldest messages. Raise it with `OLLAMA_CONTEXT_LENGTH` on the server (costs memory). This will matter for RAG.
3. **Tags have colons.** `llama3.2:1b`, not `llama-3.2-1b`. `ollama list` shows the exact names.
4. **No key ≠ empty key.** The SDK refuses `""`; pass any string.
5. **`MaxOutputTokens` is ignored** on Ollama's OpenAI endpoint: the SDK sends `max_completion_tokens`, and Ollama only reads `max_tokens`. Ask for brevity in the prompt instead.
6. **localhost only.** Ollama listens on `127.0.0.1:11434`. `OLLAMA_HOST=0.0.0.0` exposes it to your network **with no authentication**.

---

## Cheat sheet

```bash
ollama pull llama3.2:1b     # download
ollama run llama3.2:1b      # chat in the terminal (--verbose shows tokens/s)
ollama list                 # what's on disk
ollama ps                   # what's in memory
ollama stop llama3.2:1b     # unload it
ollama rm llama3.2:1b       # delete it
```

| Want to… | Use |
| --- | --- |
| swap your API to local | `--AI:Provider=Ollama` |
| run cloud + local at once | `--urls http://localhost:5081` for the second instance |
| survive the cloud failing | `try { cloud } catch (Exception) { local }` |
| typed output locally | `GetResponseAsync<T>()`, no flag needed |
| a longer context | `OLLAMA_CONTEXT_LENGTH` (server setting) |
| keep the model loaded | `OLLAMA_KEEP_ALIVE` |

---

## Assignment

> **Syllabus:** Get a local model running in Ollama and answering through your existing API. Compare one answer side-by-side with the cloud version.

You watched it in Build 5. Use `AssignmentStarter/` (Session 3's MyAiApi solution + 5 TODOs), or your own MyAiApi:

1. **TODO 1:** install Ollama, `ollama pull llama3.2:1b`, `ollama run`, `ollama ps`.
2. **TODO 2:** add an `"Ollama"` block to `appsettings.json` (Endpoint, Model, `NativeJsonSchema: true`, no `KeyVariable`).
3. **TODO 3:** make the key line work for a provider without a key (the only code change).
4. **TODO 4:** run cloud on 5080 and laptop on 5081; send the same `/ask` and `/triage` to both from `MyAiApi.http`.
5. **TODO 5:** fill in `COMPARE.md`: answers, milliseconds, valid JSON?, right answer?, would you ship it?

**Stretch (pick one):** `POST /compare` with keyed services (both answers in one call) · a second local model (`gemma3:1b`) · Build 3's fallback inside your API · the unplugged test · cold vs warm timings after `ollama stop`.

Stuck? `AssignmentSolution/` has a finished version, including `/compare`. Post your COMPARE.md table in the channel.

---

## Glossary

| Term | Meaning |
| --- | --- |
| **Ollama** | A local server + CLI that downloads open models and runs them on your machine, with an HTTP API |
| **Local model** | A model running on your own hardware instead of a provider's data centre |
| **Open weights** | Model weights you can download and run. Licences still apply |
| **Parameters** | The numbers in a model's weights. "1B" ≈ one billion |
| **Quantization** | Storing weights in fewer bits (8 or 4 instead of 16): smaller, faster, slightly less precise |
| **Model tag** | Ollama's `name:tag` naming, e.g. `llama3.2:1b` |
| **Cold start** | The first call after a model is unloaded: it must be loaded into memory first |
| **keep_alive** | How long Ollama keeps a model in memory after the last call (default 5 minutes) |
| **Context window** | How many tokens the model sees at once. Ollama's default on most laptops is 4K |
| **Tokens per second** | Generation speed. Depends on your hardware |
| **Knowledge cutoff** | The date the model's training data ends. Later facts are unknown to it |
| **Constrained decoding** | The server only lets the model generate tokens that keep the output valid against a schema or grammar |
| **Fallback** | Using a second provider when the first one fails |
| **Keyed services** | DI registrations with a name, e.g. `AddKeyedChatClient("local", …)` + `[FromKeyedServices("local")]` |

---

## Going deeper

- Ollama download and docs: <https://ollama.com/download> · <https://docs.ollama.com>
- Ollama's OpenAI compatibility: <https://docs.ollama.com/api/openai-compatibility>
- Context length: <https://docs.ollama.com/context-length>
- Ollama FAQ (keep-alive, model storage, network exposure): <https://docs.ollama.com/faq>
- Model library: <https://ollama.com/library> (e.g. <https://ollama.com/library/llama3.2>)
- Groq structured outputs (who supports `json_schema`): <https://console.groq.com/docs/structured-outputs>
- Microsoft.Extensions.AI overview: <https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai>

## Next time · Session 05

**Vector Search & Microsoft.Extensions.VectorData Basics.** Tonight the 1B model guessed when it didn't know. Next, we give it something to look up: what an embedding is (no math), storing a handful of snippets, and searching them by meaning. Keep Ollama installed: it can make embeddings too.
