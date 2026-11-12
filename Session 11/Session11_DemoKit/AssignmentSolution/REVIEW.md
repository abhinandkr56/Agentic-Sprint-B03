# Session 11 assignment · Writer → Reviewer (example answer)

**Writer:** SupportAgent · Groq openai/gpt-oss-120b · FindOrders (local), GetOrder (OrdersServer over MCP), IssueRefund (blocked)
**Reviewer:** ReviewerAgent · Groq openai/gpt-oss-120b (or Gemini with --AI:ReviewerProvider=Gemini) · no tools

> The C# was verified against Microsoft.Agents.AI 1.25.0 built from source, the real MCP C# SDK 2.2.0 and a
> rule-based stand-in for the model. **What the real models write and flag, and the token counts, come from YOUR run.**

## My reviewer's checklist

See `Models.cs` (`Prompts.Reviewer`): facts only from FACTS, no promises (and never a refund), warm tone, signed
"Asha, Support"; when there are no FACTS, approve only a draft that asks for the order number or email.

## Three tickets, end to end (expected shape)

| # | Ticket | Writer's steps | Round 1 | Round 2 | Final draft | Right? |
| --- | --- | --- | --- | --- | --- | --- |
| R1 | I cancelled my plan last week and still no refund!! priya@example.com | FindOrders → GetOrder(4411) | APPROVED | – | Rs 10,989, issued 2 Oct, expected by 9 Oct | (your run) |
| R2 | Order 4413: refund the SMS pack too … by Friday | GetOrder(4413) | NOT APPROVED: "by Friday" / a refund promise not in FACTS | APPROVED | facts only, a teammate reviews refunds | (your run) |
| R3 | Hello, where is my refund??? | (none) | APPROVED: asks for the order number or email | – | the question | (your run) |

## Passing information

- The reviewer got exactly three things: the TICKET, the FACTS (each FunctionResultContent from the writer's run) and the DRAFT.
- FACTS come from the tool results because the draft is the thing under review: checking a draft against itself proves nothing.

## Reflections

1. A checklist that says "every date must appear in FACTS" catches invented dates reliably; tone is the fuzziest rule.
2. Each reviewed ticket costs one more model call (two with a fix round): the reviewer's input is small (no tool schemas, no history).
3. Worth it where a wrong answer is expensive (money, legal, health); not worth it for internal drafts a human edits anyway.
4. S2: reviewer on Gemini · S5: tokens logged per round.
