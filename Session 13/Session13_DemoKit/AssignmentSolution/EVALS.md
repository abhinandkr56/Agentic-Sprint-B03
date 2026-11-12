# Session 13 assignment · Evals for my AI app (example answer)

**What I tested:** the RAG assistant (`POST /rag`, 8 cases) and the agent (`POST /agent/reply`, 2 cases from APPROVALS.md)
**Model under test:** Groq `openai/gpt-oss-120b` · **Judge model:** the same (stretch S2 would change it)

> The C# was verified against Microsoft.Extensions.AI.Evaluation.Quality 10.11.0 built from source, the Session 12
> MyAiApi and a rule-based stand-in for the model and the embedder. **The rows below are the stand-in's run: your
> real model, your embedder and your notes will give different verdicts. That's the point: run it and mark it.**

# Evals · (stand-in run) · 6/10 passed

| Test | Result | Problems | Right? (do you agree?) |
| --- | --- | --- | --- |
| rate limit | PASS |  | yes |
| too many | PASS |  | yes |
| deploy days | FAIL | doesn't say "Thursday"; retrieval: note [2] not found (got []) | yes: a real miss, and a RETRIEVAL one: the note never came back, so the model never saw it |
| deploy time | PASS |  | yes |
| site down | FAIL | groundedness 2/5: the response says 'within 5 minutes', which is not in the CONTEXT | yes: "within 5 minutes" is invented. A GENERATION miss: the right note came back |
| after paging | PASS |  | yes |
| not in notes | PASS |  | yes |
| not in notes 2 | FAIL | doesn't say "I don't know"; retrieval: got [3] for a question our notes can't answer | yes: the 0.3 score gate let an unrelated note through, and the model answered from it. Both halves failed |
| A1 refund status | PASS |  | yes |
| A2 pushy Friday | FAIL | says "by Friday" | yes: the same promise a person rejected in Session 12 (A2) |

## Reflections

1. None this run, but "I don't know" vs "I don’t know" (curly apostrophe) would be a false fail: the check reads characters, not meaning.
2. Retrieval: "deploy days" (the embedder didn't match "deploy" to "Deployments") and "not in notes 2" (a wrong note came back). Generation: "site down" (invented a time limit) and, again, "not in notes 2" (it answered from a note that didn't answer the question).
3. The RAG answers changed wording between runs; the verdicts mostly didn't. One pass is an anecdote; 10 cases × 3 runs is evidence.
4. More cases from real tickets, a judge that isn't the model under test, a human check of the judge on 20 answers, and a pass threshold (e.g. 9/10), not "all".
5. S1: the agent cases came straight from APPROVALS.md: a decision a person made in Session 12 is now a test that runs on every change.
