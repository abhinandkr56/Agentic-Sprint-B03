# Session 13 · Evaluating Whether Your AI App Actually Works · demo kit

| Folder | Idea | Run |
| --- | --- | --- |
| `1-LookedRightOnce/` | One answer is an anecdote: the same question 5 times, a check, a pass RATE, and what it costs | `dotnet run` (GROQ_API_KEY) |
| `2-TestCases/` | A small set of test cases with expected answers: must say · must call (the tool) · must never say | `dotnet run` |
| `3-AiJudge/` | A string can't read: an LLM judge (Equivalence, Relevance, Groundedness) from Microsoft.Extensions.AI.Evaluation | `dotnet run` |
| `4-EvalReport/` | Keep the results: on disk, cached, as an HTML report. The second run is instant | `dotnet run` (twice) |
| `5-RagEvals/` | The assignment, live: your RAG assistant tested over HTTP: retrieval, generation, EVALS.md, an exit code | MyAiApi + `dotnet run` |
| `6-Bonus/` | Show, don't type: F1 (no LLM) and the Agent Framework's `agent.EvaluateAsync` | `dotnet run` |
| `MyAiApi/` | Session 12's finished MyAiApi, unchanged: Build 5 and the assignment test it | `dotnet run` (Session 10 setup) |
| `OrdersServer/` | Session 8's MCP server, unchanged: MyAiApi starts it | (started by MyAiApi) |
| `AssignmentStarter/` | An `Evals` console app with 2 cases and TODOs: test YOUR MyAiApi | `dotnet run` (MyAiApi running) |
| `AssignmentSolution/` | One finished answer: 8 RAG cases + 2 agent cases from APPROVALS.md, EVALS.md, exit code | `dotnet run` |

Every build has `Start/` (compiles and runs as-is) and `Final/` (where the edits end up). Keep the folder layout:
MyAiApi finds the MCP server at `../OrdersServer`.

**You need:** .NET 8 SDK or later · `GROQ_API_KEY` · for Build 5 and the assignment, MyAiApi running on port 5080
(Session 10's setup: Postgres + Ollama `all-minilm`, internet for `learn.microsoft.com`).

**Packages (new tonight):** `Microsoft.Extensions.AI.Evaluation.Quality` 10.11.0 (Builds 3–5, assignment) ·
`Microsoft.Extensions.AI.Evaluation.Reporting` 10.11.0 (Build 4) · `Microsoft.Extensions.AI.Evaluation.NLP` 10.11.0-preview.1.26513.7 (bonus only: preview).
The new projects use `Microsoft.Extensions.AI` + `Microsoft.Extensions.AI.OpenAI` **10.11.0** (the Evaluation 10.11.0 packages need it)
and `Microsoft.Agents.AI` 1.26.0. MyAiApi keeps Session 12's versions.

**Model:** Groq **`openai/gpt-oss-120b`** answers AND judges. A judge call is a long prompt (the evaluator's rubric and
examples): on Groq's free tier you may hit the per-minute token limit. The client waits and retries by itself (HTTP 429 +
Retry-After); Build 4's cache makes re-runs free.

**Files the builds write** (in the folder you run from): `4-EvalReport` → `eval-results/` (results + cache) and
`report.html` · `5-RagEvals` → `EVALS.md`. Delete them to start fresh (`eval-results/cache` is what makes re-runs instant).
