# Session 05 assignment · my vector store

**Name:**
**My 8–10 texts are about:** (your team's FAQ, a runbook, release notes, product docs…)
**Embedding model:** all-minilm (384 numbers) (or:)
**Store:** pgvector in Docker (`pgvector/pgvector:pg18`)

## Proof it's in Postgres

Paste the output of:

```bash
docker exec -it pgvector psql -U postgres -c "SELECT \"Id\", \"Topic\", left(\"Text\", 40) FROM notes;"
```

## My 3 searches

| # | Search (in my own words) | Top hit | Score | Right? |
| --- | --- | --- | --- | --- |
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |

One of the three should use **no words** from the text you hoped to find.

## Reflections (2–3 lines each)

1. Which search surprised you, and why do you think it ranked that way?
2. What score would you call "a real match" for your texts? Would that number work for a different model?
3. You stored the texts and their vectors. What would you have to redo if you switched embedding models?
4. Stretch goals I tried, and what happened:
