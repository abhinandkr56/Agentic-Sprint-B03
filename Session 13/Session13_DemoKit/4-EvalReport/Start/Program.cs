// Build 4 · EvalReport (START): Build 2's test cases, Build 3's judges. The scores scroll past and they're gone.
// Was today's run better than yesterday's? Nobody can say.
//   dotnet run      (GROQ_API_KEY)      Run it twice.
using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using OpenAI;

// Session 3's chat client
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// Session 10's agent, read-only: one tool
const string Rules = """
    You answer customers of our SaaS product. Look up the order with GetOrder before you answer.
    Never invent dates, amounts or refund status. If there's no order number, ask for it.
    Reply in 1-2 sentences and sign as "Asha, Support".
    """;

// Build 2's cases, now with the full expected answer (Build 3: Equivalence compares meaning, not words)
List<TestCase> tests =
[
    new("refund 4411",   "When will my refund for order 4411 arrive?", "The refund of Rs 10,989 was issued on 2 Oct and should arrive by 9 Oct."),
    new("active 4412",   "Is my order 4412 still active?",              "Yes. Order 4412, the Starter plan at Rs 499 a month paid by UPI on 1 Oct, is active."),
    new("unknown order", "Where is order 9999?",                        "We couldn't find an order 9999. Please check the order number."),
];
IEvaluator[] evaluators = [new EquivalenceEvaluator(), new GroundednessEvaluator()];
var judgeConfig = new ChatConfiguration(chat);
// ▼ EDIT 1: keep every result

var timer = Stopwatch.StartNew();
foreach (var test in tests)
{
    // ▼ EDIT 1: one scenario per test
    AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: [AIFunctionFactory.Create(GetOrder, "GetOrder")]);   // ▼ EDIT 3
    var response = await agent.RunAsync(test.Question);

    // Session 11's evidence: what GetOrder REALLY returned is what the answer must be grounded in
    var facts = string.Join("\n", response.Messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Select(r => $"{r.Result}"));
    List<EvaluationContext> context = [new EquivalenceEvaluatorContext(test.Expected), new GroundednessEvaluatorContext(facts.Length > 0 ? facts : "(no tool was called)")];

    var result = await new CompositeEvaluator(evaluators).EvaluateAsync(test.Question, response.Text, judgeConfig, context);   // ▼ EDIT 1
    Console.WriteLine($"{test.Name,-14} " + string.Join("   ", result.Metrics.Values.OrderBy(m => m.Name)
        .Select(m => $"{m.Name} {(m as NumericMetric)?.Value}/5 {(m.Interpretation?.Failed == false ? "pass" : "FAIL")}")));
}
Console.WriteLine($"\n{tests.Count} tests in {timer.Elapsed.TotalSeconds:N1} s");
// ▼ EDIT 2: a report people can read

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

record TestCase(string Name, string Question, string Expected);
