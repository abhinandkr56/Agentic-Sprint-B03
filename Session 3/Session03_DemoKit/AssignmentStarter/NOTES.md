# Session 03 assignment notes

**Name:**
**Provider A (default):**            **Model:**
**Provider B (the swap):**           **Model:**
**How I swapped (command line, env var, or appsettings):**

## Same request, two providers

| Request | Provider A answer (short) | ms | Provider B answer (short) | ms |
| --- | --- | --- | --- | --- |
| `/ask` question 1 | | | | |
| `/ask` question 2 | | | | |
| `/triage` (or your own endpoint) | | | | |

## Lines of code I changed to swap providers

(Should be zero in Program.cs. If not, what did you have to change and why?)

## My record and prompt from Session 2

```csharp
(paste your record here)
```

## Reflections (2–3 lines each)

1. What did `GetResponseAsync<T>()` do for you that you wrote by hand in Session 2?
2. Did both providers support native JSON Schema (`NativeJsonSchema: true`)? What happened when one didn't?
3. What's still provider-specific, even behind one interface? (speed, quality, features, limits, cost...)
4. Stretch goals I tried, and what happened:
