using System.Text.RegularExpressions;

namespace PromptLab.Demos;

/// <summary>
/// Demo 4 · Run it three times (the assignment in miniature).
/// Task: write a commit message for the PR. v1 is loose, v2 adds a format, v3 adds an example.
/// We run each version 3 times and score it with plain C#, no human judgement needed.
/// </summary>
public static partial class D4_RunItThreeTimes
{
    static readonly (string Name, string System)[] Versions =
    [
        ("v1 · loose", "Write a commit message for this change."),
        ("v2 · + format rules", """
            Write a commit message for this change.
            Use Conventional Commits: type(scope): summary
            Imperative mood, max 72 characters, no trailing period.
            Output only the commit message, nothing else.
            """),
        ("v3 · + one example", """
            Write a commit message for this change.
            Use Conventional Commits: type(scope): summary
            Imperative mood, max 72 characters, no trailing period.
            Output only the commit message, nothing else.

            Example of a good message:
            fix(orders): guard against null customer in checkout
            """),
    ];

    [GeneratedRegex(@"^(feat|fix|refactor|perf|chore|docs|test|build|ci|style|security)(\([\w\-\.]+\))?!?: [^\n]{1,}$")]
    private static partial Regex Conventional();

    public static bool Passes(string s) => s.Length <= 72 && !s.EndsWith('.') && Conventional().IsMatch(s);

    const string Change = """
        Change: UserService.DeleteUser now uses a parameterised SqlCommand instead of string
        concatenation, is async Task instead of async void, and gets HttpClient from IHttpClientFactory.
        """;

    public static async Task Run()
    {
        Ui.Title("Demo 4 · Run it three times");
        Ui.Dim("   Pass = Conventional Commit format, ≤ 72 chars, no trailing period. Checked with a Regex.");
        Ui.Pause();

        foreach (var (name, system) in Versions)
        {
            Ui.Label(name);
            int passed = 0;
            for (int run = 1; run <= 3; run++)
            {
                var r = await Llm.AskAsync(system, Change, temperature: 0.9, maxTokens: 200);
                var msg = r.Content.Trim();
                bool ok = Passes(msg);
                if (ok) passed++;
                var shown = Ui.Clip(msg.Replace("\n", " ⏎ "), 90);
                if (ok) Ui.Good($"run {run}: {shown}");
                else Ui.Bad($"run {run}: {shown}");
            }
            Ui.Write(passed == 3 ? ConsoleColor.Green : ConsoleColor.Yellow, $"   score: {passed}/3");
            Ui.Pause();
        }
        Ui.Good("A prompt isn't done when it works once. It's done when it works every time you run it.");
    }
}
