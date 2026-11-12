// Build 1 · WriterReviewer (START): one agent drafts a reply. Who checks it before a customer reads it?
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

AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: WriterRules);
// ▼ EDIT 2: a second agent with a different job

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    AgentSession session = await writer.CreateSessionAsync();
    var draft = await writer.RunAsync(ticket, session);
    Console.WriteLine($"\n[SupportAgent · draft]\n{draft.Text}");

    // ▼ EDIT 1: ask the writer to check its own work
    // ▼ EDIT 2: hand the draft to the reviewer
    // ▼ EDIT 3: what did the checking cost?
}
