# Session 09 · Using MCP Tools from Your .NET App · demo kit

| Folder | Idea | Run |
| --- | --- | --- |
| `1-McpTools/` | Learn's MCP tools handed to the model: it picks the tool and the query | `dotnet run` (GROQ_API_KEY) |
| `2-OrdersMcp/` | Session 7's GetOrder, now in Session 8's server: same answer, another process | `dotnet run` (GROQ_API_KEY) |
| `3-TrustBoundary/` | What a connected server can see and say: your env vars, its instructions, its claims | `dotnet run` (Node 22.19+) |
| `4-McpApi/` | Session 7's /chat becomes an MCP client: Learn + OrdersServer + a gate | `dotnet run` → `McpApi.http` |
| `OrdersServer/` | Session 8's Build 5 Final, unchanged: Builds 2 and 4 start it for you | (started by the clients) |
| `AssignmentStarter/` | Your Session 7 MyAiApi + TODOs: connect it to an MCP server | `dotnet run` → `MyAiApi.http` |
| `AssignmentSolution/` | One finished answer (Learn + OrdersServer + the gate) | `dotnet run` |

Every build has `Start/` (compiles and runs as-is) and `Final/` (where the edits end up).
Keep the folder layout: Builds 2 and 4 find the server at `../../OrdersServer`, the assignment at `../OrdersServer`.

**You need:** .NET 8 SDK or later · `GROQ_API_KEY` (Builds 1, 2, 4, assignment) · internet access to `learn.microsoft.com`
(Builds 1, 4, assignment) · Node 22.19+ (`npx`) for Build 3 · for the assignment, Session 7's Postgres + Ollama `all-minilm`.

**The model changed:** Groq shut down `llama-3.3-70b-versatile` for free and developer accounts on 16 Aug 2026.
Everything here uses **`openai/gpt-oss-120b`** (Groq's recommended replacement). It calls tools one at a time (no parallel calls).
In your own MyAiApi, change `AI:Groq:Model` in appsettings.json. Gemini's `gemini-3.5-flash` now routes to `gemini-3.6-flash`.

**Packages (same as Sessions 7–8):** `Microsoft.Extensions.AI` 10.10.0 · `Microsoft.Extensions.AI.OpenAI` 10.10.1 ·
`ModelContextProtocol` 2.2.0 · OrdersServer also `Microsoft.Extensions.Hosting` 10.0.10 · the assignment also
`CommunityToolkit.VectorData.InMemory` / `.PgVector` 1.0.1.

**Pinned server version:** `@modelcontextprotocol/server-everything@2026.8.31` (Build 3), the same as Session 8.
