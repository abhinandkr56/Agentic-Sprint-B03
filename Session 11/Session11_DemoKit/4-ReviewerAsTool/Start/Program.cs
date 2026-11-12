// Build 4 · ReviewerAsTool (START): what if the WRITER decides when to ask the reviewer?
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

// The writer is told to ask the reviewer. Will it?
const string WriterRules = """
    You draft replies to customer support tickets for our SaaS product.
    Be warm, reassure the customer and tell them what happens next.
    Before you answer, ask ReviewerAgent to check your draft, and fix anything it finds.
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

// Build 1's reviewer. Its name and description are what the writer will see as a tool.
AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", description: "Checks a draft support reply against our rules before it is sent.", instructions: ReviewRules);

// ▼ EDIT 1: the reviewer becomes one of the writer's tools
AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: WriterRules);   // ▼ EDIT 1: + tools
// ▼ EDIT 2: see exactly what the writer sends the reviewer

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    var response = await writer.RunAsync(ticket);

    // Every step the writer took (Session 10, Build 3)
    var step = 0;
    foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
        Console.WriteLine($"  step {++step}: {call.Name}");
    Console.WriteLine($"  [{step} tool call(s)]\n\n{response.Text}");
}
