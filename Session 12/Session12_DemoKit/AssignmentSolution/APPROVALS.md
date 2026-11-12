# Session 12 assignment · An approval checkpoint (example answer)

**The final action:** `SendReply(to, body)`: an email to the customer (a stub that prints `>>> EMAIL SENT <<<`).
**Where the checkpoint sits:** after the reviewer (Session 11), before `SendReply`.

> The C# was verified against Microsoft.Agents.AI 1.26.0 built from source, the real MCP C# SDK 2.2.0 and a
> rule-based stand-in for the model. **What the real models write and the reviewer flags come from YOUR run.**

## The flow

```
POST /agent/reviewed-reply → SupportAgent (FindOrders, GetOrder) → ReviewerAgent (+1 fix round)
    → parked in `pending` (status awaiting_approval) → POST /agent/approvals/{id} → SendReply | not sent
```

## Three decisions (expected shape)

| # | Ticket | Reviewer said | Person decided | What happened | Right? |
| --- | --- | --- | --- | --- | --- |
| A1 | I cancelled my plan last week and still no refund!! priya@example.com | APPROVED | approve | `sent`, console: `>>> EMAIL SENT to priya@example.com <<<` | (your run) |
| A2 | Order 4413: refund the SMS pack too … by Friday. ravi@example.com | APPROVED after a fix round | reject | `rejected: not sent`, no email in the console | (your run) |
| A3 | A1's id again | – | approve | 404 `not found (already decided?)`: no second email | yes: one decision per reply |

## Reflections

1. The person sees the draft, the reviewer's verdict and problems, the recipient and how long it waited. Worth adding: the FACTS (the evidence) so they can check a date without opening another system.
2. It lives in memory: a restart loses it (and nobody is told). Stretch S3 saves the session; a real system stores pending replies in a database and expires them (S4).
3. Money, anything legal or medical, anything sent to many people, and anything that can't be undone: always a person. Read-only lookups and drafts nobody sees: no checkpoint, it just adds waiting.
4. S1: a rejection note sent the draft back to the writer; the new version waited under a new id. S5: approvals.log has one line per decision.
