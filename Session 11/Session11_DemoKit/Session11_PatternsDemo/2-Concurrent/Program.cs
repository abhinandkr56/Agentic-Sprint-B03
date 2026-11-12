// Pattern 2 · CONCURRENT: the SAME input goes to several agents at the same time; their answers are collected.
// Use it when the checks are independent of each other (several reviewers, several opinions, several languages).
//   dotnet run      (GROQ_API_KEY)
//
//                 ┌─▶ ToneReviewer   ─┐
//   draft ────────┼─▶ FactsReviewer  ─┼─▶ all three verdicts
//                 └─▶ PolicyReviewer ─┘
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// A draft with three different kinds of problem in it
const string Input = """
    TICKET:
    Where is my refund for order 4411?? It's been ages.

    FACTS:
    Order 4411: Pro plan, annual, Rs 11,988. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.

    DRAFT:
    Calm down, your refund of Rs 10,989 is processed and will reach you within 3 days. Thanks
    """;

// Three reviewers, each with ONE narrow job
AIAgent tone = chat.AsAIAgent(name: "ToneReviewer", instructions: """
    You review only the TONE of a draft support reply: warm, polite, never blames or talks down to the customer.
    Ignore facts and policy. Start with APPROVED or NOT APPROVED, then one line per problem.
    """);

AIAgent facts = chat.AsAIAgent(name: "FactsReviewer", instructions: """
    You review only the FACTS of a draft support reply: every date, time frame, amount and refund status must appear in FACTS.
    Ignore tone and policy. Start with APPROVED or NOT APPROVED, then one line per problem.
    """);

AIAgent policy = chat.AsAIAgent(name: "PolicyReviewer", instructions: """
    You review only POLICY in a draft support reply: no guarantees, no promises FACTS don't back up, signed "Asha, Support".
    Ignore tone. Start with APPROVED or NOT APPROVED, then one line per problem.
    """);

// THE PATTERN: fan out to all three, then fan in. The default aggregator keeps each agent's last message.
Workflow workflow = AgentWorkflowBuilder.BuildConcurrent([tone, facts, policy]);

// Run it "the long way" (Microsoft Learn's sample): start the run, send a TurnToken to start the agents,
// then wait for the ONE output event that carries the merged result.
// (workflow.AsAIAgent() works too, but three agents streaming at once arrive as interleaved chunks.)
var timer = Stopwatch.StartNew();
List<ChatMessage> verdicts = [];
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new List<ChatMessage> { new(ChatRole.User, Input) });
await run.TrySendMessageAsync(new TurnToken(emitEvents: false));
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is WorkflowOutputEvent output) { verdicts = output.As<List<ChatMessage>>()!; break; }
}
timer.Stop();

foreach (var message in verdicts)
    Console.WriteLine($"\n[{message.AuthorName}]\n{message.Text}");

// Approved only if EVERY reviewer approved. (For a typed verdict, use Task.WhenAll + RunAsync<Review>: assignment stretch S3.)
bool approved = verdicts.All(m => m.Text.TrimStart().StartsWith("APPROVED"));
Console.WriteLine($"\n{verdicts.Count} reviewers ran side by side in {timer.ElapsedMilliseconds} ms · overall: {(approved ? "APPROVED" : "NOT APPROVED")}");
