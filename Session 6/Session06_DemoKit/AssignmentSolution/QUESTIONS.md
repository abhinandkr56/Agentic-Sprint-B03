# Session 06 assignment · my first RAG assistant (example answer)

**Name:** Solution
**My notes are about:** one team's engineering handbook (API, Deployments, Onboarding, Incidents), the 10 notes from Session 5's solution
**How many notes:** 10
**Embedding model:** all-minilm (384)   **Chat model:** Groq llama-3.3-70b-versatile
**Gate (minimum score):** 0.3

## My 5 questions

The sources and scores are real (all-MiniLM-L6-v2, the model behind Ollama's all-minilm; yours will be close, not identical). The answers are what a good model should do with those sources: **run them yourself and write down what yours actually said.**

| # | Kind | Question | Sources (id · score) | Answer (short) | Right? | If wrong: retrieval or generation? |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | In one note, its own words | How many API calls can I make per minute? | 1 · 0.68 | 100 per minute; above that you get HTTP 429 [1] | Yes | |
| 2 | Same, different words | Our website is down and nobody is answering, who do I wake up? | 9 · 0.46 | Page the on-call engineer through PagerDuty, then post in #incidents [9] | Yes | No shared words, still found |
| 3 | Not in my notes | What is our parental leave policy? | (none: best was 0.13) | "I don't know. It isn't in our notes." (model not called) | Yes | The gate did its job |
| 4 | Needs two notes | Do I need two approvals to merge a hotfix at night? | 5 · 0.54 | Yes, two approvals and a green build [5]. Says nothing about *when* you can deploy | Half | **Retrieval:** note 4 (deploy times) scored 0.18 and the gate dropped it |
| 5 | My choice: Hinglish | naya laptop kab milega? | 7 · 0.32 | On day one [7] (in Hinglish, if the model follows the rules) | Yes, luckily | Only found because "laptop" is the same word in both languages. all-minilm is English-only |

## Same question, /ask vs /rag

"How many API calls can I make per minute?"

- `/ask` (no notes): a general answer about rate limits "depending on your plan", nothing about *our* API.
- `/rag`: "100 requests per minute; above that the API returns HTTP 429 [1]", with note 1 in Sources.

Same model, same question. The only difference is the note in the prompt.

## Reflections

1. Q4 surprised me: the answer sounded complete, but it only had one of the two notes it needed. A confident answer isn't a complete one, and the Sources list is how I noticed.
2. Yes, in Q4 the gate dropped note 4 (0.18). Lowering the gate would let in more noise for other questions. I'd rather try top 5 and a stricter prompt, and measure it (Session 13).
3. Onboarding questions for new joiners. First fix: more notes, and chunk the real onboarding doc instead of one-liners.
4. S1 (no model call below the gate: Q3 came back instantly) and S5 (token counts: about 130–170 tokens in per question with 1–3 notes).
