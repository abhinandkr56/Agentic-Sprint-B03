# Session 06 demo kit · Your First RAG Assistant

Agentic Sprint for .NET Developers · Milestone 2, Session 6 of 18

RAG in one sentence: **look it up, then answer with what you found.** Five small programs, each built live from a `Start` folder; every `Final` folder is the finished version, so you can replay any build yourself. Same four packages as Session 5, so if you restored Session 5's kit there's nothing new to download.

| Package | Version | Why |
| --- | --- | --- |
| `Microsoft.Extensions.AI` | 10.10.0 | `IChatClient` (Session 3) and `IEmbeddingGenerator` (Session 5) |
| `Microsoft.Extensions.AI.OpenAI` | 10.10.1 | One adapter for every OpenAI-compatible endpoint: Groq, Gemini, Ollama |
| `CommunityToolkit.VectorData.InMemory` | 1.0.1 | The in-memory vector store (Builds 2–4) |
| `CommunityToolkit.VectorData.PgVector` | 1.0.1 | The same API on Postgres + pgvector (Build 5, assignment) |

## Setup

```bash
# 1 · the embedding model (46 MB, already there since Session 5)
ollama pull all-minilm

# 2 · Postgres + pgvector (Build 5 and the assignment only)
docker start pgvector
#   no container yet?  docker run -d --name pgvector -p 5432:5432 -e POSTGRES_PASSWORD=postgres pgvector/pgvector:pg18

# 3 · keys (unchanged since Session 3)
#   PowerShell:  $env:GROQ_API_KEY = "gsk_..."   $env:GEMINI_API_KEY = "..."
#   bash / zsh:  export GROQ_API_KEY="gsk_..."   export GEMINI_API_KEY="..."
```

Then `dotnet restore` in any one folder (the packages are cached for the rest).

## The five builds

| # | Folder | One idea | The moment |
| --- | --- | --- | --- |
| 1 | `1-NoRag` | The model doesn't know your company | It invents a refund policy. Paste one FAQ by hand: right answer. Paste all ten: right again, and the prompt grows about five times |
| 2 | `2-RagLoop` | RAG = retrieve, augment, generate | Session 5's search finds the 3 closest FAQs, they go into the prompt, the answer cites them: `[10]` |
| 3 | `3-Guardrails` | Where RAG goes wrong, and the first fixes | "Monthly plan refund?" gets stretched from an annual-plan FAQ. Rules fix it. A score gate skips the model for "best biryani in Hyderabad". Gemini embeddings find a Hinglish question that all-minilm can't |
| 4 | `4-Chunking` | Documents must be cut into chunks | The whole refund policy as one record: all-minilm only reads its first 256 tokens, so "I raised a dispute with my bank" misses the Chargebacks section. One `Split("## ")` later it's the top hit (0.66) |
| 5 | `5-RagApi` | RAG inside your API | Session 4's AskApi + Session 5's pgvector FAQs: `/ask` returns a grounded answer **and** its sources as JSON. Groq on 5080, Gemini on 5081, same sources |

Run a console build: `cd 2-RagLoop/Final && dotnet run`, type a question, press Enter (empty line to quit). Build 4 reads `policy.md` from its own folder, so run it from there. Run the API: `cd 5-RagApi/Final && dotnet run`, then open <http://localhost:5080/ask?q=where%20is%20my%20refund> or use `RagApi.http`.

## Assignment

`AssignmentStarter/` is your Session 4 `MyAiApi` with your Session 5 `notes` table already connected. `POST /rag` runs, but doesn't look at your notes yet. Five TODOs: start the pieces, check the store matches Session 5, retrieve, augment + generate, then try 5 questions and write down what happened in `QUESTIONS.md`. `AssignmentSolution/` is one finished answer (with stretch S1: no model call when nothing passes the gate).

No `notes` table from Session 5? The starter seeds three sample notes on first run. If the table exists, your notes are left alone.

## Notes

- **Every hit needs a score check.** Vector search always returns the nearest records, even for "best biryani in Hyderabad". `VectorSearchOptions.ScoreThreshold` filters below a minimum score (in the database, for Postgres). 0.3 was measured for all-minilm on short texts; Gemini's scores sit in a different range, so measure again.
- **all-minilm reads at most 256 tokens** (Ollama's `num_ctx` for the model). Longer input is cut silently, not rejected. That's why Build 4 chunks the policy.
- **Register the collection as `VectorStoreCollection<int, Faq>`**, the abstraction, if an endpoint asks DI for that type. `GetCollection` returns `PostgresCollection<int, Faq>`; register that and the endpoint can't find it (`Body was inferred but the method does not allow inferred body parameters`).
- **The connection string** comes from configuration in Build 5 and the assignment (`ConnectionStrings:Vectors` from user-secrets or the `ConnectionStrings__Vectors` env var), with the local Docker one as a fallback. Never put a real password in `appsettings.json`.
- LLM answers vary from run to run and between providers. The retrieval half (which FAQs, which scores) is repeatable; the generation half isn't. That's normal at this stage, and it's what Session 13 (evaluation) is for.
