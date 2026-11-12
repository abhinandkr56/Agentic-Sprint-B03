// Build 3 · HumanCheckpoint (FINAL): Session 11's writer → reviewer. When the reviewer approves, the reply goes out.
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

    // EDIT 1: the checkpoint. The reviewer's verdict is advice; a person makes the call, right before the final action.
    Console.Write($"\nSend this to {to}? (y/n) ");
    var yes = Console.ReadLine()?.Trim().ToLower() == "y";
    if (yes) SendReply(to, draft.Text);
    else Console.WriteLine("  NOT SENT.");
    // EDIT 2: "no" + a note: the writer fixes ITS draft (same session), the reviewer checks again, and you decide again
    if (!yes)
    {
        Console.Write("  What should change? ");
        draft = await writer.RunAsync($"A teammate sent your reply back: {Console.ReadLine()}\nFix it and send the whole reply again.", session);
        review = await reviewer.RunAsync<Review>($"{evidence}\n\nDRAFT:\n{draft.Text}");
        Console.Write($"\n[SupportAgent · fixed]\n{draft.Text}\n\n[ReviewerAgent] approved: {review.Result.Approved}\n\nSend this to {to}? (y/n) ");
        yes = Console.ReadLine()?.Trim().ToLower() == "y";
        if (yes) SendReply(to, draft.Text);
        else Console.WriteLine("  NOT SENT. Dropped.");
    }
    // EDIT 3: an audit trail: who decided what, and when. The first thing an auditor (or your manager) asks for.
    File.AppendAllText("approvals.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {Environment.UserName} | {(yes ? "SENT" : "NOT SENT")} | {to} | reviewer: {review.Result.Approved}\n");
    Console.WriteLine($"  logged: {File.ReadLines("approvals.log").Last()}");
}

// The final action: an email to a real customer. It can't be unsent. A stub: in real life, SendGrid, SES or your CRM.
static void SendReply(string to, string body) => Console.WriteLine($"\n  >>> EMAIL SENT to {to} <<<\n  {body.Split('\n')[0]}");

record Review(bool Approved, List<string> Problems);
