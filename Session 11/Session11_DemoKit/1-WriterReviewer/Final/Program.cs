// Build 1 · WriterReviewer (FINAL): one agent drafts a reply. Who checks it before a customer reads it?
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
// EDIT 2: a second agent with a different job: its own instructions, its own (empty) memory. Same chat client.
AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", instructions: ReviewRules);

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    AgentSession session = await writer.CreateSessionAsync();
    var draft = await writer.RunAsync(ticket, session);
    Console.WriteLine($"\n[SupportAgent · draft]\n{draft.Text}");

    // EDIT 1: ask the writer to check its own work. Same agent, same session, same blind spots.
    var selfCheck = await writer.RunAsync("Check your draft before we send it. Is it OK? Reply APPROVED or NOT APPROVED.", session);
    Console.WriteLine($"\n[SupportAgent · checks itself]\n{selfCheck.Text}");
    // EDIT 2: our code hands the reviewer what it needs to check the draft. Nothing else.
    var review = await reviewer.RunAsync($"TICKET:\n{ticket}\n\nFACTS:\n(none: this writer has no tools yet)\n\nDRAFT:\n{draft.Text}");
    Console.WriteLine($"\n[ReviewerAgent · review]\n{review.Text}");
    // EDIT 3: what did the checking cost? Every agent call is a model call: two agents, two bills.
    Console.WriteLine($"\n  tokens in · draft {draft.Usage?.InputTokenCount:N0} · self-check {selfCheck.Usage?.InputTokenCount:N0} · review {review.Usage?.InputTokenCount:N0}");
}
