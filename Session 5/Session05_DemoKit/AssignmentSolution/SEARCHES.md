# Session 05 assignment · my vector store (example answer)

**Name:** Solution
**My 8–10 texts are about:** one team's engineering handbook (API, Deployments, Onboarding, Incidents)
**Embedding model:** all-minilm (384 numbers)
**Store:** pgvector in Docker (`pgvector/pgvector:pg18`)

## Proof it's in Postgres

```text
 Id |    Topic    |                   left
----+-------------+------------------------------------------
  1 | API         | Our API returns HTTP 429 when you send m
  2 | API         | Every request needs an X-Api-Key header;
  ...
 10 | Incidents   | Write a blameless postmortem within 48 h
(10 rows)
```

## My 3 searches

Scores from a run against all-MiniLM-L6-v2 (yours will be close, not identical).

| # | Search (in my own words) | Top hit | Score | Right? |
| --- | --- | --- | --- | --- |
| 1 | how often can I call the API? | #1 HTTP 429 above 100 requests per minute | 0.59 | Yes |
| 2 | everything is broken and customers can't log in, who do I tell? | #9 page the on-call engineer | 0.32 | Yes, but only just above my 0.3 cut-off. No shared words |
| 3 | can I ship code on a Friday? | #4 deployments Tuesday and Thursday | 0.37 | Yes: it found the rule, even though the answer is "no" |

## Reflections

1. Search 2 surprised me: no words in common, still the right note, but with a low score. Similar meaning isn't the same as a high number.
2. 0.3 works for all-minilm on short texts. A different model spreads its scores differently, so I'd have to measure again.
3. All of it: every vector came from all-minilm. A new model means a new collection (new size) and embedding every note again.
4. S1 (filter on Topic) and S2 (no good match for "best biryani in Hyderabad": best was 0.07).
