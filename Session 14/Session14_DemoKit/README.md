# Session 14 · A First Look at Observability · demo kit

| Folder | Idea | Run |
| --- | --- | --- |
| `1-FirstSpan/` | One AI call, seen from inside: a span with the model, the tokens, the time (and, if you let it, the prompt) | `dotnet run` (GROQ_API_KEY) |
| `2-OneTrace/` | A trace is a tree: one ticket, two AI calls, one parent span, in the Aspire dashboard | dashboard + `dotnet run` |
| `3-AgentTrace/` | What the agent actually did: invoke_agent → chat → execute_tool → chat, and a failure the answer hides | dashboard + `dotnet run` |
| `4-TracedApi/` | The assignment, live: your MyAiApi traced (requests, HTTP calls, AI calls, what /rag found) | dashboard + MyAiApi |
| `5-EvalTraces/` | Session 13's evals and MyAiApi in ONE trace across two processes; failing cases turn red | 4-TracedApi/Final + `dotnet run` |
| `6-Bonus/` | Show, don't type: metrics (token usage, durations) from the same calls | dashboard + `dotnet run` |
| `OrdersServer/` | Session 8's MCP server, unchanged: MyAiApi starts it | (started by MyAiApi) |
| `AssignmentStarter/` | Your MyAiApi + OpenTelemetry packages + TODOs, and `TRACES.md` | dashboard + `dotnet run` |
| `AssignmentSolution/` | One finished answer: /rag + /agent/reply traced, metrics, content capture only in Development | dashboard + `dotnet run` |

Every build has `Start/` (compiles and runs as-is) and `Final/` (where the edits end up). Keep the folder layout:
the MyAiApi copies find the MCP server at `../../OrdersServer` (builds) or `../OrdersServer` (assignment).

**The dashboard (one command, needs Docker):**

```bash
docker run --rm -it -d --name aspire-dashboard -p 18888:18888 -p 4317:18889 -p 4318:18890 \
  -e ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/aspire/dashboard:latest
```

Open http://localhost:18888. Port 4317 is OTLP over gRPC: `.AddOtlpExporter()`'s default. The anonymous flag skips the
login token on your own laptop only (without it: `docker logs aspire-dashboard` prints a login link). Telemetry lives in
memory: stop the container and it's gone. **No Docker?** Use `.AddConsoleExporter()` instead of `.AddOtlpExporter()`.

**You need:** .NET 8 SDK or later · `GROQ_API_KEY` · Docker for the dashboard · for Builds 4–5 and the assignment,
MyAiApi's usual setup (Postgres + Ollama `all-minilm`, internet for `learn.microsoft.com`).

**Packages (new tonight, all 1.20.1):** `OpenTelemetry` (the SDK) · `OpenTelemetry.Exporter.Console` ·
`OpenTelemetry.Exporter.OpenTelemetryProtocol` · and in MyAiApi `OpenTelemetry.Extensions.Hosting` ·
`OpenTelemetry.Instrumentation.AspNetCore` · `OpenTelemetry.Instrumentation.Http`.
Everything uses `Microsoft.Extensions.AI` + `.OpenAI` **10.11.0** and `Microsoft.Agents.AI` **1.27.0** (new: 30 Oct 2026).
**MyAiApi moves up** from Session 12's versions to these, and its Gemini model to `gemini-3.8-flash`.

**Source names to listen to:** `Experimental.Microsoft.Extensions.AI` (chat, embeddings, tools) ·
`Experimental.Microsoft.Agents.AI` (agents) · your own `ActivitySource` names. A source you don't `AddSource` is silent.

**Privacy:** by default the spans hold metadata (model, tokens, timings, errors), not the prompts. `EnableSensitiveData = true`
(Build 1 EDIT 2) or `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true` puts the full prompts and replies in your traces.
