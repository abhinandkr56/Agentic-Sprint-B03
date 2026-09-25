# Session 01 — LLM Fundamentals for .NET Developers

**Agentic Sprint for .NET · Milestone 1, Session 1 of 18**

Take-home notes. Everything we covered, plus the code, the assignment and where to go deeper.

---

## In one line

An LLM is a next-token predictor behind an HTTPS endpoint. Calling it from .NET is a POST with JSON, and everything else you'll learn in this cohort is built on top of that.

---

## 1. The big picture: where LLMs sit

```
Artificial Intelligence (1956 →)      machines doing "intelligent" tasks
 └─ Machine Learning (1980s →)        learns rules from data
     └─ Deep Learning (2012 →)        neural networks with many layers
         └─ Generative AI (2022 boom) creates new content
             └─ LLMs (2018 →)         generative AI for language
```

Every LLM is AI, but most AI is not an LLM.

**The mindset flip**

| | Input | Output |
| --- | --- | --- |
| Traditional programming | Rules (your `if`/`else`) + data | Answers |
| Machine learning | Data + answers (labels) | Rules (a trained model) |

**Four kinds of learning**

- **Supervised** — learns from labelled examples (house prices, spam, churn)
- **Unsupervised** — finds groups with no labels (customer segments, anomalies)
- **Reinforcement** — learns by trial, error and reward (AlphaGo, robotics, RLHF)
- **Self-supervised** — the data labels itself: hide the next word, predict it. **This is how every LLM is trained.**

**How we got here**

| Year | Moment |
| --- | --- |
| 1950 | Turing asks "Can machines think?" |
| 1956 | The Dartmouth workshop coins "artificial intelligence" |
| 1966 | ELIZA, the first chatbot (pattern matching, no learning) |
| 1974–93 | Two "AI winters": hype outran what computers could do |
| 1997 | Deep Blue beats Kasparov (brute-force search, not learning) |
| 2012 | AlexNet: deep learning + GPUs take off |
| 2016 | AlphaGo beats Lee Sedol |
| 2017 | Transformers: "Attention Is All You Need" — the "T" in GPT |
| 2020 | GPT-3, about 175 billion parameters |
| Nov 2022 | ChatGPT reaches roughly 100M users in about two months |
| 2023–24 | GPT-4, Claude, Llama, Gemini; multimodal models |
| 2025–26 | Reasoning models and AI agents |

**The .NET track:** 2018 ML.NET · 2023 Semantic Kernel · 2024 Microsoft.Extensions.AI · 2025 Microsoft Agent Framework. This cohort follows it.

**Why now?** Three things arrived together: the transformer (algorithm), GPUs (compute), the internet (data).

**Predictive AI vs generative AI**

- Predictive (discriminative): "Is this transaction fraud?" → a label or score. Still the right tool for fraud, forecasting and recommendations.
- Generative: "Reply politely to this angry customer" → brand-new content: text, code, images, audio, video.

**How an LLM is made**

1. **Pre-training** — reads trillions of tokens of web pages, books and code, learning to predict the next token. Result: a *base model*, a brilliant autocomplete that isn't helpful yet. Months, thousands of GPUs.
2. **Instruction tuning** — trained on example conversations (question → ideal answer). Result: it follows instructions instead of just continuing text.
3. **Alignment (RLHF)** — humans rank answers, and the model is rewarded for helpful, honest, safe ones. Result: the assistant you call through the API.

> **Knowledge cutoff:** the model only knows what was in its training data. Not today's news, not your codebase, not your company docs. That's why we add RAG (Session 6) and tools (Session 7).

---

## 2. What an LLM actually does

It predicts the next **token**, appends it, and runs again — until it predicts "stop".

```csharp
// pseudo-code: what happens inside the provider
string Generate(string prompt)
{
    var text = prompt;
    while (true)
    {
        var probs = model.Predict(text);   // a score for ~100k possible tokens
        var next  = Sample(probs, temperature);
        if (next == "<|end|>") break;
        text += next;                      // append and repeat
    }
    return text;
}
```

Consequences worth remembering:

- Answers stream in word by word because each token is one pass through the model.
- Output tokens are usually priced higher than input tokens.
- It is **not looking anything up**. That's why hallucinations happen: a plausible guess still "scores" well.

### Tokens

A token is a chunk of text from the model's vocabulary: a common word, part of a word, or a symbol.

