// Pattern 5 · MAGENTIC: a MANAGER agent makes a plan, picks which team member works next, checks progress,
// and writes the final answer. Nobody codes the order of steps: the manager decides it while the task runs.
// Use it for open-ended tasks where you can't list the steps in advance. Most flexible, most model calls.
//   dotnet run      (GROQ_API_KEY)
//
//              ┌──── plan, pick next, check progress ────┐
//   task ─▶ Manager ─▶ OrderAgent (GetOrder) / SupportAgent ─▶ … ─▶ final answer
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;   // the Magentic event types
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// The manager: no tools. Its job is to plan and coordinate.
AIAgent manager = chat.AsAIAgent(name: "Manager",
    instructions: "You coordinate a small customer support team. Plan the work, pick who works next, and decide when the task is done.");

// The team. The manager reads each NAME and DESCRIPTION to decide who should do what.
AIAgent orders = chat.AsAIAgent(name: "OrderAgent", description: "Looks up order status, amounts and refund dates with the GetOrder tool",
    instructions: "You look up orders with GetOrder and report the facts exactly. You never write to customers.",
    tools: [AIFunctionFactory.Create(GetOrder, "GetOrder")]);

AIAgent writer = chat.AsAIAgent(name: "SupportAgent", description: "Writes the reply to the customer, using only facts the team has found",
    instructions: "You draft replies to customers using ONLY facts the team has found. Reply in 3-4 sentences and sign as \"Asha, Support\".");

// THE PATTERN: a manager + a team. Two settings worth knowing:
Workflow workflow = AgentWorkflowBuilder.CreateMagenticBuilderWith(manager)
    .AddParticipants(orders, writer)
    .RequirePlanSignoff(false)   // in 1.25.0 the default is TRUE: the run pauses for a human to approve the plan (Session 12)
    .WithMaxRounds(8)            // a cap, like every loop tonight
    .Build();
const string Task = "Customer Priya (order 4411) says her refund never arrived and wants to know exactly when it will. Find the facts and write her a reply.";

// Run it with events, so we can WATCH the manager think: its plan, each "who's next", and the final answer.
await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new List<ChatMessage> { new(ChatRole.User, Task) });
await run.TrySendMessageAsync(new TurnToken(emitEvents: false));
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    switch (evt)
    {
        case MagenticPlanCreatedEvent plan:                      // 1 · the manager made a plan
            var text = plan.FullTaskLedger.Text;
            var start = text.IndexOf("Here is the plan");
            Console.WriteLine($"── Manager's plan\n{(start >= 0 ? text[start..] : text).Trim()}\n");
            break;
        case MagenticProgressLedgerUpdatedEvent step:            // 2 · after every turn: done? if not, who's next and what to do
            var ledger = step.ProgressLedger;
            Console.WriteLine(ledger.IsRequestSatisfied
                ? "── Manager: the request is satisfied. Writing the final answer.\n"
                : $"── Manager → {ledger.NextSpeaker}: {ledger.InstructionOrQuestion}");
            break;
        case WorkflowOutputEvent output:                         // 3 · the final answer
            foreach (var message in output.As<List<ChatMessage>>()!)
                Console.WriteLine($"── Final answer ({message.AuthorName})\n{message.Text}");
            break;
    }
}

// Session 7's tool, unchanged: a stub order system
[Description("Gets an order's status, amount and refund details. Only call it when the customer gives an order number; otherwise ask for it.")]
static string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.WriteLine($"  [tool] GetOrder({orderId})");
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active. Payment log: one charge of Rs 499 on 1 Oct.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}
