// Build 5 · RagEvals (FINAL): your Session 6 RAG assistant, tested from the OUTSIDE, over HTTP.
//   Terminal 1: MyAiApi/ → dotnet run (port 5080)      Terminal 2: here → dotnet run (GROQ_API_KEY: the judge)
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

// Questions about the 3 notes MyAiApi seeds (Session 5). Your own notes? Change the questions and the NoteIds.
List<RagCase> cases =   
[
    new("rate limit",   "How many requests per minute can I send to our API?", MustSay: "100",          NoteId: 1),
    new("deploy days",  "Which days do we deploy to production?",              MustSay: "Thursday",     NoteId: 2),
    new("site down",    "The site is down. What do I do first?",               MustSay: "PagerDuty",    NoteId: 3),
    new("not in notes", "What is our parental leave policy?",                  MustSay: "I don't know", NoteId: null),   // from chat
];
List<string> rows = ["| Test | Result | Problems |", "| --- | --- | --- |"];   // EDIT 3: EVALS.md, the assignment's table

var passed = 0;
foreach (var test in cases)
{
    var rag = await (await api.PostAsJsonAsync("/rag", new { question = test.Question })).Content.ReadFromJsonAsync<RagResponse>();
    List<string> problems = [];
    if (!rag!.Answer.Contains(test.MustSay, StringComparison.OrdinalIgnoreCase)) problems.Add($"doesn't say \"{test.MustSay}\"");
    // EDIT 1: RAG fails in two halves (Session 6). Half 1, retrieval: did the right note come back at all?
    var got = rag.Sources.Select(s => s.Id).ToList();
    if (test.NoteId is { } id && !got.Contains(id)) problems.Add($"retrieval: note [{id}] not found (got [{string.Join(",", got)}])");
    if (test.NoteId is null && got.Count > 0) problems.Add($"retrieval: got [{string.Join(",", got)}] for a question our notes can't answer");
    // EDIT 2: half 2, generation: is the answer backed by the notes it was given? A judge only where we need one.
    if (rag.Sources.Count > 0)
    {
        var notes = string.Join("\n", rag.Sources.Select(s => $"[{s.Id}] {s.Text}"));
        var result = await new GroundednessEvaluator().EvaluateAsync(test.Question, rag.Answer, judgeConfig, [new GroundednessEvaluatorContext(notes)]);
        var grounded = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        if (grounded.Interpretation?.Failed != false) problems.Add($"groundedness {grounded.Value}/5: {grounded.Reason}");
    }

    if (problems.Count == 0) passed++;
    Console.WriteLine($"{(problems.Count == 0 ? "PASS" : "FAIL"),-5} {test.Name,-13} {string.Join("; ", problems)}\n      {rag.Answer.Split('\n')[0]}");
    rows.Add($"| {test.Name} | {(problems.Count == 0 ? "PASS" : "FAIL")} | {string.Join("; ", problems)} |");   // EDIT 3
}
Console.WriteLine($"\n{passed}/{cases.Count} passed");
// EDIT 3: write the table, and fail the build if anything failed. That's how evals run in CI.
File.WriteAllText("EVALS.md", $"# Evals · {DateTime.Now:yyyy-MM-dd HH:mm} · {passed}/{cases.Count} passed\n\n" + string.Join("\n", rows) + "\n");
return passed == cases.Count ? 0 : 1;

record RagCase(string Name, string Question, string MustSay, int? NoteId);
record RagResponse(string Answer, string Provider, string Model, List<Source> Sources);   // what MyAiApi's /rag returns (Session 6)
record Source(int Id, string Topic, string Text, double Score);
