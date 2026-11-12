// Bonus · FrameworkEvals: two things you can SHOW if there's time (no live typing). Not in the 60-minute plan.
//   1) A judge with no LLM: F1 word overlap (Microsoft.Extensions.AI.Evaluation.NLP, still preview). Free, instant, same every time.
//   2) The Agent Framework's own eval helper: agent.EvaluateAsync runs the agent on every query, N times, and checks it.
//   dotnet run      (GROQ_API_KEY)
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.NLP;
using OpenAI;

// Session 3's chat client
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// 1) F1: what fraction of the words match the expected answer? No model involved.
const string Expected = "The refund of Rs 10,989 was issued on 2 Oct and should arrive by 9 Oct.";
string[] answers =
[
    "Your refund of Rs 10,989 was issued on 2 Oct and should arrive by 9 Oct.",
    "Your refund went out on October 2 and should land by October 9.",
    "Your refund is on its way and will reach your account within 5-7 business days.",
];
Console.WriteLine("F1 (word overlap, no LLM, 0-1, 0.5 passes):");
foreach (var answer in answers)
{
    var f1 = (await new F1Evaluator().EvaluateAsync(answer, additionalContext: [new F1EvaluatorContext(Expected)])).Get<NumericMetric>(F1Evaluator.F1MetricName);
    Console.WriteLine($"  {f1.Value:0.00} {(f1.Interpretation?.Failed == true ? "FAIL" : "PASS")}  {answer}");
}

// 2) The Agent Framework 1.26 evaluates agents too: run every query 3 times, check each answer and its tool calls
// Session 10's agent, read-only: one tool
const string Rules = """
    You answer customers of our SaaS product. Look up the order with GetOrder before you answer.
    Never invent dates, amounts or refund status. If there's no order number, ask for it.
    Reply in 1-2 sentences and sign as "Asha, Support".
    """;
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: [AIFunctionFactory.Create(GetOrder, "GetOrder")]);
var results = await agent.EvaluateAsync(
    queries: ["When will my refund for order 4411 arrive?", "Is my order 4412 still active?"],
    evaluator: new LocalEvaluator(EvalChecks.ContainsExpected(), EvalChecks.ToolCalledCheck("GetOrder")),
    expectedOutput: ["9 Oct", "Active"],
    numRepetitions: 3);
Console.WriteLine($"\nagent.EvaluateAsync: {results.Passed}/{results.Total} passed (2 queries x 3 runs)");
for (var i = 0; i < results.Total; i++)
    Console.WriteLine($"  {results.InputItems![i].Query,-45} " + string.Join("  ", results.Items[i].Metrics.Values.Select(m => $"{m.Name}: {(m.Interpretation?.Failed == true ? "FAIL" : "pass")}")));

// Session 7's tool, unchanged: a stub order system. It only READS.
[Description("Gets an order's status, amount and refund details. Only call it when the customer gives an order number; otherwise ask for it.")]
static string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.WriteLine($"  [tool] GetOrder({orderId})");
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}
