namespace PromptLab.Demos;

/// <summary>
/// Demo 1 · Vague vs specific.
/// Same code, same model. Prompt A is what most people type. Prompt B uses the five-part
/// anatomy from the slides: Role, Task, Context, Constraints, Format.
/// </summary>
public static class D1_VagueVsSpecific
{
    public const string VaguePrompt = "Review this code.";

    public const string SpecificSystem = """
        ROLE: You are a senior .NET reviewer on a payments team.
        TASK: Review the pull request the user sends you.
        CONTEXT: The code runs in an ASP.NET Core 8 API that handles customer data. The author is a mid-level developer.
        CONSTRAINTS: Report at most 3 issues, most severe first. Only real bugs or security problems, no style nitpicks.
        Be specific: cite the line number. If you are unsure, say so.
        FORMAT: One markdown bullet per issue: **[Severity] Title (line N)**: one-sentence fix.
        Then one final line: Verdict: MERGE or DO NOT MERGE.
        """;

    public static async Task Run()
    {
        Ui.Title("Demo 1 · Vague vs specific");
        Ui.Label("The pull request:");
        Ui.Box(Samples.PullRequest, ConsoleColor.Gray);
        Ui.Pause();

        Ui.Label($"Prompt A (vague): \"{VaguePrompt}\"");
        var a = await Llm.AskAsync(null, VaguePrompt + "\n\n" + Samples.PullRequest);
        Ui.Box(a.Content);
        Ui.Dim("   " + a.Stats);
        Ui.Pause("that's prompt A. Enter for prompt B");

        Ui.Label("Prompt B (role · task · context · constraints · format):");
        Ui.Box(SpecificSystem, ConsoleColor.DarkCyan);
        var b = await Llm.AskAsync(SpecificSystem, Samples.PullRequest);
        Ui.Box(b.Content, ConsoleColor.Green);
        Ui.Dim("   " + b.Stats);

        Console.WriteLine();
        Ui.Label("Side by side");
        Console.WriteLine($"   A: {a.Content.Length,5} chars · {a.CompletionTokens,4} output tokens");
        Console.WriteLine($"   B: {b.Content.Length,5} chars · {b.CompletionTokens,4} output tokens");
        Ui.Good("Which one could you paste straight into a PR comment?");
    }
}
