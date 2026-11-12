# Session 04 · Enterprise Use Cases for Local Models

*Agentic Sprint for .NET Developers · companion to Session 4 (Running Models Locally with Ollama)*

Use this as a talking-point sheet when attendees ask "where would a company actually use this?" (slides 6 and 22, Q&A), or share it as a handout. Each use case names the business reason, the size of model it usually needs, and which of tonight's builds it builds on.

---

## The short answer

Companies run models locally, or on their own servers, when at least one of these is true:

| Driver | Typical trigger |
| --- | --- |
| **Data can't leave** | Contracts, regulation, customer PII, source code, health or financial records, internal policy |
| **No reliable network** | Factories, ships, field work, branch offices, air-gapped networks |
| **Volume makes per-token pricing hurt** | Millions of small, repetitive calls (classify, tag, extract) |
| **Control and stability** | A pinned model version that won't be retired or changed by a vendor |
| **Developer productivity** | Dev, tests and CI without keys, quotas or bills |

And they keep the cloud for what small models do badly: open-ended reasoning, long documents, the newest capabilities.

> In production, "local" rarely means a laptop. It usually means **self-hosted**: Ollama (or a similar runtime) on a company server or GPU VM inside the network, called by many apps. The code is the same as tonight: only `Endpoint` in `appsettings.json` changes.

---

## Use cases by function

### 1. Support ticket triage and routing *(Build 4)*

- **What:** classify incoming tickets (category, priority, sentiment), extract order IDs, route to the right queue.
- **Why local:** tickets contain customer names, phone numbers, addresses, payment details; volume is high and the task is narrow.
- **Model size:** 1–4B with a clear prompt and examples; 7–8B for multilingual tickets or messy text.
- **Pattern:** `GetResponseAsync<Triage>()` with enums (the local server enforces the schema), plus a test set of real tickets to measure accuracy (Session 13).
- **Watch out:** valid JSON isn't a correct priority. Route low-confidence or High-priority tickets to a human.

### 2. PII detection and redaction before the cloud *(Builds 3 and 5)*

- **What:** a local model finds names, account numbers and addresses in text and masks them; only the cleaned text goes to a cloud model for the hard part.
- **Why local:** the raw data never leaves the network, but you still get cloud-quality answers.
- **Model size:** 1–8B, ideally combined with deterministic rules (regex for e-mails, card numbers, Aadhaar/PAN formats).
- **Pattern:** Session 3's middleware (`.Use(...)`), with a local `IChatClient` doing the masking step instead of a regex.
- **Watch out:** LLM redaction misses things. Treat it as one layer, not the only control.

### 3. Internal document Q&A for confidential content *(preview of Sessions 5–6)*

- **What:** "Ask the HR policy", "ask the engineering wiki", "ask the contract archive".
- **Why local:** the documents are confidential; leadership won't approve sending them to a third party.
- **Model size:** 7–14B on a GPU server for decent answers; small models struggle with long retrieved context.
- **Pattern:** RAG: embeddings + vector search (Session 5), grounded answers (Session 6). Ollama can serve both the embedding model and the chat model.
- **Watch out:** Ollama's default context is 4K on most hardware. Raise `OLLAMA_CONTEXT_LENGTH` on the server, or retrieved documents get cut.

### 4. Code assistance on proprietary source

- **What:** explain legacy code, generate unit test skeletons, summarise pull requests, write commit messages.
- **Why local:** source code is often the most sensitive asset a software company has; some clients forbid sending it outside.
- **Model size:** 7B+ code-tuned models on a developer GPU or shared server.
- **Pattern:** the same `IChatClient`, behind an internal API or a dev tool.
- **Watch out:** check each model's licence for commercial use.

### 5. Offline and edge operations *(Build 3)*

- **What:** maintenance manuals on a factory floor, field-engineer assistants, ships, mines, remote clinics, retail branches with poor connectivity.
- **Why local:** the network is unreliable or absent; the answer is needed anyway.
- **Model size:** 1–4B on edge hardware or a rugged laptop; 7–8B where a small GPU is available.
- **Pattern:** cloud first, local fallback (Build 3), or local only.
- **Watch out:** updating models and documents across many devices is an operations problem in itself.

### 6. Regulated industries: banking, insurance, healthcare, government

