# Session 05 demo kit · Vector Search & Microsoft.Extensions.VectorData Basics

Agentic Sprint for .NET Developers · Milestone 2, Session 5 of 18

Demo-first again: five small programs, each built live in a few minutes from a `Start` folder. Every `Final` folder is the finished version, so you can replay any build yourself. All seven projects share one `.csproj`, so one restore covers the whole kit.

| Package | Version | Why |
| --- | --- | --- |
| `Microsoft.Extensions.AI` | 10.10.0 | `IEmbeddingGenerator`, plus `IChatClient` from Session 3 |
| `Microsoft.Extensions.AI.OpenAI` | 10.10.1 | One adapter for any OpenAI-compatible endpoint: Ollama and Gemini tonight |
| `CommunityToolkit.VectorData.InMemory` | 1.0.1 | A vector store in memory (Build 3) |
| `CommunityToolkit.VectorData.PgVector` | 1.0.1 | The same API on Postgres + pgvector (Builds 4–5, assignment) |

Both CommunityToolkit packages bring in `Microsoft.Extensions.VectorData.Abstractions` (the interfaces: `VectorStore`, `VectorStoreCollection`, the `[VectorStore…]` attributes). The old `Microsoft.SemanticKernel.Connectors.*` packages are the previous home of the same connectors. Use the CommunityToolkit ones.

## Setup

### 1. The embedding model (46 MB)

Ollama is already installed from Session 4.

```bash
ollama pull all-minilm
ollama list
```

### 2. Postgres with pgvector, in Docker (about 150 MB download)

```bash
docker run -d --name pgvector -p 5432:5432 -e POSTGRES_PASSWORD=postgres pgvector/pgvector:pg18
docker ps
```

Already running a local Postgres on 5432? Use `-p 5433:5432` and add `Port=5433;` to the connection string. No Docker? Builds 1–3 need none, and any Postgres with the `vector` extension works for the rest.

### 3. Keys (unchanged)

```bash
# PowerShell
$env:GROQ_API_KEY   = "gsk_..."
$env:GEMINI_API_KEY = "..."
# bash / zsh
export GROQ_API_KEY="gsk_..."
export GEMINI_API_KEY="..."
```

Then run `dotnet restore` in any one folder (the packages are cached for the rest).

## The five builds

| # | Folder | One idea | The moment |
| --- | --- | --- | --- |
| 1 | `1-Embeddings` | An embedding is meaning as numbers | Groq has no embeddings (404). Ollama returns 384 numbers. Two sentences with no shared words score close, and "money was deducted" vs "was **not** deducted" score 0.99 |
| 2 | `2-TinySearch` | Vector search is embed, compare, sort | A search engine in a for-loop. Then "best biryani in Hyderabad" still gets three answers |
| 3 | `3-VectorStore` | Microsoft.Extensions.VectorData does the loop for you | One record class with attributes, `UpsertAsync`, `SearchAsync`. Same scores as Build 2. Then a `Filter` |
| 4 | `4-Postgres` | Same API, a real database | One line swaps memory for pgvector. The ranking stays, but every score flips (0.37 → 0.63): Postgres defaults to distance. Then it's just a table in `psql` |
| 5 | `5-SwapModel` | The model and its vectors are a pair | Hinglish fails on all-minilm. Three strings switch to Gemini... and Postgres refuses: `expected 384 dimensions, not 3072`. A new collection fixes it |

Run any build: `cd 3-VectorStore/Final && dotnet run`, then type a search and press Enter (empty line to quit).

## Assignment

`AssignmentStarter/` (MyVectorSearch) runs as-is in memory with 2 sample notes and has 5 TODOs: your own 8–10 texts in pgvector and 3 searches, recorded in `SEARCHES.md`. `AssignmentSolution/` is one finished answer, with stretch S1 (filter) and S2 (no good match).

## Notes

- Don't name a project `Pgvector` (or `PgVector`): it clashes with the `Pgvector` library's assembly name and fails at runtime with `TypeLoadException: Could not load type 'Npgsql.VectorExtensions'`. That's why Build 4's folder is `4-Postgres`.
- `[VectorStoreVector(384)]` must match the model: all-minilm returns 384 numbers, gemini-embedding-001 returns 3072. A different model means a new collection and re-embedding everything.
- Postgres scores are cosine **distance** by default (0 = identical); the in-memory store gives cosine **similarity** (1 = identical). Set `DistanceFunction = DistanceFunction.CosineSimilarity` to make them agree.
- `EnsureCollectionExistsAsync()` creates the `vector` extension and the table for you. Without an index, Postgres compares the query with every row: fine for 10 rows, slow for 10 million (that is what an HNSW index is for).
- Scores depend on the model. A threshold like 0.3 that works for all-minilm means nothing for Gemini.
