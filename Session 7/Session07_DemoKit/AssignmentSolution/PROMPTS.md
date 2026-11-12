# Session 07 assignment · my first tool (example answer)

**Name:** Solution
**My tool:** `GetOnCall(team)`: who's on call for payments, platform or mobile (stub rota). Plus S1: `SearchNotes(query)`.
**Chat model:** Groq llama-3.3-70b-versatile

## My tool's description

- Method: "Gets who is on call right now for an engineering team. Use it when someone needs to page, wake up or reach the on-call engineer."
- Parameter: team = "The team name: payments, platform or mobile"

## My 5 prompts

These are what a good model *should* do; the C# was verified against a mock, not the real Groq API. **Run them yourself and write down what yours actually did.**

| # | Kind | Prompt | ToolCalls | Answer (short) | Right? |
| --- | --- | --- | --- | --- | --- |
| 1 | Should call it | Who is on call for payments right now? | GetOnCall(payments) | Priya Nair, +91 98450 00001, until 9 am | Yes |
| 2 | Different words | Payments are failing for every customer. Who do I wake up? | GetOnCall(payments) | Priya Nair… (and maybe SearchNotes for the incident process) | Yes |
| 3 | Different words again | I need the mobile team's on-call number | GetOnCall(mobile) | Sara Thomas, +91 98450 00003 | Yes |
| 4 | Should NOT call it | What does on-call mean, in one sentence? | (none) | A one-sentence definition | Yes |
| 5 | Missing argument | Who is on call? | (none) | "Which team: payments, platform or mobile?" | Yes, if the model follows "don't guess". Watch for GetOnCall(platform) invented from nothing |

## Reflections

1. Prompt 5 is the fragile one. Without "Don't guess arguments" in the system prompt, models often pick a team. The parameter description listing the three teams helped the model map "payments are failing" to `payments`.
2. Arguments are lower-cased or capitalised unpredictably ("Payments"), which is why the stub uses `ToLowerInvariant()`. Never trust a model's arguments more than you'd trust a user's.
3. A read-only rota lookup is safe. "Page the on-call engineer" (an action that wakes a human at 3 am) should need approval (Session 12).
4. S1: "The site is down. What's the process, and who is on call for platform?" called both tools, SearchNotes then GetOnCall, in one request. S4: removing the parameter description dropped it from the schema.
