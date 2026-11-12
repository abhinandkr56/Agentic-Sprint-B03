# Session 08 assignment · exploring an MCP server (example answer)

**Server explored:** Microsoft Learn MCP · `https://learn.microsoft.com/api/mcp`
**Transport:** Streamable HTTP · **Auth needed?** No: no key, no sign-up
**Server name + version / protocol:** fill in from your own run (the prep machine could not reach learn.microsoft.com)

> The tool names and inputs below come from Microsoft's documentation and its GitHub repo (October 2026).
> Microsoft says the tool list can change at any time, which is exactly why clients call `tools/list` on every connect.
> **Check them against your own Inspector run.**

## Tools

| Tool | What it does | Inputs | What came back | When should a model use it? |
| --- | --- | --- | --- | --- |
| `microsoft_docs_search` | Semantic search over official Microsoft documentation; returns short excerpts with titles and URLs | `query`: string, required | (your run) A handful of excerpts with learn.microsoft.com links | Any "how do I… in .NET / Azure / Microsoft 365" question, before answering from memory |
| `microsoft_docs_fetch` | Fetches one Microsoft Learn page and returns it as Markdown | `url`: string, required | (your run) The whole article as Markdown: long | When a search excerpt isn't enough and it needs the full page (step-by-step guides) |
| `microsoft_code_sample_search` | Searches official Microsoft/Azure code samples | `query`: string, required; `language`: string, optional | (your run) Code snippets with their source links | When the user wants working code, not prose |

## Resources

Not offered (per the docs). Learn's server is tools-only: everything it knows is behind a search, so there is no fixed list of documents to expose.

## Prompts

Not offered.

## Compared with the "everything" test server (stretch S1, verified on the prep machine)

| | Microsoft Learn | everything (2026.8.31) |
| --- | --- | --- |
| Transport | Streamable HTTP (remote) | stdio (local process) |
| Tools | 3 | 14 |
| Resources | none | 7 (+ 2 templates) |
| Prompts | none | 4 |
| Protocol | (your run) | 2025-11-25 (its `server/discover` returns "Method not found") |

## Trust check

- Learn's tools only read public documentation. Nothing writes. Annotations: (your run; print them with S3).
- It runs on Microsoft's servers, not your machine: it can't see your files or environment variables. What it CAN see is every query you send it, so don't paste secrets or customer data into a search.
- The everything server is the opposite: a local process with `get-env`, which returns its environment variables. Started with the C# SDK's defaults it inherits ALL of yours (Session 9 shows it live).

## Reflections

1. The descriptions are long and written FOR a model ("use this when…"). They're prompts, like Session 7's `[Description]`.
2. "How do I add health checks to ASP.NET Core?" → `microsoft_docs_search`, then maybe `microsoft_docs_fetch` on the best hit.
3. GitHub (official server exists), Azure DevOps, our Postgres (read-only views only). Session 9 connects one to MyAiApi.
4. S1 above. S4: `?maxTokenBudget=2000` caps how much text search results return (experimental).
