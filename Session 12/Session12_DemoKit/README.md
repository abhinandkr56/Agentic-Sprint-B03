# Session 12 · Keeping a Human in the Loop · demo kit

| Folder | Idea | Run |
| --- | --- | --- |
| `1-ApproveRefund/` | Some actions need a person first: `ApprovalRequiredAIFunction` pauses the run; you answer in the SAME session | `dotnet run` (GROQ_API_KEY) |
| `2-RememberMe/` | Remembering context: one session per conversation; save it to `session.json` and it survives a restart; `/forget` | `dotnet run` (run it twice) |
| `3-HumanCheckpoint/` | Session 11's writer → reviewer, plus a person right before the final action (`SendReply`); "no" + a note; an audit log | `dotnet run` |
| `4-WorkflowCheckpoint/` | Session 11's workflow (writer → reviewer) + a person who approves its RESULT before SendReply; a note re-runs the whole workflow in the same session | `dotnet run` |
| `5-ApprovalApi/` | The assignment, live: replies wait at GET /approvals; POST /approvals/{id} approves, rejects, or sends back with a note | `dotnet run` → `ApprovalApi.http` |
| `OrdersServer/` | Session 8's Build 5 Final, unchanged: the assignment starts it for you | (started by the assignment) |
| `AssignmentStarter/` | Your Session 11 MyAiApi + TODOs: an approval checkpoint before SendReply | `dotnet run` → `MyAiApi.http` |
| `AssignmentSolution/` | One finished answer (+ reject with a note → rewrite, + approvals.log) | `dotnet run` |

Every build has `Start/` (compiles and runs as-is) and `Final/` (where the edits end up).
Keep the folder layout: the assignment finds the MCP server at `../OrdersServer`.

**You need:** .NET 8 SDK or later · `GROQ_API_KEY` · for the assignment, Session 10's setup (Postgres + Ollama
`all-minilm`, internet access to `learn.microsoft.com`).

**Packages:** `Microsoft.Agents.AI` 1.26.0 (was 1.25.0: bumped) · `Microsoft.Agents.AI.Workflows` 1.26.0 (Build 4 + assignment) ·
`Microsoft.Extensions.AI` 10.10.0 · `Microsoft.Extensions.AI.OpenAI` 10.10.1 · the assignment also `ModelContextProtocol` 2.2.0,
`CommunityToolkit.VectorData.InMemory` / `.PgVector` 1.0.1 · OrdersServer `ModelContextProtocol` 2.2.0 + `Microsoft.Extensions.Hosting` 10.0.10.

**Model:** Groq **`openai/gpt-oss-120b`** for every agent. `ApprovalRequiredAIFunction`, `ToolApprovalRequestContent` and
`CreateResponse(approved, reason)` come from Microsoft.Extensions.AI; the Agent Framework stores the pending request in the
session, so **an approval only counts in the session that asked for it**.

**Files the builds write** (in the folder you run from): `2-RememberMe` → `session.json` (the conversation, in plain text:
treat it as personal data) · `3-HumanCheckpoint` → `approvals.log`. Delete them to start fresh.
