// Build 5 · EvalTraces (FINAL): Session 13's evals, code checks only. They tell you IF a case failed. Not WHERE.
//   Terminal 1: 4-TracedApi/Final (MyAiApi, traced) on :5080 · Terminal 2: here → dotnet run
using System.Diagnostics;
using System.Net.Http.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// EDIT 1: the evals send traces too, to the same dashboard
using var tracing = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Evals"))
    .AddSource("Evals")
    .AddHttpClientInstrumentation()   // EDIT 1: the hop itself: a client span for each call to MyAiApi
    .AddOtlpExporter()
    .Build();
var evals = new ActivitySource("Evals");

var api = new HttpClient { BaseAddress = new Uri("http://localhost:5080"), Timeout = TimeSpan.FromMinutes(2) };

// Session 13's cases, on the 3 notes MyAiApi seeds
List<RagCase> cases =
[
    new("rate limit",   "How many requests per minute can I send to our API?", MustSay: "100",          NoteId: 1),
    new("deploy days",  "Which days do we deploy to production?",              MustSay: "Thursday",     NoteId: 2),
    new("site down",    "The site is down. What do I do first?",               MustSay: "PagerDuty",    NoteId: 3, MustNotSay: "minutes"),
    new("not in notes", "What is our parental leave policy?",                  MustSay: "I don't know", NoteId: null),   // from chat
];

var passed = 0;
foreach (var test in cases)
{
    using var span = evals.StartActivity($"eval {test.Name}");   // EDIT 1: HttpClient sends this span's id to MyAiApi
    var rag = await (await api.PostAsJsonAsync("/rag", new { question = test.Question })).Content.ReadFromJsonAsync<RagResponse>();
    List<string> problems = [];
    if (!rag!.Answer.Contains(test.MustSay, StringComparison.OrdinalIgnoreCase)) problems.Add($"doesn't say \"{test.MustSay}\"");
    if (test.MustNotSay is not null && rag.Answer.Contains(test.MustNotSay, StringComparison.OrdinalIgnoreCase)) problems.Add($"says \"{test.MustNotSay}\"");
    if (test.NoteId is { } id && !rag.Sources.Any(s => s.Id == id)) problems.Add($"note [{id}] not retrieved");
    // EDIT 2: the verdict goes on the span: failing cases turn red in the dashboard
    span?.SetTag("eval.result", problems.Count == 0 ? "pass" : "fail");
    if (problems.Count > 0) span?.SetStatus(ActivityStatusCode.Error, string.Join("; ", problems));

    if (problems.Count == 0) passed++;
    Console.WriteLine($"{(problems.Count == 0 ? "PASS" : "FAIL"),-5} {test.Name,-13} {string.Join("; ", problems)}");
    if (problems.Count > 0) Console.WriteLine($"      trace: http://localhost:18888/traces/detail/{span?.TraceId}");   // EDIT 3: IF → WHERE
}
Console.WriteLine($"\n{passed}/{cases.Count} passed");

record RagCase(string Name, string Question, string MustSay, int? NoteId, string? MustNotSay = null);
record RagResponse(string Answer, string Provider, string Model, List<Source> Sources);   // what MyAiApi's /rag returns (Session 6)
record Source(int Id, string Topic, string Text, double Score);
