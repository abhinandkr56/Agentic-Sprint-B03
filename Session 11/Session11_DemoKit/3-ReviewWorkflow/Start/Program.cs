// Build 3 · ReviewWorkflow (START): two agents wired by hand. The Agent Framework's Workflows can do the wiring.
//   dotnet run      (GROQ_API_KEY)      Paste a ticket from chat; an empty line quits.
using System.ClientModel;
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

// The writer, now given the facts (Build 5 lets a tool fetch them)
const string WriterRules = """
    You draft replies to customer support tickets for our SaaS product.
    You get the TICKET and the FACTS our systems returned. Use only those facts; never invent dates or amounts.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """;

// The reviewer (Build 2's checklist, plus: say so when you can't see what you need)
const string ReviewRules = """
    You review draft replies to customer support tickets before a customer sees them.
    You get the customer's TICKET, the FACTS our systems returned, and SupportAgent's DRAFT.
    Check: 1) every date, time frame, amount and refund status in the draft appears in FACTS;
    2) it promises nothing FACTS doesn't back up; 3) the tone is warm and never blames the customer;
    4) it is signed "Asha, Support".
    If you can't see the TICKET or the FACTS, you can't check the draft: say NOT APPROVED and say what's missing.
    Start with APPROVED or NOT APPROVED, then list each problem on its own line.
    """;

// What Session 10's GetOrder returned for this customer
const string Facts = "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.";

AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: WriterRules);
AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", instructions: ReviewRules);
// ▼ EDIT 1: the same two agents as a workflow
// ▼ EDIT 2: draw it

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;
    var input = $"TICKET:\n{ticket}\n\nFACTS:\n{Facts}";

    // By hand: run the writer, paste its draft into the reviewer's input                  ▼ EDIT 1: let the workflow do it
    var draft = await writer.RunAsync(input);
    var review = await reviewer.RunAsync($"{input}\n\nDRAFT:\n{draft.Text}");
    List<ChatMessage> messages = [.. draft.Messages, .. review.Messages];
    Console.WriteLine($"\n— by hand: {messages.Count} messages —");

    foreach (var m in messages)
        Console.WriteLine($"\n[{m.AuthorName}]\n{m.Text}");
}
