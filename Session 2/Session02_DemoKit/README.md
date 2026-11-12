# Session 02 demo kit · Prompt Engineering & Getting Useful Output

Agentic Sprint for .NET Developers · Milestone 1, Session 2 of 18

Same conventions as Session 1's `HelloLLM`: raw `HttpClient` + `System.Text.Json`, no NuGet packages, .NET 8, keys from environment variables.

## Setup

```bash
# PowerShell
$env:GROQ_API_KEY = "gsk_..."
# bash / zsh
export GROQ_API_KEY="gsk_..."
```

Optional:

| Variable | Default | Used for |
| --- | --- | --- |
| `LLM_MODEL` | `llama-3.3-70b-versatile` | Demos 1–6 and 8 |
| `STRICT_MODEL` | `openai/gpt-oss-20b` | Demo 7 (`json_schema` with `strict: true`) |
| `LLM_BASE_URL` | `https://api.groq.com/openai/v1` | Any OpenAI-compatible endpoint |

## PromptLab (the live demos)

```bash
cd PromptLab
dotnet run            # menu
dotnet run -- 3       # jump to demo 3
dotnet run -- all     # rehearse everything
```

| # | Demo | The surprise |
| --- | --- | --- |
| 1 | Vague vs specific | Same model, same code: an essay vs a PR comment you can paste |
| 2 | Three system prompts | One question, three completely different answers |
| 3 | Zero-shot vs few-shot | Zero-shot invents its own labels; 3 examples lock them in |
| 4 | Run it three times | A prompt that "worked once" scores 0/3 against a Regex |
| 5 | Just ask for JSON | `JsonException` on a "JSON" answer (fences, preambles) |
| 6 | JSON mode | Parses fine, but missing fields silently become `null` and `Severity.Low` |
| 7 | Structured outputs | Strict schema, typed `review.Severity`, merge blocked in plain C# |
| 8 | Prompt injection | A code comment tries to approve its own SQL injection |

`LiveBuild/Start` → `LiveBuild/Final`: TicketSorter, the 5-minute live build (the instructor types 3 edits; you can replay it).

`groq-json.http` shows the three levels of "give me JSON" as raw HTTP requests.

## AssignmentStarter

Runs as-is with the ticket-triage example. Work through TODO 1–5, then fill in `NOTES.md`.

## Notes

- Strict `json_schema` on Groq works only on some models (the gpt-oss family at the time of writing). Check <https://console.groq.com/docs/structured-outputs> before class.
- LLM output varies run to run. If a demo "fails to fail", that's a teaching moment, not a bug.
