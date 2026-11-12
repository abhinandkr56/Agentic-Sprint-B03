using System.Text.Json;
using System.Text.Json.Serialization;

namespace PromptLab;

// ── Wire format (what Groq / OpenAI-compatible APIs send back) ──────────────
public record ChatResponse(string Id, string Model, List<Choice> Choices, Usage? Usage);
public record Choice(int Index, WireMessage Message, string? FinishReason);
public record WireMessage(string Role, string? Content);
public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);

// ── The running example: the CodeReview object promised at the end of Session 1 ──
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Severity { Low, Medium, High, Critical }

public record CodeIssue(string Title, Severity Severity, int? Line, string Fix);

public record CodeReview(string Summary, Severity Severity, List<CodeIssue> Issues, bool SafeToMerge);

// ── Second example: support-ticket triage (used in Demo 3 and the assignment) ──
public record TicketTriage(string Category, string Priority, string Sentiment, string OneLineSummary);

public static class Json
{
    /// <summary>For the provider's wire format: snake_case (prompt_tokens, finish_reason...).</summary>
    public static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>For our own objects: camelCase JSON in, C# records out.</summary>
    public static readonly JsonSerializerOptions App = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static string Pretty(string json)
    {
        try { return JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement, new JsonSerializerOptions { WriteIndented = true }); }
        catch { return json; }
    }
}

/// <summary>
/// The JSON Schema for CodeReview, written by hand so you can see every rule strict mode needs:
/// every property listed in "required", additionalProperties:false on every object,
/// and "optional" expressed as a union with null.
/// (.NET 9 can generate this for you with JsonSchemaExporter; Session 3's IChatClient does it automatically.)
/// </summary>
public static class Schemas
{
    public const string CodeReview = """
    {
      "type": "object",
      "properties": {
        "summary":     { "type": "string", "description": "One sentence verdict for a busy tech lead" },
        "severity":    { "type": "string", "enum": ["Low", "Medium", "High", "Critical"], "description": "Worst issue found" },
        "issues": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "title":    { "type": "string" },
              "severity": { "type": "string", "enum": ["Low", "Medium", "High", "Critical"] },
              "line":     { "type": ["integer", "null"], "description": "Line number in the snippet, or null" },
              "fix":      { "type": "string", "description": "Concrete fix in one or two sentences" }
            },
            "required": ["title", "severity", "line", "fix"],
            "additionalProperties": false
          }
        },
        "safeToMerge": { "type": "boolean" }
      },
      "required": ["summary", "severity", "issues", "safeToMerge"],
      "additionalProperties": false
    }
    """;
}
