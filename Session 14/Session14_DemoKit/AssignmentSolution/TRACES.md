# Session 14 assignment · What my traces showed (example answer)

**Endpoint I traced:** `POST /rag` (+ stretch S1: `POST /agent/reply`)
**Viewer:** Aspire dashboard (on the prep machine: the OTLP stand-in, which prints the same spans as a tree)
**Model:** Groq `openai/gpt-oss-120b` · embeddings: Ollama `all-minilm`

> The C# was verified against OpenTelemetry 1.20.1, Microsoft.Extensions.AI 10.11.0 and MAF 1.27.0 built from source, with
> a rule-based stand-in for Groq and Ollama that sleeps like a real call. **The numbers below are the stand-in's: your real
> model's durations and token counts will differ. The SHAPE of the traces is the point.**

| Run | Question | Total (ms) | Spans you see (name · ms) | Model call (ms) | Tokens in / out | rag.hits |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | requests per minute | ~430 | POST /rag · embeddings all-minilm (~45) · POST (HTTP to Ollama) · chat openai/gpt-oss-120b (~380) · POST (HTTP to Groq) | ~380 | 139 / 40 | 2 |
| 2 | which days we deploy | ~50 | POST /rag · embeddings all-minilm (~45) · POST (HTTP to Ollama) | **none** | — | **0** |
| 3 | the site is down | ~430 | same shape as run 1 | ~380 | 137 / 40 | 1 |

## What I noticed

1. The chat span is ~90% of every request that calls the model. The embedding is ~10%. The vector search itself barely shows.
2. Run 2 has a different shape: no chat span at all. `rag.hits = 0`, so /rag answered "I don't know" without calling the model.
   That's Session 13's failing "deploy days" case: the trace shows it's a RETRIEVAL miss before I read a line of code.
3. At startup there are traces I didn't trigger: the MCP connection to Microsoft Learn and the three seed notes being embedded.
4. `gen_ai.usage.input_tokens` (cost), the chat span's duration (latency), `error.type` (failures), `gen_ai.response.model`
   (did the provider swap the model under me?).
5. Not with the defaults: prompts and replies are NOT in the spans unless EnableSensitiveData is on. I turned it on only in
   Development, from config (stretch S3), and left it false in appsettings.Development.json.
6. S1: `/agent/reply` for order 4411 → invoke_agent SupportAgent → chat (finish_reasons tool_calls) → execute_tool GetOrder
   → chat (stop). The tool comes from the MCP OrdersServer: the trace doesn't care. S2: the Metrics page shows
   `gen_ai.client.token.usage` split into input and output.
