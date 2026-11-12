using Microsoft.Extensions.VectorData;

namespace MyAiApi;

// Requests and responses of YOUR API (Sessions 3–4, unchanged)
public record AskRequest(string Question);
public record AskResponse(string Answer, string Provider, string Model);
public record TriageRequest(string Text);
public record TicketTriage(string Category, string Priority, string Sentiment, string OneLineSummary);

// NEW in Session 6: a grounded answer and the notes it came from
public record Source(int Id, string Topic, string Text, double Score);
public record RagResponse(string Answer, string Provider, string Model, List<Source> Sources);

// Session 5's record: one note = one row in the "notes" table.
// If your Session 5 class looked different (other names, another size), make this match it.
public class Note
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Topic { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}

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

    // NEW · Build 3's rules, reworded for notes. Tune them for your texts (stretch S5).
    public const string Rag = """
        You answer questions using our team's notes.
        Use ONLY the notes you are given. If they don't contain the answer, reply exactly:
        "I don't know. It isn't in our notes."
        Cite the note number, like [3]. Reply in the language of the question.
        """;
}