- English rule of thumb: 1 token ≈ 4 characters ≈ ¾ of a word. 1,000 tokens ≈ 750 words.
- `"Hello, .NET developers! 🚀"` ≈ 8 tokens.
- Indic scripts (Hindi, Tamil, Malayalam) typically cost **2–4× more tokens** than the same sentence in English, because tokenizers are trained mostly on English text. If you build for regional languages, budget for it and measure.
- See tokens visually: <https://tiktokenizer.vercel.app>

Why it matters: you pay per token, limits are in tokens, and speed is measured in tokens per second.

### The context window

Everything the model can "see" in one request: system prompt + chat history + any documents + your question + room for the answer. Typical sizes today are 128K tokens and up (a 128K window is roughly a 300-page book).

**The API is stateless.** The model remembers nothing between calls. "Memory" means *you* resend the conversation every time — and you pay for it every turn. Calling an LLM is like calling a brilliant consultant with total amnesia: you re-brief them on every call.

### Temperature

The randomness knob on `Sample()`.

| Temperature | Behaviour | Use for |
| --- | --- | --- |
| 0 – 0.3 | Focused, repeatable | JSON, classification, extraction, code |
| 0.5 – 0.8 | Balanced | Chat assistants, summaries, Q&A |
| 1.0+ | Creative, chaotic | Brainstorming, names, stories |

Even at 0 the output is not guaranteed identical, so never unit-test for an exact LLM string (Session 13 covers proper evaluation).

### Model types

| Type | Shape | Use |
| --- | --- | --- |
| **Chat** | `messages[]` in → a message out | 90% of what you'll do, including everything today |
| **Completion** | text in → more text out | Older, raw form; mostly replaced by chat |
| **Embedding** | text in → a vector of numbers | Similarity search, RAG (Session 5) |

### Providers

Most speak the **OpenAI-compatible** "chat completions" format, so switching is often just a URL + key change.

| Provider | Notes |
| --- | --- |
| Groq | Very fast open models (Llama, gpt-oss); generous free tier — what we use in class |
| OpenAI | GPT family; the API shape everyone copies |
| Azure OpenAI | OpenAI models inside your Azure tenant (enterprise, compliance) |
| Anthropic | Claude family; strong at code and long documents |
| Google Gemini | Very large context windows, multimodal |
| Ollama | Runs open models locally on your laptop (Session 4) |

---

## 3. LLMs in a .NET app

Your app → `HttpClient` → `POST /chat/completions` → the provider → JSON back. Nothing magical.

### The request

```http
POST https://api.groq.com/openai/v1/chat/completions
Authorization: Bearer gsk_••••••••
Content-Type: application/json

{
  "model": "llama-3.3-70b-versatile",
  "messages": [
    { "role": "system", "content": "You are a .NET mentor." },
    { "role": "user",   "content": "What is DI?" }
  ],
  "temperature": 0.7,
  "max_tokens": 300
}
```

- `model` — which brain. Changing this string changes quality, speed and price.
- `messages` — the whole conversation. `system` = rules/persona, `user` = the human, `assistant` = the model's earlier replies (this is how you give it memory).
- `temperature` — creativity. `max_tokens` — caps the **output** only.

### The response

```json
{
  "id": "chatcmpl-8f3a…",
  "model": "llama-3.3-70b-versatile",
  "choices": [{
    "message": { "role": "assistant", "content": "DI means your class asks for…" },
    "finish_reason": "stop"
  }],
  "usage": { "prompt_tokens": 24, "completion_tokens": 87, "total_tokens": 111 }
}
```

Read these four things on every call:

1. `choices[0].message.content` — the answer
2. `finish_reason` — `stop` = finished naturally, `length` = **truncated** by max_tokens
3. `usage` — your bill; log it
4. `id` — keep it for support and debugging

### The whole integration

```csharp
using var http = new HttpClient();
http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", apiKey);

var body = """
  { "model": "llama-3.3-70b-versatile",
    "messages": [{ "role": "user", "content": "Hi!" }] }
  """;

var res  = await http.PostAsync(url,
    new StringContent(body, Encoding.UTF8, "application/json"));
var json = await res.Content.ReadAsStringAsync();

var answer = JsonDocument.Parse(json).RootElement
    .GetProperty("choices")[0].GetProperty("message").GetProperty("content");
```

In production: `IHttpClientFactory` instead of `new HttpClient()` per request, typed records instead of `JsonDocument`, and `Microsoft.Extensions.Http.Resilience` (Polly) for retries. In **Session 3**, `IChatClient` from Microsoft.Extensions.AI replaces all of this — and lets you swap providers through configuration.

---

## 4. What we ran live

The demo app (`HelloLLM`) is in the session zip. Set your key, then `dotnet run`:

