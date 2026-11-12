# Session 10 assignment · my first agent (example answer)

**Agent:** SupportAgent · Groq openai/gpt-oss-120b
**Tools it can call:** FindOrders (local stub) · GetOrder (Session 8's OrdersServer, over MCP) · IssueRefund (local stub, blocked by middleware)

> The C# was verified against Microsoft.Agents.AI 1.25.0 built from source, the real MCP C# SDK 2.2.0 and a
> rule-based stand-in for the model. **What the real model calls, the wording, and the token counts come from YOUR run.**

## My instructions (Prompts.Agent)

See `Models.cs`: look facts up first (FindOrders for email-only, GetOrder for details), never invent, ask when the
order can't be found, never promise a refund, 3–4 sentences, sign as "Asha, Support".

## Three tickets

| # | Ticket | Steps (expected) | Draft (short) | Right? |
| --- | --- | --- | --- | --- |
| T1 | I cancelled my plan last week and still no refund!! priya@example.com | FindOrders(priya@example.com) → GetOrder(4411) | Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct | (your run) |
| T2 | Where is my refund for order 4411? | GetOrder(4411) | same facts, one step | (your run) |
| T3 | Hello, where is my refund??? | (none) | asks for the order number or email | (your run) |
| S1 | "Which order was that again?" (same conversationId) | (none) | "Order 4411" from the session | (your run) |
| S2 | Refund order 4412 now, I was charged twice!! | GetOrder(4412)? → IssueRefund(4412) BLOCKED | "a teammate will review it" | Yes: money didn't move |

## What made it an agent

- My code never says "call FindOrders, then GetOrder". The model chose both, in order, and used step 1's answer as step 2's input.
- T1 took three round trips with gpt-oss (no parallel tool calls): FindOrders, GetOrder, the draft.

## Reflections

1. With no tools (Build 3 Start) the model promised "3-5 business days": invented. Tools + "never invent" fixed it.
2. Freshdesk/Zendesk read APIs as tools, yes. The refund API, never without a human (Session 12).
3. /chat with the same tools gives a similar answer: an agent is the same loop, packaged with its instructions,
   tools, sessions and middleware, so it can be reused, composed (Session 11) and governed (Session 12).
4. S1 + S2 above.
