# Session 11 · Multiple Agents Working Together · demo kit

| Folder | Idea | Run |
| --- | --- | --- |
| `1-WriterReviewer/` | Why split: a second agent with its own job and a clean context catches what the writer approves in itself | `dotnet run` (GROQ_API_KEY) |
| `2-ReviewLoop/` | Passing information cleanly: a typed verdict (`RunAsync<Review>`), then a fix loop our code controls | `dotnet run` |
| `3-ReviewWorkflow/` | The same two agents as a MAF sequential workflow; full conversation vs only the last reply | `dotnet run` |
| `4-ReviewerAsTool/` | `agent.AsAIFunction()`: the WRITER decides whether, and with what, to call the reviewer | `dotnet run` |
| `5-ReviewedReplyApi/` | The assignment, live: POST /reply → writer with tools → reviewer with the tools' FACTS → one fix round | `dotnet run` → `ReviewedReplyApi.http` |
| `OrdersServer/` | Session 8's Build 5 Final, unchanged: the assignment starts it for you | (started by the assignment) |
| `AssignmentStarter/` | Your Session 10 MyAiApi + TODOs: add POST /agent/reviewed-reply | `dotnet run` → `MyAiApi.http` |
| `AssignmentSolution/` | One finished answer (+ a reviewer on another provider, + tokens per round) | `dotnet run` |

Every build has `Start/` (compiles and runs as-is) and `Final/` (where the edits end up).
Keep the folder layout: the assignment finds the MCP server at `../OrdersServer`.

**You need:** .NET 8 SDK or later · `GROQ_API_KEY` · for the assignment, Session 10's setup (Postgres + Ollama
`all-minilm`, internet access to `learn.microsoft.com`) · for assignment stretch S2, `GEMINI_API_KEY`.

**Packages:** `Microsoft.Agents.AI` 1.25.0 · **new tonight (Build 3 only):** `Microsoft.Agents.AI.Workflows` 1.25.0 ·
`Microsoft.Extensions.AI` 10.10.0 · `Microsoft.Extensions.AI.OpenAI` 10.10.1 · the assignment also `ModelContextProtocol` 2.2.0,
`CommunityToolkit.VectorData.InMemory` / `.PgVector` 1.0.1 · OrdersServer `ModelContextProtocol` 2.2.0 + `Microsoft.Extensions.Hosting` 10.0.10.

**Model:** Groq **`openai/gpt-oss-120b`** for both agents. Its JSON-schema structured output is what makes `RunAsync<Review>` work;
Groq's docs say structured outputs can't be combined with tool calls, which is why only the REVIEWER (no tools) returns a typed verdict.
