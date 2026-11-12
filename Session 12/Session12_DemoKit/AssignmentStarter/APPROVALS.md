# Session 12 assignment · An approval checkpoint

**Name:**
**The final action:** ______ (e.g. SendReply: an email to the customer)
**Where the checkpoint sits:** after ______, before ______

## The flow (fill in)

```
POST /agent/reviewed-reply → SupportAgent (tools) → ReviewerAgent → ________ → (a person) → ________
```

## Three decisions, end to end

| # | Ticket | Reviewer said | Person decided | What happened (status, console) | Right? |
| --- | --- | --- | --- | --- | --- |
| A1 | | | approve | | |
| A2 | | | reject | | |
| A3 | (A1's id again) | – | approve | | |

## Reflections (2–3 lines each)

1. What does the person see when deciding? Is it enough to decide well? What would you add?
2. What happens to a reply nobody decides on? (Restart the app: where did it go?)
3. Which actions in YOUR job should always wait for a person? Which shouldn't (and why)?
4. Stretch goals I tried, and what happened:
