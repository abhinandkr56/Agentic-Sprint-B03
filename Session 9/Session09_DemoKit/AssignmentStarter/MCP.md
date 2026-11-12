# Session 09 assignment · my API, now an MCP client

**Name:**
**MCP server I connected:** (Microsoft Learn `https://learn.microsoft.com/api/mcp`, or the one I explored in Session 8)
**Transport:** (Streamable HTTP / stdio)   **Protocol version it negotiated:**
**Chat model:** (Groq openai/gpt-oss-120b / Gemini / Ollama …)

## What /chat can call now (GET /)

| Tool | Where it lives (local C# / which MCP server) | Did I review it? |
| --- | --- | --- |
| GetOnCall | local (Session 7) | yes, I wrote it |
| | | |

## My prompts

| # | Kind | Prompt | ToolCalls | Answer (short) | Right? |
| --- | --- | --- | --- | --- | --- |
| 1 | Should use the MCP tool | | | | |
| 2 | Should use a local tool | | | | |
| 3 | Should use both | | | | |
| 4 | Should use no tool | | | | |

## Trust check (Build 3)

- Is my MCP server remote or local? What can it see: my files, my environment, only what I send it?
- Which of its tools did I hand to the model, and which did I leave out? Why?
- Would I let the model call each of them without asking me? (If not: stretch S2.)

## Reflections (2–3 lines each)

1. How many tokens did one docs question cost (the `tokens in` line or Groq's dashboard)? What made it big?
2. Did the model ever skip the MCP tool when it should have used it, or call it when it shouldn't? What did you change?
3. Which system at work would you want behind an MCP server for this API? Does a server for it already exist?
4. Stretch goals I tried, and what happened:
