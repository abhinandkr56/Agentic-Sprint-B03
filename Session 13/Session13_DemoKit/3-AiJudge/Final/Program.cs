// Build 3 · AiJudge (FINAL): Build 1 failed a CORRECT answer ("October 9") and passed nothing it didn't spell exactly.
// A string check can't read. Can a model check the MEANING?
//   dotnet run      (GROQ_API_KEY)
using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using OpenAI;

// The JUDGE: Session 3's chat client. (Tonight the same Groq model that answers also judges: stretch S2 swaps it.)
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

// What Session 10's GetOrder returns for order 4411
const string Facts = "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.";
const string Question = "When will my refund for order 4411 arrive?";
const string Expected = "The refund of Rs 10,989 was issued on 2 Oct and should arrive by 9 Oct.";   // the expected answer, in full

// Two answers our app really gave in Build 1
string[] answers =
[
    "Your refund went out on October 2 and should land by October 9.",                       // right, different words
    "Your refund is on its way and will reach your account within 5-7 business days.",       // sounds right, invented
];
Console.WriteLine($"QUESTION: {Question}\nEXPECTED: {Expected}\nFACTS:    {Facts}");
// EDIT 1: an LLM judge from Microsoft.Extensions.AI.Evaluation. ChatConfiguration = which model does the judging.
// EDIT 2: three judges, three questions. Same meaning as expected? On topic? Backed by the FACTS?
IEvaluator judge = new CompositeEvaluator(new EquivalenceEvaluator(), new RelevanceEvaluator(), new GroundednessEvaluator());
var judgeConfig = new ChatConfiguration(chat);
List<EvaluationContext> context = [new EquivalenceEvaluatorContext(Expected), new GroundednessEvaluatorContext(Facts)];

foreach (var answer in answers)
{
    Console.WriteLine($"\n\"{answer}\"");
    Console.WriteLine($"  {"contains 9 Oct:",-17} {(answer.Contains("9 Oct") ? "PASS" : "FAIL")}");
    // EDIT 1: the judge reads both and scores 1-5. 4 or more passes.
    var result = await judge.EvaluateAsync(Question, answer, judgeConfig, context);
    foreach (var metric in result.Metrics.Values.OrderBy(m => m.Name))
    {
        Console.WriteLine($"  {metric.Name + ":",-17} {(metric as NumericMetric)?.Value}/5 {(metric.Interpretation?.Failed == false ? "PASS" : "FAIL")}");
        // EDIT 3: the judge explains itself, or says why it couldn't score: check the checker
        if (metric.Reason is not null) Console.WriteLine($"      why: {metric.Reason}");
        if (metric.Diagnostics?.FirstOrDefault(d => d.Severity == EvaluationDiagnosticSeverity.Error) is { } error) Console.WriteLine($"      error: {error.Message.Split('\n')[0]}");
    }
}
