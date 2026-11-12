# Session 04 demo kit · Running Models Locally with Ollama

Agentic Sprint for .NET Developers · Milestone 2, Session 4 of 18

Tonight is demo-first again: five small programs, each built live in a few minutes from a `Start` folder. Every `Final` folder is the finished version, so you can replay any build yourself. Same packages as Session 3, so if you restored Session 3's kit, there's nothing new to download from NuGet.

| Package | Version | Why |
| --- | --- | --- |
| `Microsoft.Extensions.AI` | 10.10.0 | `IChatClient`, `GetResponseAsync<T>()`, middleware, `AddChatClient` |
| `Microsoft.Extensions.AI.OpenAI` | 10.10.1 | One adapter for any OpenAI-compatible endpoint: Groq, Gemini, and tonight Ollama |

## Setup

### 1. Install Ollama

| OS | Command (or download the installer from <https://ollama.com/download>) |
| --- | --- |
| Windows 10+ (PowerShell) | `irm https://ollama.com/install.ps1 \| iex` |
| macOS 14+ | `curl -fsSL https://ollama.com/install.sh \| sh` or the macOS app |
| Linux | `curl -fsSL https://ollama.com/install.sh \| sh` |

### 2. Pull tonight's model (1.3 GB) and say hello

```bash
ollama pull llama3.2:1b
ollama run llama3.2:1b "Say hello in one line"
ollama ps
```

Slow network? `gemma3:1b` is 815 MB. Change the model string in the code and `appsettings.json` to match.

### 3. Keys (unchanged from Session 3)

```bash
# PowerShell
$env:GROQ_API_KEY = "gsk_..."
# bash / zsh
export GROQ_API_KEY="gsk_..."
```

Ollama needs no key. Then run `dotnet restore` in every folder you want to use.

## The five builds

| # | Folder | One idea | The moment |
| --- | --- | --- | --- |
| 1 | `1-LocalHello` | Local is just another endpoint | Three strings change and the answer comes from your laptop. Ask twice: the first call is slow (the model is loading), the second is fast |
| 2 | `2-ThreeSizes` | Size is a quality dial | One Llama family, three sizes: 70B and 8B on Groq, 1B on your laptop. A trap question shows what 1B gets wrong |
| 3 | `3-Unplugged` | Local keeps working offline | "Offline" in one terminal: the cloud call crashes. A try/catch answers locally instead |
| 4 | `4-LocalTickets` | The server sets the capabilities, not the model size | The 1B local model takes a native JSON Schema with no flag (Groq's 70B Llama couldn't). Valid JSON isn't the same as right, so examples in the prompt help |
| 5 | `5-AskApi` | Your Session 3 API, now with a local provider | `dotnet run --AI:Provider=Ollama` crashes (no key!), one line fixes it, then cloud and laptop run side by side on 5080 and 5081 |

Run a console build: `cd 1-LocalHello/Final && dotnet run`. Run AskApi: `cd 5-AskApi/Final && dotnet run`, then open <http://localhost:5080/ask?q=hello>.

## Assignment

`AssignmentStarter/` is Session 3's MyAiApi solution with 5 new TODOs, a `.http` file with requests for both ports, and `COMPARE.md`. `AssignmentSolution/` is one finished answer, including stretch S1 (`POST /compare`, both answers in one request).

## Notes

- Ollama's OpenAI-compatible API lives at `http://localhost:11434/v1`. It ignores the API key, but the OpenAI SDK refuses an empty one, so we pass any string. See <https://docs.ollama.com/api/openai-compatibility>.
- The first call after a while is slow because Ollama loads the model into memory. It stays loaded for 5 minutes by default. `ollama ps` shows what's loaded, `ollama stop llama3.2:1b` unloads it.
- On the OpenAI-compatible endpoint, Ollama turns `json_schema` into grammar-constrained output, so even a 1B model returns JSON that matches your schema. Matching the schema doesn't make it right, though.
- `ChatOptions.MaxOutputTokens` is sent as `max_completion_tokens`, which Ollama's OpenAI endpoint ignores today. Don't rely on it to cap local answers.
- LLM output varies, and small models vary more. If a build "fails to fail", that's a teaching moment, not a bug.
