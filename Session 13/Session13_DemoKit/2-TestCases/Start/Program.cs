// Build 2 · TestCases (START): one question isn't a test. A small set of questions, each with what a correct
// answer must say, run against the agent, marked PASS or FAIL.
//   dotnet run      (GROQ_API_KEY)
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
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
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: [AIFunctionFactory.Create(GetOrder, "GetOrder")]);

// The test cases: a question, the fact a correct answer must contain, the tool call it must make, a phrase it must never say
List<TestCase> tests =
[
    new("refund 4411",   "When will my refund for order 4411 arrive?", MustSay: "9 Oct",  MustCall: "GetOrder(4411)"),
    new("active 4412",   "Is my order 4412 still active?",              MustSay: "active", MustCall: "GetOrder(4412)"),
    new("unknown order", "Where is order 9999?",                        MustSay: "9999",   MustCall: "GetOrder(9999)", MustNotSay: "Rs"),
    new("no number",     "Where is my refund??",                        MustSay: "order",  MustNotSay: "Oct"),
    // ▼ EDIT 3: your test case, from chat
];

var passed = 0;
foreach (var test in tests)
{
    var response = await agent.RunAsync(test.Question);   // a new session per test: tests don't share memory
    List<string> problems = [];
    if (!response.Text.Contains(test.MustSay, StringComparison.OrdinalIgnoreCase)) problems.Add($"doesn't say \"{test.MustSay}\"");
    // ▼ EDIT 1: did it really look it up?
    // ▼ EDIT 2: what it must never say

    if (problems.Count == 0) passed++;
    Console.WriteLine($"{(problems.Count == 0 ? "PASS" : "FAIL"),-5} {test.Name,-14} {string.Join("; ", problems)}\n      {response.Text.Split('\n')[0]}");
}
Console.WriteLine($"\n{passed}/{tests.Count} passed");

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

record TestCase(string Name, string Question, string MustSay, string? MustCall = null, string? MustNotSay = null);
