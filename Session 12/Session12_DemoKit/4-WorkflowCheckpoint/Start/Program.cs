// Build 4 · WorkflowCheckpoint (START): Session 11's workflow drafts and reviews. If the reviewer says APPROVED,
// the email goes out. Where does a person fit in a workflow?
//   dotnet run      (GROQ_API_KEY)      Paste a ticket from chat; an empty line quits.
using System.ClientModel;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
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

// Session 11's reviewer, prose verdict (Session 11, Build 3)
const string ReviewRules = """
    You review draft replies to customer support tickets before a customer sees them.
    Check: 1) every date, time frame, amount and refund status in the draft appears in FACTS;
    2) it promises nothing FACTS doesn't back up; 3) the tone is warm; 4) it is signed "Asha, Support".
    Start with APPROVED or NOT APPROVED, then list each problem on its own line.
    """;

// What Session 10's GetOrder returned for this customer
const string Facts = "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.";

AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: WriterRules);
AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", instructions: ReviewRules);

// Session 11, Build 3: the two agents as one sequential workflow, run like any agent
AIAgent pipeline = AgentWorkflowBuilder.BuildSequential(writer, reviewer).AsAIAgent(name: "ReviewedReply");

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;
    var to = Regex.Match(ticket, @"[\w.+-]+@[\w-]+\.[\w.]+").Value is { Length: > 0 } email ? email : "the customer";

    // A workflow keeps a session like any agent (Session 10): EDIT 2 needs it
    AgentSession session = await pipeline.CreateSessionAsync();
    var response = await pipeline.RunAsync($"TICKET:\n{ticket}\n\nFACTS:\n{Facts}", session);
    var draft = response.Messages.Last(m => m.AuthorName == "SupportAgent").Text;
    var verdict = response.Messages.Last(m => m.AuthorName == "ReviewerAgent").Text;
    Console.WriteLine($"\n[SupportAgent]\n{draft}\n\n[ReviewerAgent]\n{verdict}");

    // The final action: the workflow's verdict is enough.                               ▼ EDIT 1: a person decides
    if (verdict.TrimStart().StartsWith("APPROVED"))
        SendReply(to, draft);
}

// The final action: an email to a real customer. It can't be unsent. A stub: in real life, SendGrid, SES or your CRM.
static void SendReply(string to, string body) => Console.WriteLine($"\n  >>> EMAIL SENT to {to} <<<\n  {body.Split('\n')[0]}");
