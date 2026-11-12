// Build 2 · ReviewLoop (FINAL): Build 1's two agents. Now our CODE has to decide: send it, or send it back?
//   dotnet run      (GROQ_API_KEY)      Paste a ticket from chat; an empty line quits.
using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client. One client, many agents: each agent adds its own instructions on top.
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// The writer: Session 10's SupportAgent as a quick first draft. No tools yet, and a prompt that says "reassure them".
const string WriterRules = """
    You draft replies to customer support tickets for our SaaS product.
    Be warm, reassure the customer and tell them what happens next.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """;

// The reviewer: a different job, written as a checklist
const string ReviewRules = """
    You review draft replies to customer support tickets before a customer sees them.
    You get the TICKET, the FACTS our systems returned, and the DRAFT.
    Check: 1) every date, time frame, amount and refund status in the draft appears in FACTS;
    2) it promises nothing FACTS doesn't back up; 3) the tone is warm and never blames the customer;
    4) it is signed "Asha, Support".
    Start with APPROVED or NOT APPROVED, then list each problem on its own line.
    """;

const string Facts = "(none: this writer has no tools yet)";

// Build 1's two agents
AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: WriterRules);
AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", instructions: ReviewRules);

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    AgentSession session = await writer.CreateSessionAsync();
    var draft = await writer.RunAsync(ticket, session);
    Console.WriteLine($"\n[SupportAgent · round 1]\n{draft.Text}");

    var review = await reviewer.RunAsync<Review>($"TICKET:\n{ticket}\n\nFACTS:\n{Facts}\n\nDRAFT:\n{draft.Text}");   // EDIT 1: a typed verdict
    Console.WriteLine($"\n[ReviewerAgent]\n{review.Text}");

    // Our code needs a yes or a no
    bool approved = review.Result.Approved;                                              // EDIT 1: no string matching
    foreach (var problem in review.Result.Problems) Console.WriteLine($"  - {problem}");
    Console.WriteLine($"  approved: {approved}");
    // EDIT 2: the loop. Not approved? The problems go back to the SAME writer session. At most two more rounds.
    for (var round = 2; !approved && round <= 3; round++)
    {
        draft = await writer.RunAsync($"Our reviewer found these problems. Fix them and send the whole reply again:\n- {string.Join("\n- ", review.Result.Problems)}", session);
        Console.WriteLine($"\n[SupportAgent · round {round}]\n{draft.Text}");
        review = await reviewer.RunAsync<Review>($"TICKET:\n{ticket}\n\nFACTS:\n{Facts}\n\nDRAFT:\n{draft.Text}");
        approved = review.Result.Approved;
        Console.WriteLine($"  approved: {approved}");
        foreach (var problem in review.Result.Problems) Console.WriteLine($"  - {problem}");
    }
}
// EDIT 1: the contract between the two agents. The reviewer must answer in exactly this shape (JSON schema).
record Review(bool Approved, List<string> Problems);
