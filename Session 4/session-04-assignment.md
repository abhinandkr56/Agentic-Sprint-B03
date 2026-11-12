# Session 04 Assignment · Your API, Answering from Your Laptop

*Agentic Sprint for .NET Developers · Milestone 2, Session 4*

> **Syllabus:** Get a local model running in Ollama and answering through your existing API. Compare one answer side-by-side with the cloud version.

You watched this live in **Build 5**. Now do it in your own API.

**Time:** 30–45 minutes, plus the model download (1.3 GB).
**You need:** Ollama, `GROQ_API_KEY` (from Session 1), and either `AssignmentStarter/` from the Session 4 kit or your own MyAiApi from Session 3.

---

## What you'll hand in

- Your MyAiApi answering `/ask` and `/triage` from `llama3.2:1b` on your laptop
- `COMPARE.md` filled in: one question and one ticket, cloud vs laptop, side by side
- (Optional) one stretch goal

Post your COMPARE.md table in the channel.

---

## Starting point

`AssignmentStarter/` is **Session 3's MyAiApi solution**: `GET /`, `POST /ask`, `POST /triage`, provider chosen by `AI:Provider` in config. It compiles and runs on Groq as-is. If you finished Session 3 with your own MyAiApi, do the same TODOs in yours.

| File | What's in it |
| --- | --- |
| `Program.cs` | Session 3's API + TODO 1–5 + stretch ideas |
| `appsettings.json` | Groq and Gemini blocks, and a TODO comment where Ollama goes |
| `Models.cs` | Your Session 2/3 record and prompt (keep yours if you replaced them) |
| `MyAiApi.http` | Requests for **both** ports: `@cloud` = 5080, `@local` = 5081 |
| `COMPARE.md` | The table you fill in |

```bash
cd AssignmentStarter
dotnet restore
dotnet run            # Groq on http://localhost:5080
```

---

## Steps

### TODO 1 · Install Ollama and pull a model (10 min + download)

```bash
# Windows (PowerShell)
irm https://ollama.com/install.ps1 | iex
# macOS / Linux
curl -fsSL https://ollama.com/install.sh | sh
```

(Or the installer from <https://ollama.com/download>.) Then, in a **new** terminal:

```bash
ollama pull llama3.2:1b
ollama run llama3.2:1b "Say hello in one line"
ollama ps
```

✅ Done when `ollama ps` shows `llama3.2:1b`. Write its SIZE and PROCESSOR in COMPARE.md.

### TODO 2 · Add an Ollama block to appsettings.json (2 min)

After the Gemini block (mind the comma):

```json
"Ollama": {
  "Endpoint": "http://localhost:11434/v1",
  "Model": "llama3.2:1b",
  "NativeJsonSchema": true
}
```

No `KeyVariable`: there is no key. `NativeJsonSchema: true` because Ollama accepts a JSON Schema (Build 4). The `// TODO 2` comment can stay: ASP.NET Core's JSON configuration allows comments.

Now try it, and **read the error**:

```bash
dotnet run --AI:Provider=Ollama
```

You should see `ArgumentException: Value cannot be an empty string. (Parameter 'key')`.

### TODO 3 · A provider without a key (2 min)

This is the only line of code you change:

```csharp
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "not-needed";
```

Ollama ignores the key, but `ApiKeyCredential` refuses an empty string. Run `dotnet run --AI:Provider=Ollama` again and open <http://localhost:5080/>: it should say `"provider": "Ollama"`.

### TODO 4 · Cloud and laptop, side by side (10 min)

Two terminals, same folder:

```bash
# Terminal A: the cloud
dotnet run
# Terminal B: the laptop
dotnet run --AI:Provider=Ollama --urls http://localhost:5081
```

(Windows: if Terminal B fails to build because A is running, use `dotnet run --no-build --AI:Provider=Ollama --urls http://localhost:5081`.)

Open `MyAiApi.http` and send, to **both** ports:

1. `GET /` (check which provider each one runs)
2. the same `POST /ask` question (send the local one **twice**: the first call is the cold one)
3. the same `POST /triage` ticket

### TODO 5 · Fill in COMPARE.md (10 min)

For one question and one ticket, write down for cloud and laptop: the answer (short), the milliseconds, whether the JSON was valid, whether the answer was *right*, and your verdict. Then the four reflection questions.

---

## How you know you're done

- [ ] `ollama ps` shows `llama3.2:1b`
- [ ] `GET http://localhost:5081/` says `"provider": "Ollama"`
- [ ] `/ask` on 5081 answers, with `"model": "llama3.2:1b"`
- [ ] `/triage` on 5081 returns your record as JSON
- [ ] The only code change in Program.cs is the key line
- [ ] COMPARE.md is filled in and posted

---

## Stretch goals (pick one or more)

| # | Goal | Hint |
| --- | --- | --- |
| S1 | **`POST /compare`**: one question, both answers + timings in one response | Register a second client: `builder.Services.AddKeyedChatClient("local", localClient)`; inject `[FromKeyedServices("local")] IChatClient laptop`. The solution has it |
| S2 | **A second local model** | `ollama pull gemma3:1b`, add a `"Gemma"` block, compare three answers |
| S3 | **Cloud first, laptop fallback** in your API | Build 3: `try { cloud } catch (Exception) { local }`. Return which one answered |
| S4 | **Unplugged test** | In Terminal B's shell: `$env:HTTPS_PROXY = "http://127.0.0.1:9"`, run, prove Ollama still answers and Groq doesn't |
| S5 | **Cold vs warm** | `ollama stop llama3.2:1b`, then time the first and second `/ask`. Try `ollama run llama3.2:1b --verbose` for the exact tokens/s |

---

## Troubleshooting

| Problem | Fix |
| --- | --- |
| `Connection refused (localhost:11434)` | Ollama isn't running. Start the app, or `ollama serve` |
| `model "llama3.2:1b" not found` (404) | `ollama pull llama3.2:1b`; check `ollama list` for the exact name |
| `ArgumentNullException (Parameter 'variable')` | Your API has Session 3's `ai["KeyVariable"]!` line: apply TODO 3 |
| `ArgumentException … empty string (Parameter 'key')` | TODO 3 isn't done yet |
| Still answers as Groq | Spelling: `--AI:Provider=Ollama` must match the block name |
| JSON error at startup | Missing comma between blocks in appsettings.json |
| Very slow answers | Normal on older laptops: close other apps, plug in, and write the numbers in COMPARE.md. That *is* a finding |
| Download too slow | Use `gemma3:1b` (815 MB) and change `"Model"` |
| `/triage` returns nulls or odd values on the laptop | Valid isn't correct. Try adding 2–3 examples to your prompt (Build 4) |
| Company laptop can't install Ollama | Use a personal machine, or note it in COMPARE.md and do the comparison from the session recording |

---

## Why this matters

You now have one API that can answer from three companies' clouds *or* from your own machine, chosen by configuration. In Session 5 we'll start giving the model something to look up, and in Session 6 this same API becomes a RAG assistant.
