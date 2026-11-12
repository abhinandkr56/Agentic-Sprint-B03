namespace MyAiApi;

// Requests and responses of YOUR API
public record AskRequest(string Question);
public record AskResponse(string Answer, string Provider, string Model);
public record TriageRequest(string Text);

// ── TODO 5 · Replace this with your Session 2 record ────────────────────────
// Tip: turn string fields that come from a fixed set into enums (with [JsonConverter(typeof(JsonStringEnumConverter))]).
// Microsoft.Extensions.AI puts the enum values into the JSON Schema, so the model can only pick from them.
public record TicketTriage(string Category, string Priority, string Sentiment, string OneLineSummary);

public static class Prompts
{
    // ── TODO 5 · Replace this with your best (v3) prompt from Session 2 ─────
    public const string Triage = """
        You triage customer support tickets for an Indian SaaS company.
        Category must be one of: Billing, Bug, HowTo, FeatureRequest, Cancellation.
        Priority must be one of: Low, Medium, High. Money lost or app down = High.
        Sentiment must be one of: Positive, Neutral, Negative.
        OneLineSummary: max 8 words.
        """;
}
