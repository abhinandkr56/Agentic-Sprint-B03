# Session 15 assignment · Your RAG app, one Run

**Goal (syllabus):** put your API and its vector store in a basic Aspire AppHost and run the whole thing with one command.
You watched it built from an empty folder. Now build it yourself. `RagApi/` is Build 1's API (one chat endpoint) with TODOs;
everything else you create with the commands below. Stuck? `../../AssignmentSolution/RagApp/` is one finished answer.

## Before you start
- .NET 10 SDK, Docker Desktop running, the Aspire CLI 13.6 (`aspire --version`), `dotnet dev-certs https --trust`
- A Groq key and a Gemini key (Gemini does the embeddings). You type them into the dashboard once.
- `dotnet new install Aspire.ProjectTemplates::13.6.4`

## Steps (run the commands in the `RagApp/` folder)
1. **Run RagApi on its own** (Rider: the RagApi `http` configuration, or `dotnet run --project RagApi`) and call `GET /ask` from `RagApi/RagApi.http`.
   It needs `GROQ_API_KEY` in its environment for this step only.
2. **Add Aspire:**
   ```
   dotnet new aspire-apphost -n AppHost
   dotnet new aspire-servicedefaults -n ServiceDefaults
   dotnet sln add AppHost ServiceDefaults
   dotnet add AppHost reference RagApi
   dotnet add RagApi reference ServiceDefaults
   ```
   Then TODO 2 in `RagApi/Program.cs`.
3. **AppHost.cs:** a `groq-key` parameter, and RagApi as a resource with the key and `WithHttpHealthCheck("/health")`. Run the AppHost.
   TODO 3: make the chat call show up as a span (and add `.AddSource("Experimental.Microsoft.Extensions.AI")` in `ServiceDefaults/Extensions.cs`).
4. **The vector store:** `dotnet add AppHost package Aspire.Hosting.PostgreSQL --version 13.6.4`, then in AppHost.cs: a `gemini-key` parameter,
   Postgres with the `pgvector/pgvector:pg17` image and a data volume, a `vectors` database, `WithReference` + `WaitFor`.
5. **RagApi:** `dotnet add RagApi package CommunityToolkit.VectorData.PgVector --version 1.0.2`, the `Embeddings` block in appsettings.json, then TODO 5–7.
6. **Run everything with one Run** (or `aspire run` in `AppHost/`). Save three notes, ask three questions.

## Hand in (copy this into your post)

**The one command I run:** `________________`  (from the folder: ________)

| Resource | Type (Project / Container / Parameter) | State | Health |
| --- | --- | --- | --- |
| | | | |

1. What did RagApi wait for, and how did the dashboard show it?
2. Which environment variables did the AppHost give RagApi? (ConnectionStrings__…, GROQ_API_KEY masked?)
3. Ask a question your notes answer, and one they don't. Paste both answers.
4. Stop and run again: are the notes still there? Why?
5. Stretch goals I tried, and what happened:
