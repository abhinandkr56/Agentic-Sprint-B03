namespace HelloLLM.Demos;

/// DEMO 3 — Tokens are the currency. Same meaning, very different token counts.
/// We ask the API itself to count (usage.prompt_tokens) and cap output at 1 token to keep it cheap.
public static class D3_Tokens
{
    private static readonly (string Label, string Text)[] Samples =
    [
        ("English",   "Good morning! Welcome to the AI for .NET developers cohort."),
        ("Hindi",     "सुप्रभात! .NET डेवलपर्स के लिए एआई कोहोर्ट में आपका स्वागत है।"),
        ("Malayalam", "സുപ്രഭാതം! .NET ഡെവലപ്പർമാർക്കുള്ള എഐ കോഹോർട്ടിലേക്ക് സ്വാഗതം."),
        ("C# code",   "var app = WebApplication.CreateBuilder(args).Build(); app.MapGet(\"/\", () => \"Hi\");"),
        ("Emoji",     "🚀🔥🤖💜🎉🚀🔥🤖💜🎉"),
    ];

    public static async Task Run(Llm llm)
    {
        // Every chat request carries some fixed template overhead; measure it once and subtract.
        var baseline = (await llm.AskAsync(".", maxTokens: 1)).Usage?.PromptTokens ?? 0;
        Ui.Dim($"(chat-template overhead ≈ {baseline} tokens, subtracted below)\n");

        var results = new List<(string Label, int Chars, int Tokens)>();
        foreach (var (label, text) in Samples)
        {
            var r = await llm.AskAsync(text, maxTokens: 1);
            var tokens = Math.Max(1, (r.Usage?.PromptTokens ?? 0) - baseline + 1);
            results.Add((label, text.Length, tokens));
        }

        int max = results.Max(r => r.Tokens);
        foreach (var (label, chars, tokens) in results)
        {
            var bar = new string('█', Math.Max(1, tokens * 40 / max));
            Console.Write($"  {label,-10} {chars,3} chars → {tokens,3} tokens  ");
            Ui.Write(ConsoleColor.Magenta, bar + "\n");
        }

        Ui.Good("\nSame greeting, different price. Tokens ≠ words ≠ characters.");
        Ui.Good("Rule of thumb (English): 1 token ≈ 4 characters ≈ ¾ of a word.");
        Ui.Dim("See tokens visually: https://tiktokenizer.vercel.app");
    }
}
