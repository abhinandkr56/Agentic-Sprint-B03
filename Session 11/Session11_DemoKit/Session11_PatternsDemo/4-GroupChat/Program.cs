// Pattern 4 · GROUP CHAT: agents take turns in one shared conversation; a MANAGER decides who speaks next and when to stop.
// Use it for back-and-forth: a writer and a reviewer iterating until the reviewer is happy.
//   dotnet run      (GROQ_API_KEY)
//
//   SupportAgent ⇄ ReviewerAgent   (turns picked by a round-robin manager, stop on APPROVED, max 6 turns)
using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

AIAgent writer = chat.AsAIAgent(name: "SupportAgent", instructions: """
    You draft replies to customer support tickets. Be warm and reassure the customer.
    If ReviewerAgent lists problems, rewrite the WHOLE reply fixing every one of them.
    Reply in 3-4 sentences and sign as "Asha, Support".
    """);

AIAgent reviewer = chat.AsAIAgent(name: "ReviewerAgent", instructions: """
    You review draft replies to customer support tickets before a customer sees them.
    Check: 1) every date, time frame, amount and refund status in the draft appears in FACTS;
    2) it promises nothing FACTS doesn't back up; 3) the tone is warm; 4) it is signed "Asha, Support".
    Start with APPROVED or NOT APPROVED, then list each problem on its own line.
    """);

// THE PATTERN: participants + a manager. RoundRobinGroupChatManager takes turns in order (writer, reviewer, writer…).
// Our stop rule: the last message starts with APPROVED. The cap: 6 turns, whatever happens.
Workflow workflow = AgentWorkflowBuilder
    .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents,
        shouldTerminateFunc: (_, history, _) => ValueTask.FromResult(history.LastOrDefault()?.Text.TrimStart().StartsWith("APPROVED") == true))
    {
        MaximumIterationCount = 6,
    })
    .AddParticipants(writer, reviewer)
    .Build();
AIAgent room = workflow.AsAIAgent(name: "DraftRoom");

var response = await room.RunAsync("""
    TICKET:
    I cancelled last week. WHEN will I get my refund? I need a date!! priya@example.com

    FACTS:
    (none: nobody has looked up the order yet)
    """);

var turn = 0;
foreach (var message in response.Messages.Where(m => !string.IsNullOrWhiteSpace(m.Text)))
    Console.WriteLine($"\n── turn {++turn} · {message.AuthorName}\n{message.Text}");
