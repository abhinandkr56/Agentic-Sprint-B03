# Session 07 demo kit · Function & Tool Calling Basics

Agentic Sprint for .NET Developers · Milestone 3 (Tools & MCP), Session 7 of 18

Tonight the model stops just *talking* and starts *asking us to run C#*. Five small programs, each built live from a `Start` folder; every `Final` folder is the finished version, so you can replay any build yourself. **Same four packages as Sessions 5 and 6**: if you restored those kits, there's nothing new to download.

| Package | Version | Why |
| --- | --- | --- |
| `Microsoft.Extensions.AI` | 10.10.0 | `IChatClient`, `AIFunctionFactory`, `UseFunctionInvocation()`, `ApprovalRequiredAIFunction` |
| `Microsoft.Extensions.AI.OpenAI` | 10.10.1 | One adapter for every OpenAI-compatible endpoint: Groq, Gemini, Ollama |
| `CommunityToolkit.VectorData.InMemory` | 1.0.1 | Session 6's FAQ store, in memory (Build 3) |
| `CommunityToolkit.VectorData.PgVector` | 1.0.1 | Session 6's FAQ store on Postgres (Build 5, assignment) |

## Setup

```bash
# 1 · the embedding model (Builds 3, 5 and the assignment) and the tiny local chat model (Build 4)
ollama pull all-minilm
ollama pull llama3.2:1b          # Session 4's model. `ollama show llama3.2:1b` should list "tools" under Capabilities

# 2 · Postgres + pgvector (Build 5 and the assignment only)
docker start pgvector
#   no container yet?  docker run -d --name pgvector -p 5432:5432 -e POSTGRES_PASSWORD=postgres pgvector/pgvector:pg18

# 3 · keys (unchanged since Session 3)
#   PowerShell:  $env:GROQ_API_KEY = "gsk_..."   $env:GEMINI_API_KEY = "..."
#   bash / zsh:  export GROQ_API_KEY="gsk_..."   export GEMINI_API_KEY="..."
```

## The five builds

| # | Folder | One idea | The moment |
| --- | --- | --- | --- |
| 1 | `1-ToolRequest` | The model doesn't run your code. It *asks* you to | Give it `GetOrder` and the answer is… empty. `FinishReason: tool_calls`, "model asks: GetOrder(orderId, 4411)", and nothing ran. Then we run it ourselves, send the result back and ask again: the whole tool loop, by hand |
| 2 | `2-AutoLoop` | `UseFunctionInvocation()` runs the loop for you | One builder line replaces Build 1's loop. Print the JSON schema: that's ALL the model knows about your method. Add `[Description]`s and watch the schema and the behaviour change: "hi" calls nothing, "orders 4412 and 4413" calls it twice, "my order?" asks for the number |
| 3 | `3-AgenticRag` | Your RAG search becomes a tool | Session 6 searched on every question. Now the model decides: "hi" searches nothing, "dark mode?" searches once, "cheque, and GST invoice?" searches twice, "order 4411 refunded, when's my money?" uses both tools |
| 4 | `4-Showdown` | Same tool, three models, three behaviours | Groq does it cleanly. Gemini through the OpenAI-compatible endpoint is expected to fail on the second round trip (`missing a thought_signature`). The 1B local model may call tools when it shouldn't, or invent arguments |
| 5 | `5-ToolsApi` | Tools inside your API, and a tool that *does* something | Session 6's API gets `GET /chat` with GetOrder + SearchFaqs; the console shows every round trip. Add `CancelOrder`: one vague sentence cancels an order. Wrap it in `ApprovalRequiredAIFunction`: now it stops and asks |

Run a console build: `cd 2-AutoLoop/Final && dotnet run`, type a question, press Enter (empty line to quit). Build 4 runs four fixed prompts: `dotnet run`, `dotnet run -- Gemini`, `dotnet run -- Ollama`. Run the API: `cd 5-ToolsApi/Final && dotnet run`, then open <http://localhost:5080/chat?q=where%20is%20my%20refund%20for%20order%204411> or use `ToolsApi.http`.

## Assignment

Syllabus: *"Give your chat client one simple C# tool (a stub is fine) and get the model to call it correctly on 3 different prompts."*

`AssignmentStarter/` is your Session 6 `MyAiApi` (/ask, /triage, /rag) plus a new `POST /chat` with no tools yet. Five TODOs: write one stub tool, describe it, turn on the loop, pass the tools and report which ones were called, then fill in `PROMPTS.md` (3 prompts that should call it, 1 that shouldn't, 1 with a missing argument). `AssignmentSolution/` is one finished answer: a `GetOnCall(team)` rota stub, plus stretch S1 (`/rag`'s search as a second tool, `SearchNotes`) and S4 (schemas printed at startup).

## Notes

- **The model never runs anything.** It returns a *request* (`FunctionCallContent`: a name and JSON arguments). Your code runs the method and sends the result back. `UseFunctionInvocation()` (FunctionInvokingChatClient) just automates that loop, up to 40 round trips per request by default.
- **Name your tools.** In top-level statements a local function's real name is mangled (`_Main_g_GetOrder_0_0`), so the kit always passes the name: `AIFunctionFactory.Create(GetOrder, "GetOrder")`.
- **Descriptions are the prompt.** The method's `[Description]` and each parameter's `[Description]` go into the JSON schema the model reads. "When to use it" and "what NOT to do" belong there.
- **Tools cost tokens, on every request.** Every tool's schema is sent with every call, and each tool call adds a round trip. Watch the "tokens in" numbers.
- **Middleware that rebuilds messages from `.Text` deletes tool calls.** Session 3's masking middleware did that; Build 5's version only touches user messages.
- **Gemini 3 models + tools via the OpenAI-compatible endpoint:** Gemini requires its `thought_signature` to be sent back with each tool call; the OpenAI adapter in Microsoft.Extensions.AI 10.10.x doesn't carry it, so expect `400 … missing a thought_signature` on the second round trip. Groq and Ollama are unaffected. For Gemini tools in production, use Google's own .NET SDK (`Google.GenAI`, which has its own `IChatClient`).
- **A tool that changes things needs a human.** `ApprovalRequiredAIFunction` makes the loop stop and return a `ToolApprovalRequestContent` instead of running the method. Session 12 sends the yes/no back.
- LLM tool choices vary from run to run and between providers. Your C# side (what the tool does with the arguments) is repeatable; the model's decision isn't. That's what Session 13 (evaluation) is for.