- **What:** summarising case notes, drafting customer letters, classifying claims or complaints, extracting fields from forms.
- **Why local:** data-residency rules, regulator expectations, client contracts, audit requirements.
- **Model size:** depends on the task: narrow extraction 1–8B, drafting 8–14B+.
- **Pattern:** self-hosted inside the regulated network, with logging, access control and evaluation.
- **Watch out:** local doesn't mean compliant by itself. Access control, logging, retention and human review are still required. Get the compliance team involved early.

### 7. Bulk back-office processing

- **What:** tagging product catalogues, normalising addresses, extracting fields from invoices or purchase orders, classifying spend, cleaning CRM data.
- **Why local:** huge volume of simple calls, where per-token pricing adds up; jobs can run overnight on owned hardware.
- **Model size:** 1–4B with typed output (Build 4); measure accuracy on a sample.
- **Pattern:** a background worker calling `IChatClient` with `GetResponseAsync<T>()`; batch, retry, and send failures to a review queue.
- **Watch out:** throughput depends on the GPU, not the code. Measure tokens per second (Build 1) before promising a deadline.

### 8. Development, testing and CI

- **What:** run AI features in dev and integration tests without API keys, quotas or costs; demos that don't depend on conference Wi-Fi.
- **Why local:** no key management for every developer, deterministic environments, no bill for test runs.
- **Model size:** 1B is enough to test the plumbing (tonight's model).
- **Pattern:** the config swap from Build 5: `AI:Provider=Ollama` in development, a cloud provider in production. In CI, run Ollama as a container next to the tests.
- **Watch out:** a 1B model in tests doesn't tell you how the production model will answer. Test quality separately (Session 13).

### 9. Hybrid routing: cheap and local first, cloud when needed

- **What:** a small local model handles easy requests; harder ones, or ones where it's unsure, go to a large cloud model.
- **Why:** cost and privacy for the majority of traffic, quality for the rest.
- **Pattern:** keyed clients (the assignment's `/compare` stretch) plus a routing rule: by task type, by input length, or by a "can you answer this?" check.
- **Watch out:** routing logic needs its own evaluation; a bad router sends hard questions to the weak model.

---

## Quick decision guide

| Question | If yes → |
| --- | --- |
| Is sending the data outside forbidden or risky? | Local / self-hosted (or redact locally first) |
| Must it work without internet? | Local, at least as a fallback |
| Is it a narrow task (classify, extract, rewrite)? | Try a small local model first and measure |
| Does it need deep reasoning, long documents, or the latest knowledge? | Cloud (or a large self-hosted model on GPUs) |
| Millions of calls a month? | Compare self-hosting cost with API cost on real numbers |
| Many users and no one to run GPUs? | Cloud, or a managed private deployment |

---

## What "doing it properly" adds in an enterprise

Tonight was one laptop. A company deployment also needs:

- **Hosting:** Ollama (or another runtime) on GPU servers or VMs inside the network, sized for concurrency, not just one user.
- **Security:** Ollama has no authentication. Put it behind an API gateway or reverse proxy with auth; don't expose `OLLAMA_HOST=0.0.0.0` on an open network.
- **Model governance:** an approved list of models and versions, licence review, and a way to roll out updates.
- **Evaluation:** a test set per use case, run before every model or prompt change (Session 13).
- **Observability:** traces, latency, token counts and errors per call (Session 14; Session 3's middleware is the hook).
- **Packaging:** the API, the vector store and the model server run together (Session 15, .NET Aspire).
- **Human in the loop** for actions with real consequences (Session 12).

---

## Talking points for class

- "Most companies don't choose local *or* cloud. They choose per feature, and many use both."
- "Local isn't about your laptop. It's about where the data goes. In production it's a server inside your network."
- "The best first local use case is boring: classify, extract, redact. High volume, sensitive data, narrow task."
- "Whatever you pick, the code you wrote tonight doesn't change. That's what `IChatClient` bought us."

---

## Caveats

- Model sizes in this sheet are rough starting points, not benchmarks. Measure on your own data and hardware.
- Licences differ per model (Llama community licence, Gemma terms, Apache 2.0 for many Qwen and Granite models). Check before commercial use.
- This sheet describes common patterns, not legal or compliance advice: regulated use needs your compliance and legal teams.
