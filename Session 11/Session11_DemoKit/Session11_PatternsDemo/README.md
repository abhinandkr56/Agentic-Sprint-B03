# Session 11 · Five orchestration patterns · demo kit

One small console app per pattern in the Microsoft Agent Framework. Same support-desk example as the session (Asha, orders 4411–4413).

| Folder | Pattern | In one line | Run |
| --- | --- | --- | --- |
| `1-Sequential/` | Sequential | A → B → C: an assembly line (triage → writer → reviewer) | `dotnet run` |
| `2-Concurrent/` | Concurrent | Same input to several agents at once (tone ‖ facts ‖ policy reviewers) | `dotnet run` |
| `3-Handoff/` | Handoff | A front desk passes the conversation to the right specialist | `dotnet run` |
| `4-GroupChat/` | Group chat | Agents take turns; a manager picks who speaks and when to stop | `dotnet run` |
| `5-Magentic/` | Magentic | A manager agent plans, delegates and decides when it's done | `dotnet run` |

**You need:** .NET 8 SDK or later · `GROQ_API_KEY` · packages (already in each csproj): `Microsoft.Agents.AI` 1.25.0,
`Microsoft.Agents.AI.Workflows` 1.25.0, `Microsoft.Extensions.AI` 10.10.0, `Microsoft.Extensions.AI.OpenAI` 10.10.1.

**Model:** Groq `openai/gpt-oss-120b` for every agent. Full explanation: `session-11-orchestration-patterns`.
