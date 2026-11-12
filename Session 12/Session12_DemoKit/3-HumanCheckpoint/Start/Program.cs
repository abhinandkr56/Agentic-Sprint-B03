// Build 3 · HumanCheckpoint (START): Session 11's writer → reviewer. When the reviewer approves, the reply goes out.
// Nobody else looks.
//   dotnet run      (GROQ_API_KEY)      Paste a ticket from chat; an empty line quits.
using System.ClientModel;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client. One client, many agents: each agent adds its own instructions on top.
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// Session 11's writer: gets the facts, uses only those
const string WriterRules = """
    You draft replies to customer support tickets for our SaaS product.
    You get the TICKET and the FACTS our systems returned. Use only those facts; never invent dates or amounts.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """;

// Session 11's reviewer: a checklist
const string ReviewRules = """
    You review draft replies to customer support tickets before a customer sees them.
    You get the TICKET, the FACTS our systems returned, and the DRAFT.
    Check: 1) every date, time frame, amount and refund status in the draft appears in FACTS;
    2) it promises nothing FACTS doesn't back up; 3) the tone is warm and never blames the customer;
    4) it is signed "Asha, Support".
    List each problem as one short sentence.
    """;

// What Session 10's GetOrder returned for this customer
const string Facts = "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.";

AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: WriterRules);
AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", instructions: ReviewRules);

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;
    var to = Regex.Match(ticket, @"[\w.+-]+@[\w-]+\.[\w.]+").Value is { Length: > 0 } email ? email : "the customer";
    var evidence = $"TICKET:\n{ticket}\n\nFACTS:\n{Facts}";

    // Session 11: the writer drafts (in its own session), the reviewer returns a typed verdict
    AgentSession session = await writer.CreateSessionAsync();
    var draft = await writer.RunAsync(evidence, session);
    var review = await reviewer.RunAsync<Review>($"{evidence}\n\nDRAFT:\n{draft.Text}");
    Console.WriteLine($"\n[SupportAgent]\n{draft.Text}\n\n[ReviewerAgent] approved: {review.Result.Approved} {string.Join(" · ", review.Result.Problems)}");

    // The final action: the reviewer's yes is enough.                                   ▼ EDIT 1: a person decides
    if (review.Result.Approved)
        SendReply(to, draft.Text);
    // ▼ EDIT 2: a "no" with a note goes back to the writer
    // ▼ EDIT 3: write down who decided what
}

// The final action: an email to a real customer. It can't be unsent. A stub: in real life, SendGrid, SES or your CRM.
static void SendReply(string to, string body) => Console.WriteLine($"\n  >>> EMAIL SENT to {to} <<<\n  {body.Split('\n')[0]}");

record Review(bool Approved, List<string> Problems);
