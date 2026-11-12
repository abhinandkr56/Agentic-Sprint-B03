// Pattern 1 · SEQUENTIAL: A → B → C. Each agent's output becomes the next agent's input.
// Use it when the steps are known and always happen in the same order (an assembly line).
//   dotnet run      (GROQ_API_KEY)        or   dotnet run -- "your own ticket"
//
//   ticket ─▶ TriageAgent ─▶ SupportAgent ─▶ ReviewerAgent ─▶ result
using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client. One client, three agents: each adds its own instructions.
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

var ticket = args.Length > 0 ? string.Join(" ", args) : "I was charged twice for order 4412 and nobody answers my emails!! priya@example.com";
const string Facts = "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active. Payment log: one charge of Rs 499 on 1 Oct.";

// Three agents, three different jobs
AIAgent triage = chat.AsAIAgent(name: "TriageAgent", instructions: """
    You triage support tickets. Reply in two lines only:
    Category: Billing, Bug, HowTo or Cancellation
    Summary: the customer's problem in one sentence
    """);

AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: """
    You draft replies to customer support tickets. Use the triage summary and ONLY the FACTS you are given.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """);

AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", instructions: """
    You review draft replies to customer support tickets before a customer sees them.
    Check: 1) every date, amount and payment status in the draft appears in FACTS; 2) it promises nothing FACTS
    doesn't back up; 3) the tone is warm; 4) it is signed "Asha, Support".
    Start with APPROVED or NOT APPROVED, then list each problem on its own line.
    """);

// THE PATTERN: one line. The order of the arguments is the order they run in.
Workflow workflow = AgentWorkflowBuilder.BuildSequential(triage, writer, reviewer);

// A workflow can be run like any agent (Session 10)
AIAgent pipeline = workflow.AsAIAgent(name: "TicketPipeline");
var response = await pipeline.RunAsync($"TICKET:\n{ticket}\n\nFACTS:\n{Facts}");

// One message per agent, in the order they ran
foreach (var message in response.Messages.Where(m => !string.IsNullOrWhiteSpace(m.Text)))
    Console.WriteLine($"\n[{message.AuthorName}]\n{message.Text}");
