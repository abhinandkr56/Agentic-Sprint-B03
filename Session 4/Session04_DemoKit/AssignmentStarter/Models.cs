namespace MyAiApi;

// Requests and responses of YOUR API (unchanged from Session 3)
public record AskRequest(string Question);
public record AskResponse(string Answer, string Provider, string Model);
public record TriageRequest(string Text);

// Your Session 2/3 record. If you replaced it in Session 3, keep yours.
public record TicketTriage(string Category, string Priority, string Sentiment, string OneLineSummary);

public static class Prompts
{
    // Your Session 2/3 prompt. If you replaced it in Session 3, keep yours.
    public const string Triage = """
        You triage customer support tickets for an Indian SaaS company.
        Category must be one of: Billing, Bug, HowTo, FeatureRequest, Cancellation.
        Priority must be one of: Low, Medium, High. Money lost or app down = High.
        Sentiment must be one of: Positive, Neutral, Negative.
        OneLineSummary: max 8 words.
        """;
}
