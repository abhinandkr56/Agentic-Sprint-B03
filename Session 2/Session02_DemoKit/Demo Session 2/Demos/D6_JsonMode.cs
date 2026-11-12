using System.Text.Json;

namespace PromptLab.Demos;

/// <summary>
/// Demo 6 · JSON mode: valid JSON is not the same as the right JSON.
/// Part A: JSON mode + a vague prompt. It parses. The fields are silently wrong.
/// Part B: JSON mode + the exact shape + validate + one retry with the error fed back.
/// </summary>
public static class D6_JsonMode
{
    const string VagueSystem = "You are a senior .NET reviewer. Review the pull request. Respond in JSON.";

    public const string ShapedSystem = """
        You are a senior .NET reviewer. Review the pull request the user sends.
        Respond with ONLY a JSON object in exactly this shape (camelCase keys, no extra keys):
        {
          "summary": "one sentence for a busy tech lead",
          "severity": "Low | Medium | High | Critical  (the worst issue found)",
          "issues": [
            { "title": "short title", "severity": "Low | Medium | High | Critical", "line": 13, "fix": "one or two sentences" }
          ],
          "safeToMerge": false
        }
        Use null for "line" if it does not apply. Report at most 3 issues, most severe first.
        """;

    static readonly string[] RequiredKeys = ["summary", "severity", "issues", "safeToMerge"];

    public static async Task Run()
    {
        Ui.Title("Demo 6 · JSON mode");
        Ui.Dim("""   response_format: { "type": "json_object" }  → the provider guarantees syntactically valid JSON""");
        Ui.Pause();

        // ── Part A ──
        Ui.Label("Part A · JSON mode with a vague prompt");
        var a = await Llm.AskAsync(VagueSystem, Samples.PullRequest, temperature: 0.7, maxTokens: 900,
            responseFormat: Llm.JsonMode());
        Ui.Box(Ui.Clip(Json.Pretty(a.Content), 700), ConsoleColor.Gray);
        try
        {
            var ra = JsonSerializer.Deserialize<CodeReview>(a.Content, Json.App);
            Ui.Good("It parsed. No exception. Now look at the object:");
            Show(ra);
        }
        catch (JsonException ex)
        {
            // Also a lesson: valid JSON, but a value C# can't map (e.g. "severity": "high risk").
            Ui.Bad("Valid JSON, but it doesn't fit CodeReview: " + Ui.Clip(ex.Message.Split('\n')[0], 120));
        }
        var missing = MissingKeys(a.Content);
        if (missing.Count > 0)
            Ui.Bad($"Keys the model didn't send: {string.Join(", ", missing)}. Missing enums silently default to Severity.{default(Severity)}!");
        Ui.Pause("Enter for Part B");

        // ── Part B ──
        Ui.Label("Part B · JSON mode + exact shape + validate + retry once");
        var review = await ReviewWithRetry(Samples.PullRequest);
        if (review is not null)
        {
            Show(review);
            Console.WriteLine();
            Ui.Write(ConsoleColor.Green, $"   review.Severity → {review.Severity}");
            Ui.Good("That's the Session 1 promise: a typed C# object you can put in an if statement.");
        }
    }

    /// <summary>The production-shaped pattern: call → parse → validate → feed the error back once.</summary>
    public static async Task<CodeReview?> ReviewWithRetry(string code, int maxAttempts = 2)
    {
        var messages = new List<Msg> { Msg.System(ShapedSystem), Msg.User(code) };
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var r = await Llm.ChatAsync(messages, temperature: 0.2, maxTokens: 900, responseFormat: Llm.JsonMode());
            Ui.Dim($"   attempt {attempt}: {r.Stats}");
            string? error = null;
            CodeReview? review = null;
            try
            {
                review = JsonSerializer.Deserialize<CodeReview>(r.Content, Json.App);
                var missing = MissingKeys(r.Content);
                if (missing.Count > 0) error = $"Missing keys: {string.Join(", ", missing)}";
                else if (review?.Issues is null) error = "issues must be an array";
            }
            catch (JsonException ex) { error = ex.Message; }

            if (error is null) { Ui.Good("valid"); return review; }

            Ui.Bad("invalid: " + Ui.Clip(error, 120));
            messages.Add(Msg.Assistant(r.Content));
            messages.Add(Msg.User($"That JSON was invalid: {error}. Reply with ONLY the corrected JSON object."));
        }
        Ui.Bad("Gave up after retries. In production: log it, return a fallback, alert a human.");
        return null;
    }

    public static List<string> MissingKeys(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return [.. RequiredKeys];
            return RequiredKeys.Where(k => !doc.RootElement.TryGetProperty(k, out _)).ToList();
        }
        catch (JsonException) { return [.. RequiredKeys]; }
    }

    public static void Show(CodeReview? r)
    {
        if (r is null) { Ui.Bad("null"); return; }
        Console.WriteLine($"   Summary     : {r.Summary ?? "(null)"}");
        Console.WriteLine($"   Severity    : {r.Severity}");
        Console.WriteLine($"   SafeToMerge : {r.SafeToMerge}");
        Console.WriteLine($"   Issues      : {(r.Issues is null ? "(null)" : r.Issues.Count.ToString())}");
        foreach (var i in r.Issues ?? [])
            Console.WriteLine($"     - [{i.Severity}] {i.Title} (line {i.Line?.ToString() ?? "-"}): {i.Fix}");
    }
}
