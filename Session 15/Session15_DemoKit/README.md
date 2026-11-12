# Session 15 · Packaging Your App: An Introduction to .NET Aspire · demo kit (from scratch)

Tonight we build ONE solution, `RagApp`, from an empty folder, in five builds. The live-builds script has every command,
click and line of code. This kit is the safety net and the homework, not the starting point.

| Folder | What it is |
| --- | --- |
| `checkpoints/after-build-1/RagApp/` | RagApi on its own: GET /ask (Groq) |
| `checkpoints/after-build-2/RagApp/` | + AppHost and ServiceDefaults: one Run, the dashboard, a secret, health, traces with the chat span |
| `checkpoints/after-build-3/RagApp/` | + Postgres/pgvector as a resource, Gemini embeddings, POST /notes, WaitFor |
| `checkpoints/after-build-4/RagApp/` | + RAG: /ask retrieves the closest notes and answers from them |
| `checkpoints/after-build-5/RagApp/` | + RagEvals as a resource (Start button), Groq → Gemini from the AppHost |
| `AssignmentStarter/RagApp/` | RagApi (Build 1) with TODOs + `RUN.md` |
| `AssignmentSolution/RagApp/` | One finished answer (= after-build-5) |

**If a live step fails:** stop the AppHost, File → Open → the checkpoint's `RagApp.sln`, run the AppHost (https profile).
The checkpoints share the UserSecretsId `agentic-sprint-s15`, so keys saved once (in rehearsal) work in all of them.

**You need:** .NET 10 SDK · the Aspire CLI 13.6 (`aspire --version`) · `dotnet new install Aspire.ProjectTemplates::13.6.4` ·
Docker Desktop · `dotnet dev-certs https --trust` · a Groq key and a Gemini key · pre-pull `docker pull pgvector/pgvector:pg17`.

**Packages:** Microsoft.Extensions.AI + .OpenAI 10.11.0 · CommunityToolkit.VectorData.PgVector 1.0.2 (Session 5's connector) · Aspire.AppHost.Sdk / Aspire.Hosting.PostgreSQL 13.6.4
· ServiceDefaults (template): Microsoft.Extensions.Http.Resilience / ServiceDiscovery 10.11.0, OpenTelemetry 1.20.1.

**Models:** chat openai/gpt-oss-120b (Groq) or gemini-3.8-flash (Gemini) · embeddings gemini-embedding-2 (Gemini, 3072 dimensions).

**Privacy:** in Development the AppHost sets `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true`: prompts, notes and
answers are in your traces on the dashboard. Fine on a laptop; never let production inherit it.
