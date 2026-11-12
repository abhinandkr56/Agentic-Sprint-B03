using Microsoft.Extensions.VectorData;

namespace MyAiApi;

// Requests and responses of YOUR API (Sessions 3–10, unchanged)
public record AskRequest(string Question);
public record AskResponse(string Answer, string Provider, string Model);
public record TriageRequest(string Text);
public record TicketTriage(string Category, string Priority, string Sentiment, string OneLineSummary);
public record Source(int Id, string Topic, string Text, double Score);
public record RagResponse(string Answer, string Provider, string Model, List<Source> Sources);
public record ChatReply(string Answer, string Provider, string Model, List<string> ToolCalls);
public record ReplyRequest(string Ticket, string? ConversationId = null);
public record AgentReply(string Draft, string Provider, string Model, List<string> Steps);

// Session 11: the reviewer's verdict (the contract between the two agents) and what the endpoint returns
public record ReviewVerdict(bool Approved, List<string> Problems);
public record ReviewedReply(string Draft, bool Approved, List<string> Problems, int Rounds, List<string> Steps, string Provider, string Model);

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

    public const string Chat = """
        You are our team's assistant. You have tools: use them when the question needs data they provide.
        GetOnCall: who is on call. SearchNotes: our team's notes. microsoft_docs_search: .NET and Azure questions.
        GetOrder: a customer's order, when they give its number.
        Don't guess arguments: if something the tool needs is missing, ask for it.
        Answer in two or three sentences. When you used microsoft_docs_search, end with the learn.microsoft.com link.
        """;

    // Session 10's agent
    public const string Agent = """
        You draft replies to customer support tickets for our SaaS product.
        Look up the facts with your tools before you write: FindOrders when the customer gives only an email,
        GetOrder for an order's status, amount and refund. Never invent dates, amounts or refund status.
        If you can't find the order, ask for the order number or the email they ordered with.
        Refunds need a human: never promise one yourself.
        Reply in 3-4 sentences, warm and specific, and sign as "Asha, Support".
        """;

    // Session 11 · TODO 1: the reviewer's job, as a checklist (Build 1). This first version runs; make it yours.
    public const string Reviewer = """
        You review draft replies to customer support tickets.
        """;
}
