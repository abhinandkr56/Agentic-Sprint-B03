using Microsoft.Extensions.VectorData;

namespace MyAiApi;

// Requests and responses of YOUR API (Sessions 3–7, unchanged)
public record AskRequest(string Question);
public record AskResponse(string Answer, string Provider, string Model);
public record TriageRequest(string Text);
public record TicketTriage(string Category, string Priority, string Sentiment, string OneLineSummary);
public record Source(int Id, string Topic, string Text, double Score);
public record RagResponse(string Answer, string Provider, string Model, List<Source> Sources);

// Session 7: the answer, plus which tools the model called (local AND, from tonight, MCP tools)
public record ChatReply(string Answer, string Provider, string Model, List<string> ToolCalls);

// Session 5's record: one note = one row in the "notes" table. If yours looked different in Session 5, keep yours.
public class Note
{
    [VectorStoreKey] public int Id { get; set; }
    [VectorStoreData] public string Topic { get; set; } = "";
    [VectorStoreData] public string Text { get; set; } = "";
    [VectorStoreVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] public string Embedding => Text;
}

public static class Prompts
{
    public const string Triage = """
        You triage customer support tickets for an Indian SaaS company.
        Category must be one of: Billing, Bug, HowTo, FeatureRequest, Cancellation.
        Priority must be one of: Low, Medium, High. Money lost or app down = High.
        Sentiment must be one of: Positive, Neutral, Negative.
        OneLineSummary: max 8 words.
        """;

    public const string Rag = """
        You answer questions using our team's notes.
        Use ONLY the notes you are given. If they don't contain the answer, reply exactly:
        "I don't know. It isn't in our notes."
        Cite the note number, like [3]. Reply in the language of the question.
        """;

    // TODO 4 · tell the model when to use the MCP tool, and to cite it
    public const string Chat = """
        You are our team's assistant. You have tools: use them when the question needs data they provide.
        GetOnCall: who is on call. SearchNotes: our team's notes. microsoft_docs_search: .NET and Azure questions.
        GetOrder: a customer's order, when they give its number.
        Don't guess arguments: if something the tool needs is missing, ask for it.
        Answer in two or three sentences. When you used microsoft_docs_search, end with the learn.microsoft.com link.
        """;
}