```bash
# PowerShell
$env:GROQ_API_KEY = "gsk_..."
# bash / zsh
export GROQ_API_KEY="gsk_..."

cd HelloLLM
dotnet run          # menu
dotnet run -- 3     # jump to demo 3
```

| # | Demo | What it showed |
| --- | --- | --- |
| 1 | Raw call | A JSON string out, a JSON string back, plus rate-limit headers |
| 2 | Typed call | Mapping to C# records; reading usage and finish_reason |
| 3 | Tokens | English vs Hindi vs Malayalam vs code vs emoji, measured live |
| 4 | Temperature | Same prompt at 0.0 and 1.5, three runs each |
| 5 | Memory | The model forgets your name until you resend the history |
| 6 | Streaming | Server-Sent Events, and why time-to-first-token is what users feel |
| 7 | Limits & errors | `finish_reason: length`, 404 unknown model, 401 bad key |
| 8 | DotNetBuddy | System prompt + history + streaming = a chatbot in ~30 lines |

Optional settings: `LLM_MODEL` (default `llama-3.3-70b-versatile`; `llama-3.1-8b-instant` is faster) and `LLM_BASE_URL` (OpenAI, xAI, GitHub Models, or local Ollama at `http://localhost:11434/v1`).

---

## 5. Before you ship anything

| Risk | What to do |
| --- | --- |
| **Secrets** | Keys in env vars, `dotnet user-secrets` or Key Vault. Never in git. Revoke leaked keys immediately |
| **Rate limits (429)** | Retry with exponential backoff and jitter |
| **Latency** | Seconds, not milliseconds. Stream the response and set sensible timeouts |
| **Cost** | Log `usage` per call. Long chats grow input tokens quietly |
| **Hallucinations** | Confident ≠ correct. Ground with RAG (Session 6), verify with evals (Session 13) |
| **Data privacy** | What you send leaves your network. Check provider terms, company policy and regulations |

**Napkin math:** 1,000 users × 20 messages × (1,500 input + 300 output) tokens ≈ 30M input + 6M output tokens a day. On a small model at about $0.075 / $0.30 per million tokens that's roughly **$4 a day**. The same traffic on a frontier model can be 20–50× more. Model choice is an architecture decision.

---

## 6. Your assignment (before Session 02)

**Goal:** call one LLM provider's API directly from a .NET console app using `HttpClient`, print the response, and note what a request and response actually look like.

1. Get a free Groq key at <https://console.groq.com/keys> and put it in an environment variable.
2. Open `AssignmentStarter/Program.cs` and fill in the 5 TODOs.
3. Call → print the raw JSON → pull out `content`, `usage` and `finish_reason`.
4. Fill in `NOTES.md` with what you saw.

**Stretch (pick one):** temperature 0 vs 1.5 three times each · prove statelessness with two calls · force `finish_reason: "length"` · set `"stream": true` · point the same code at a second provider by changing only the URL and key.

Time budget: 30–45 minutes. Don't over-engineer it. Post a screenshot of your output in the community channel.

---

## 7. Glossary

| Term | Meaning |
| --- | --- |
| **Token** | A chunk of text the model reads and writes; billing and limits are counted in these |
| **Context window** | Everything the model sees in one request, input and output together |
| **Temperature** | Randomness of token sampling; low = consistent, high = creative |
| **System prompt** | Instructions and persona sent before the conversation |
| **finish_reason** | Why generation stopped: `stop` (natural) or `length` (hit max_tokens) |
| **Streaming / SSE** | Tokens sent as they're generated, as `data:` lines |
| **Parameters** | The learned weights inside a model (70B = ~70 billion) |
| **Transformer** | The 2017 neural-network design behind modern LLMs |
| **RLHF** | Reinforcement learning from human feedback; tunes a model toward helpful answers |
| **Knowledge cutoff** | The date a model's training data ends |
| **RAG** | Retrieval-augmented generation: look it up, then answer with what you found (Session 6) |
| **Agent** | An LLM in a loop with tools and a goal (Sessions 10–12) |

---

## 8. Go deeper (optional)

- Groq docs and model list — <https://console.groq.com/docs>
- Microsoft.Extensions.AI (what we use in Session 3) — search "Microsoft.Extensions.AI" on Microsoft Learn
- "Attention Is All You Need" (2017) — the transformer paper
- Tokenizer playground — <https://tiktokenizer.vercel.app>
- Andrej Karpathy, "Let's build GPT" / "Intro to LLMs" on YouTube — the best free deep dive

**Next session — 02: Prompt Engineering & Getting Useful Output.** Bring one real prompt from your day job. We'll turn model output into a typed C# object.
