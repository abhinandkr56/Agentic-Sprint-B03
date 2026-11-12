# Session 09 assignment · my API, now an MCP client (example answer)

**MCP server I connected:** Microsoft Learn · `https://learn.microsoft.com/api/mcp` · plus (S1) Session 8's OrdersServer
**Transport:** Streamable HTTP (Learn) · stdio (OrdersServer)   **Protocol version:** (your run: printed at startup)
**Chat model:** Groq openai/gpt-oss-120b

> The C# was verified against the real MCP C# SDK 2.2.0 built from source, a local stand-in for Learn's server and a
> rule-based stand-in for the model. **What the real model calls, and the real token counts, come from YOUR run.**

## What /chat can call now (GET /)

| Tool | Where it lives | Reviewed? |
| --- | --- | --- |
| GetOnCall | local C# (Session 7) | yes, I wrote it |
| SearchNotes | local C# (Session 7, S1) | yes, I wrote it |
| microsoft_docs_search | Learn MCP server (remote) | yes: read-only search of public docs |
| GetOrder | OrdersServer MCP (local, stdio) | not yet → wrapped in ApprovalRequiredAIFunction (S2) |

Left out on purpose: `microsoft_docs_fetch` (returns whole articles: lots of tokens) and `microsoft_code_sample_search` (not needed for these prompts).

## My prompts

| # | Kind | Prompt | ToolCalls (expected) | Answer (short) | Right? |
| --- | --- | --- | --- | --- | --- |
| 1 | MCP tool | How do I add health checks to an ASP.NET Core app? | microsoft_docs_search(...) | AddHealthChecks + MapHealthChecks, with a learn link | (your run) |
| 2 | Local tool | Who is on call for payments right now? | GetOnCall(payments) | Priya Nair, until 9 am | (your run) |
| 3 | Both | Our API keeps returning 429. What's our limit, and how do I add rate limiting in ASP.NET Core? | SearchNotes(...) + microsoft_docs_search(...) | 100 req/min [1]; AddRateLimiter… + link | (your run) |
| 4 | No tool | What does MCP stand for, in one sentence? | (none) | Model Context Protocol… | (your run) |
| S1 | Our server | Where is my refund for order 4411? | NEEDS APPROVAL: GetOrder(4411) | (empty: waiting for a human) | Yes: the gate worked |

## Trust check

- Learn is remote: it can't see my disk or environment, but it sees every query. I don't send customer data in queries.
- OrdersServer is local and runs as me: started with `InheritEnvironmentVariables = false`, so it never sees `GROQ_API_KEY`.
- Unreviewed tools need approval. Annotations (`readOnlyHint`) are the server's claim, not a guarantee.

## Reflections

1. One docs question cost far more input tokens than a GetOnCall question: the search returns long excerpts, and every round trip re-sends the whole conversation plus every tool schema.
2. gpt-oss on Groq calls tools one at a time (no parallel tool use), so prompt 3 takes an extra round trip.
3. Azure DevOps (official MCP server exists) for build status; our Postgres through read-only views only.
4. S1 + S2 above.
