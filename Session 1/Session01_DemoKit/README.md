# Session 01 · LLM Fundamentals for .NET Developers — Demo Kit

"An LLM is just another HTTP API." Everything here is plain `HttpClient` + `System.Text.Json`. No NuGet packages.

## What's inside

| Folder / file | What it is |
|---|---|
| `HelloLLM/` | The instructor demo app: a menu of 8 live demos |
| `HelloLLM/groq.http` | The same raw call as a `.http` file (Visual Studio 2022 / VS Code REST Client) |
| `AssignmentStarter/` | Skeleton with TODOs that attendees finish as homework, plus `NOTES.md` |

## Setup (5 minutes)

1. Install the .NET 8, 9 or 10 SDK (`dotnet --version`). The project targets `net8.0`; switch it to `net10.0` if you like.
2. Create a free Groq API key at https://console.groq.com/keys (no credit card).
3. Set the key in the terminal you'll run from:

   ```powershell
   # PowerShell
   $env:GROQ_API_KEY = "gsk_..."
   ```
   ```bash
   # bash / zsh
   export GROQ_API_KEY="gsk_..."
   ```
4. Run it:
   ```bash
   cd HelloLLM
   dotnet run          # menu
   dotnet run -- 3     # jump straight to demo 3
   ```

### Optional settings

| Variable | Default | Why change it |
|---|---|---|
| `LLM_MODEL` | `llama-3.3-70b-versatile` | `llama-3.1-8b-instant` is faster and has higher free limits. `openai/gpt-oss-20b` also works |
| `LLM_BASE_URL` | `https://api.groq.com/openai/v1` | Point to any OpenAI-compatible API: OpenAI `https://api.openai.com/v1`, xAI Grok `https://api.x.ai/v1`, local Ollama `http://localhost:11434/v1` |

> If you meant **xAI's Grok** rather than **Groq**: set `LLM_BASE_URL=https://api.x.ai/v1`, `LLM_MODEL` to a current Grok model id, and put your xAI key in `GROQ_API_KEY` (or `LLM_API_KEY`). The code doesn't change, and that's the point of the session.

## The 8 demos

| # | Demo | The "aha" |
|---|---|---|
| 1 | Raw call | A hand-written JSON string goes out and a JSON string comes back. You already know how to do this |
| 2 | Typed call | Map it to C# records; read `content`, `usage`, `finish_reason` |
| 3 | Tokens | The same greeting in English, Hindi and Malayalam costs very different token counts |
| 4 | Temperature | Same prompt at 0 and at 1.5 gives consistent vs creative answers |
| 5 | Memory | The API is stateless. "Memory" means you resend the whole history, and you pay for it every turn |
| 6 | Streaming | SSE `data:` lines; time-to-first-token is what feels fast |
| 7 | Limits & errors | `finish_reason: length`, 404 for an unknown model, 401 for a bad key |
| 8 | DotNetBuddy | System prompt + history + streaming makes a chatbot in about 30 lines |

## If things go wrong live

| Symptom | Fix |
|---|---|
| `GROQ_API_KEY is not set` | You set it in a different terminal. Set it again in this one |
| HTTP 401 | Key typo, or extra quotes or spaces. Regenerate it |
| HTTP 429 | Free-tier rate limit. Wait 60 seconds or `set LLM_MODEL=llama-3.1-8b-instant` |
| HTTP 404 model | The model was renamed or retired. Check https://console.groq.com/docs/models |
| Corporate network blocks it | Use a phone hotspot. Keep a screen recording of each demo as a backup |
| Strange characters in Hindi/Malayalam | Use Windows Terminal (not the old conhost). The app already sets UTF-8 output |
