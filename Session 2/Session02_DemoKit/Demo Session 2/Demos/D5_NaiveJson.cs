using System.Text.Json;

namespace PromptLab.Demos;

/// <summary>
/// Demo 5 · "Just ask for JSON" (the naive way).
/// No JSON mode, just a polite request. We try to deserialize each answer into CodeReview.
/// Typical failures: ```json fences, a friendly preamble, trailing commentary, "severity": "high risk".
/// </summary>
public static class D5_NaiveJson
{
    const string System = """
        You are a senior .NET reviewer. Review the pull request and return JSON with the fields
        summary, severity, issues (title, severity, line, fix) and safeToMerge.
        """;

    public static async Task Run()
    {
        Ui.Title("Demo 5 · Just ask for JSON");
        Ui.Dim("   No response_format. Temperature 0.7. We call JsonSerializer.Deserialize<CodeReview> on the raw text.");
        Ui.Pause();

        int ok = 0;
        for (int run = 1; run <= 3; run++)
        {
            var r = await Llm.AskAsync(System, Samples.PullRequest, temperature: 0.7, maxTokens: 900);
            Ui.Label($"Run {run} · first 160 chars of what came back:");
            Ui.Box(Ui.Clip(r.Content, 160), ConsoleColor.Gray);
            try
            {
                var review = JsonSerializer.Deserialize<CodeReview>(r.Content, Json.App);
                Ui.Good($"Parsed. Severity = {review?.Severity}, issues = {review?.Issues?.Count}");
                ok++;
            }
            catch (JsonException ex)
            {
                Ui.Bad("JsonException: " + Ui.Clip(ex.Message.Split('\n')[0], 120));
            }
            Ui.Pause();
        }
        Ui.Write(ok == 3 ? ConsoleColor.Green : ConsoleColor.Yellow, $"   Parsed {ok}/3.");
        Ui.Warn("\"Usually works\" is not a contract. Production code needs \"always works\".");
    }
}
