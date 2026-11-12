// RagEvals: arrange (save the notes), act (ask), assert (check the answer). Exit code 1 if anything fails.
using System.Net.Http.Json;

var baseUrl = Environment.GetEnvironmentVariable("RAGAPI_HTTP") ?? "http://localhost:5080";   // the AppHost sets RAGAPI_HTTP
var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(2) };
Console.WriteLine($"Evals against {baseUrl}\n");

string[] notes =
[
    "Our public API allows 100 requests per minute per key.",
    "We deploy to production every Thursday at 14:00 IST.",
    "If the site is down, page the on-call engineer before anything else.",
];
for (var i = 0; i < notes.Length; i++)   // ids 1–3: posting again updates the same rows (upsert)
    (await http.PostAsJsonAsync("/notes", new { id = i + 1, text = notes[i] })).EnsureSuccessStatusCode();

(string Question, string MustContain)[] cases =
[
    ("How many requests per minute can I send to our API?", "100"),
    ("Which day do we deploy to production?", "Thursday"),
    ("The site is down. What do I do first?", "on-call"),
    ("What is our CEO's favourite colour?", "don't know"),
];

var passed = 0;
foreach (var (question, mustContain) in cases)
{
    var reply = await http.GetFromJsonAsync<Reply>($"/ask?q={Uri.EscapeDataString(question)}");
    var answer = reply!.Answer.Replace('\u2019', '\'');   // a curly apostrophe still counts
    var ok = answer.Contains(mustContain, StringComparison.OrdinalIgnoreCase);
    if (ok) passed++;
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {question}\n      {reply.Model}: {answer}\n");
}

Console.WriteLine($"{passed}/{cases.Length} passed");
return passed == cases.Length ? 0 : 1;

record Reply(string Answer, string[] Notes, string Model);
