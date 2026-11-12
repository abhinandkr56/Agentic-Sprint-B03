# Session 08 · Agentic RAG + Understanding the Model Context Protocol · demo kit

| Folder | Idea | Run |
| --- | --- | --- |
| `1-AgenticRag/` | Session 7's skipped build: Session 6's FAQ search becomes a tool the model decides to call | `dotnet run` (Ollama all-minilm + GROQ_API_KEY) |
| `2-Inspector/` | Meet an MCP server without code: Microsoft Learn (HTTP) and "everything" (stdio) | `npx @modelcontextprotocol/inspector` |
| `3-RawMcp/` | MCP is JSON-RPC over a pipe: discover → initialize → tools/list → tools/call, by hand | `dotnet run` |
| `4-McpExplorer/` | The C# SDK: one client, any server (Learn over HTTP, everything over stdio) | `dotnet run` · `dotnet run -- everything` |
| `5-OrdersServer/` | Session 7's GetOrder as an MCP server (+ a resource). stdout is sacred | `cd 5-OrdersServer/Final` then `npx @modelcontextprotocol/inspector dotnet run` |
| `AssignmentStarter/` | Explore one MCP server: Inspector + explorer + EXPLORATION.md | `dotnet run` |
| `AssignmentSolution/` | One finished answer | `dotnet run` · `dotnet run -- everything` |

Every build has `Start/` (compiles and runs as-is) and `Final/` (where the edits end up).

**You need:** .NET 8 SDK or later · for Build 1: Ollama with `all-minilm` (Session 5) and `GROQ_API_KEY` (the only key tonight) ·
for Builds 2–5: Node 22.19+ (`npx`, for the Inspector and the "everything" server) and internet access to `learn.microsoft.com`.

**Packages:** Build 1: Microsoft.Extensions.AI 10.10.0, Microsoft.Extensions.AI.OpenAI 10.10.1, CommunityToolkit.VectorData.InMemory 1.0.1
(the same as Session 7) · Builds 4–5 and the assignment: `ModelContextProtocol` 2.2.0 · Build 5 also `Microsoft.Extensions.Hosting` 10.0.10 ·
Build 3 uses no packages at all.

**Pinned server version:** `@modelcontextprotocol/server-everything@2026.8.31`, the version these builds were checked against.
Pinning is also a security habit (Session 9): `npx -y package` without a version runs whatever was published last.
