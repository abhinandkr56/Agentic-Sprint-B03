# Session 10 · Agent Frameworks in .NET: From Semantic Kernel to MAF · demo kit

| Folder | Idea | Run |
| --- | --- | --- |
| `1-FirstAgent/` | An agent = your IChatClient + instructions + a name, behind `RunAsync` | `dotnet run` (GROQ_API_KEY) |
| `2-AgentMemory/` | No session, no memory. A session = the history the agent resends every turn | `dotnet run` |
| `3-OrderAgent/` | Tools + the loop: email → FindOrders → GetOrder → a drafted reply | `dotnet run` |
| `4-GuardedAgent/` | Middleware: one place that sees (and can block) every tool call, local or MCP | `dotnet run` |
| `5-SupportApi/` | The assignment, live: POST /reply in a Minimal API, a session per conversation | `dotnet run` → `SupportApi.http` |
| `OrdersServer/` | Session 8's Build 5 Final, unchanged: Build 4 and the assignment start it for you | (started by the clients) |
| `AssignmentStarter/` | Your Session 9 MyAiApi + TODOs: add POST /agent/reply | `dotnet run` → `MyAiApi.http` |
| `AssignmentSolution/` | One finished answer (+ sessions and middleware) | `dotnet run` |

Every build has `Start/` (compiles and runs as-is) and `Final/` (where the edits end up).
Keep the folder layout: Build 4 finds the server at `../../OrdersServer`, the assignment at `../OrdersServer`.

**You need:** .NET 8 SDK or later · `GROQ_API_KEY` · for the assignment, Session 9's setup (Postgres + Ollama
`all-minilm`, internet access to `learn.microsoft.com`).

**Packages:** `Microsoft.Agents.AI` 1.25.0 (the Microsoft Agent Framework) · `Microsoft.Extensions.AI` 10.10.0 ·
`Microsoft.Extensions.AI.OpenAI` 10.10.1 · Build 4 and the assignment also `ModelContextProtocol` 2.2.0 · OrdersServer
`Microsoft.Extensions.Hosting` 10.0.10 · the assignment `CommunityToolkit.VectorData.InMemory` / `.PgVector` 1.0.1.
Microsoft.Agents.AI 1.25.0 needs Microsoft.Extensions.AI 10.10.0 or later: the versions above already match.

**Model:** Groq **`openai/gpt-oss-120b`** (it calls tools one at a time: watch the steps).
