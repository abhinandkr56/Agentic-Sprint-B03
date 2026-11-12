# Session 14 assignment · What my traces showed

**Name:**
**Endpoint I traced:** `POST /rag` ☐ · other: ______
**Viewer:** Aspire dashboard ☐ · console exporter ☐ · other: ______
**Model:** ______

| Run | Question | Total (ms) | Spans you see (name · ms) | Model call (ms) | Tokens in / out | rag.hits |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | | | | | | |
| 2 | | | | | | |
| 3 | | | | | | |

## What I noticed (2–3 lines each)

1. Which span takes most of the time? Is it the one you expected?
2. Do the three runs have the same shape (the same spans)? If not, why not?
3. Was there anything in the trace you didn't know your app did? (startup calls, retries, a second model call…)
4. Look at the attributes on a `chat` span. Which ones would you alert on in production?
5. Is anything in your traces that shouldn't be there (an email, a prompt)? How would you keep it out?
6. Stretch goals I tried, and what happened:
