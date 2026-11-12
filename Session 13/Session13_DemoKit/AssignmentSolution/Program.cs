// ─────────────────────────────────────────────────────────────────────────────
// Session 13 assignment SOLUTION · Evaluating Whether Your AI App Actually Works
//
// TODO 1–4 done: 8 cases for the RAG assistant (/rag): what it should answer, what it can't, and near misses.
// Each is checked three ways: the answer (code), retrieval (code: the right note came back) and generation (one LLM
// judge, Groundedness, only when notes came back). Plus stretch S1: 2 agent cases from Session 12's APPROVALS.md
// (/agent/reply: the tools it called, what it must not say). 10 cases → EVALS.md → exit code 0 or 1.
//
//   Terminal 1: MyAiApi → dotnet run      Terminal 2: here → dotnet run      then open EVALS.md and do TODO 5
// ─────────────────────────────────────────────────────────────────────────────
using System.ClientModel;
using System.Net.Http.Json;
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
var judgeConfig = new ChatConfiguration(chat);

// Your MyAiApi, from the outside: the same HTTP calls a customer's app would make
var api = new HttpClient { BaseAddress = new Uri("http://localhost:5080"), Timeout = TimeSpan.FromMinutes(2) };

// TODO 1 · 8 RAG cases on the 3 notes MyAiApi seeds: 6 it can answer (2 per note, in different words), 2 it can't
List<RagCase> cases =
[
    new("rate limit",       "How many requests per minute can I send to our API?",      MustSay: "100",          NoteId: 1),
    new("too many",         "What does our API return if I send too many requests?",    MustSay: "429",          NoteId: 1),
    new("deploy days",      "Which days do we deploy to production?",                   MustSay: "Thursday",     NoteId: 2),
    new("deploy time",      "What time do production deployments happen?",              MustSay: "6 pm",         NoteId: 2),
    new("site down",        "The site is down. What do I do first?",                    MustSay: "PagerDuty",    NoteId: 3),
    new("after paging",     "Where do I post after paging the on-call engineer?",       MustSay: "#incidents",   NoteId: 3),
    new("not in notes",     "What is our parental leave policy?",                       MustSay: "I don't know", NoteId: null),
    new("not in notes 2",   "What is the office Wi-Fi password?",                       MustSay: "I don't know", NoteId: null),
];
// STRETCH S1 · 2 agent cases from Session 12's APPROVALS.md: A1 (approved) and A2 (rejected: it promised Friday)
List<AgentCase> agentCases =
[
    new("A1 refund status", "I cancelled my plan last week and still no refund!! priya@example.com", MustCall: "FindOrders(priya@example.com)", MustNotSay: "business days"),
    new("A2 pushy Friday",  "Order 4413: refund the SMS pack too and confirm it'll be back in my account by Friday. ravi@example.com", MustCall: "GetOrder(4413)", MustNotSay: "by Friday"),
];

List<string> rows = ["| Test | Result | Problems | Right? (TODO 5) |", "| --- | --- | --- | --- |"];
var passed = 0;
foreach (var test in cases)
{
    var rag = await (await api.PostAsJsonAsync("/rag", new { question = test.Question })).Content.ReadFromJsonAsync<RagResponse>();
    List<string> problems = [];
    if (!rag!.Answer.Contains(test.MustSay, StringComparison.OrdinalIgnoreCase)) problems.Add($"doesn't say \"{test.MustSay}\"");

    // TODO 2 · retrieval: the right note came back (or none, for a question our notes can't answer)
    var got = rag.Sources.Select(s => s.Id).ToList();
    if (test.NoteId is { } id && !got.Contains(id)) problems.Add($"retrieval: note [{id}] not found (got [{string.Join(",", got)}])");
    if (test.NoteId is null && got.Count > 0) problems.Add($"retrieval: got [{string.Join(",", got)}] for a question our notes can't answer");

    // TODO 3 · generation: grounded in the notes it was given (a judge, only when there are notes)
    if (rag.Sources.Count > 0)
    {
        var notes = string.Join("\n", rag.Sources.Select(s => $"[{s.Id}] {s.Text}"));
        var result = await new GroundednessEvaluator().EvaluateAsync(test.Question, rag.Answer, judgeConfig, [new GroundednessEvaluatorContext(notes)]);
        var grounded = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        if (grounded.Interpretation?.Failed != false) problems.Add($"groundedness {grounded.Value}/5: {grounded.Reason}");
    }

    if (problems.Count == 0) passed++;
    Console.WriteLine($"{(problems.Count == 0 ? "PASS" : "FAIL"),-5} {test.Name,-16} {string.Join("; ", problems)}\n      {rag.Answer.Split('\n')[0]}");
    rows.Add($"| {test.Name} | {(problems.Count == 0 ? "PASS" : "FAIL")} | {string.Join("; ", problems)} | |");
}

// STRETCH S1 · the agent: the tools it called (its Steps) and what it must never promise
foreach (var test in agentCases)
{
    var reply = await (await api.PostAsJsonAsync("/agent/reply", new { ticket = test.Ticket })).Content.ReadFromJsonAsync<AgentReply>();
    List<string> problems = [];
    if (!reply!.Steps.Contains(test.MustCall)) problems.Add($"didn't call {test.MustCall} (called: {string.Join(", ", reply.Steps)})");
    if (reply.Draft.Contains(test.MustNotSay, StringComparison.OrdinalIgnoreCase)) problems.Add($"says \"{test.MustNotSay}\"");

    if (problems.Count == 0) passed++;
    Console.WriteLine($"{(problems.Count == 0 ? "PASS" : "FAIL"),-5} {test.Name,-16} {string.Join("; ", problems)}\n      {reply.Draft.Split('\n')[0]}");
    rows.Add($"| {test.Name} | {(problems.Count == 0 ? "PASS" : "FAIL")} | {string.Join("; ", problems)} | |");
}

var total = cases.Count + agentCases.Count;
Console.WriteLine($"\n{passed}/{total} passed");

// TODO 4 · EVALS.md + a gate: exit code 1 fails a CI build
File.WriteAllText("EVALS.md", $"# Evals · {DateTime.Now:yyyy-MM-dd HH:mm} · {passed}/{total} passed\n\n" + string.Join("\n", rows) + """


    ## Reflections (TODO 5)

    1. Which verdicts did you disagree with, and why?
    2. Which failures were retrieval, which generation?
    3. What would you add before trusting this in production?

    """);
Console.WriteLine($"wrote {Path.GetFullPath("EVALS.md")}");
return passed == total ? 0 : 1;

record RagCase(string Name, string Question, string MustSay, int? NoteId);
record RagResponse(string Answer, string Provider, string Model, List<Source> Sources);   // what MyAiApi's /rag returns (Session 6)
record Source(int Id, string Topic, string Text, double Score);
record AgentCase(string Name, string Ticket, string MustCall, string MustNotSay);
record AgentReply(string Draft, string Provider, string Model, List<string> Steps);   // what MyAiApi's /agent/reply returns (Session 10)
