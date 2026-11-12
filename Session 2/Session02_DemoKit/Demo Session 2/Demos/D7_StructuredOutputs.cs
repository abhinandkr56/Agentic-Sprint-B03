using System.Text.Json;
using System.Text.Json.Nodes;

namespace PromptLab.Demos;

/// <summary>
/// Demo 7 · Structured outputs (json_schema, strict).
/// The provider constrains decoding to our schema, so the shape is guaranteed, not requested.
/// Uses STRICT_MODEL (default openai/gpt-oss-20b) because on Groq strict mode is only
/// available on some models. That itself is a lesson: capabilities differ per model.
/// </summary>
public static class D7_StructuredOutputs
{
    const string System = """
        You are a senior .NET reviewer. Review the pull request the user sends.
        Report at most 3 issues, most severe first. Cite line numbers from the snippet.
        """;

    public static async Task Run()
    {
        Ui.Title("Demo 7 · Structured outputs (strict JSON Schema)");
        Ui.Label($"Model: {Llm.StrictModel}");
        Ui.Dim("   response_format: { type: json_schema, json_schema: { name, strict: true, schema } }");
        Ui.Box(Ui.Clip(Schemas.CodeReview.Trim(), 520), ConsoleColor.DarkCyan);
        Ui.Pause();

        var extra = new JsonObject { ["reasoning_effort"] = "low" }; // gpt-oss is a reasoning model; keep it quick
        var r = await Llm.AskAsync(System, Samples.PullRequest, temperature: 0.2, maxTokens: 2000,
            responseFormat: Llm.JsonSchema("code_review", Schemas.CodeReview), model: Llm.StrictModel, extra: extra);

        Ui.Label("Raw content:");
        Ui.Box(Ui.Clip(Json.Pretty(r.Content), 900), ConsoleColor.Gray);
        Ui.Dim("   " + r.Stats);

        var review = JsonSerializer.Deserialize<CodeReview>(r.Content, Json.App)!;
        Console.WriteLine();
        Ui.Label("Typed C#:");
        D6_JsonMode.Show(review);
        Console.WriteLine();

        // The payoff: ordinary C# logic on model output.
        if (review.Severity >= Severity.High)
            Ui.Write(ConsoleColor.Red, $"   ⛔ Blocking merge: {review.Severity} severity. {review.Issues.Count} issue(s).");
        else
            Ui.Write(ConsoleColor.Green, "   ✅ Non-blocking. Leave comments and approve.");
        Ui.Good("No regex, no string parsing, no retry loop. The schema is the contract.");
    }
}
