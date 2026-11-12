// ─────────────────────────────────────────────────────────────────────────────
// Session 13 assignment starter · Evaluating Whether Your AI App Actually Works
//
// Syllabus: "Write 8–10 test cases for your RAG assistant or agent, run them, and mark each pass or fail."
//
// A console app that tests YOUR MyAiApi from the outside, over HTTP. Build 5 (RagEvals) showed every step live.
//   Terminal 1: your MyAiApi → dotnet run   (docker start pgvector · ollama list: all-minilm)
//   Terminal 2: here → dotnet run           (GROQ_API_KEY: the judge)
// It runs as-is: 2 example cases, one check each. Work through TODO 0 → TODO 5.
// ─────────────────────────────────────────────────────────────────────────────
// ── TODO 0 · Packages: already in Evals.csproj ───────────────────────────────
//   Microsoft.Extensions.AI.Evaluation.Quality 10.11.0 (the judges) · Microsoft.Extensions.AI + .OpenAI 10.11.0 (the judge's client).
//   Your MyAiApi doesn't change at all: these evals call it like any client would.
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

// ── TODO 1 · Your test cases: 8–10 of them (Build 2) ─────────────────────────
//   Mix them: questions your notes answer (MustSay = the fact, NoteId = the note it's in) · questions they CAN'T
//   answer (MustSay "I don't know", NoteId null) · near misses: other words, a typo, another language.
//   Using your own Session 5 notes? Write the questions for YOUR notes and their ids.
List<RagCase> cases =
[
    new("rate limit",   "How many requests per minute can I send to our API?", MustSay: "100",          NoteId: 1),
    new("not in notes", "What is our parental leave policy?",                  MustSay: "I don't know", NoteId: null),
];

var passed = 0;
foreach (var test in cases)
{
    var rag = await (await api.PostAsJsonAsync("/rag", new { question = test.Question })).Content.ReadFromJsonAsync<RagResponse>();
    List<string> problems = [];
    if (!rag!.Answer.Contains(test.MustSay, StringComparison.OrdinalIgnoreCase)) problems.Add($"doesn't say \"{test.MustSay}\"");

    // ── TODO 2 · Retrieval: did the right note come back? (Build 5, EDIT 1) ──────
    //   rag.Sources holds the notes /rag found. NoteId set → it must be among them. NoteId null → there should be none.

    // ── TODO 3 · Generation: is the answer grounded in those notes? (Build 5, EDIT 2) ──
    //   Only when there are sources: new GroundednessEvaluator().EvaluateAsync(question, answer, judgeConfig,
    //   [new GroundednessEvaluatorContext(the sources' text)]) → .Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName)
    //   → Interpretation.Failed, Value, Reason.

    if (problems.Count == 0) passed++;
    Console.WriteLine($"{(problems.Count == 0 ? "PASS" : "FAIL"),-5} {test.Name,-13} {string.Join("; ", problems)}\n      {rag.Answer.Split('\n')[0]}");
}
Console.WriteLine($"\n{passed}/{cases.Count} passed");

// ── TODO 4 · EVALS.md + a gate (Build 5, EDIT 3) ─────────────────────────────
//   Write a table (test | result | problems) to EVALS.md, and `return passed == cases.Count ? 0 : 1;`

// ── TODO 5 · Open EVALS.md and mark every row yourself: do YOU agree with the verdict? ──
//   A false pass (it passed, but the answer is wrong) or a false fail (a good answer failed) is a bug in the TEST.
//   Fix the case, run again. Then answer the reflection questions at the bottom of EVALS.md.

// ── STRETCH (pick one or more) ──────────────────────────────────────────────────
// S1. Agent cases from your APPROVALS.md (Session 12): POST /agent/reply { ticket }. Check its Steps (the tools it
//     called, e.g. "GetOrder(4413)") and what it must NOT say ("by Friday"). Session 12's A2 reject = a test case.
// S2. A different judge: Gemini judges Groq's answers (Session 3's swap: same code, another endpoint and key).
// S3. Run each case 3 times (Build 1) and report a pass RATE ("2/3"), not a pass.
// S4. Keep the results: DiskBasedReportingConfiguration + HtmlReportWriter (Build 4), or the aieval tool.
// S5. A free judge: F1Evaluator (Microsoft.Extensions.AI.Evaluation.NLP, preview) next to Groundedness. Where do they disagree?
// S6. In CI: a GitHub Actions step that starts MyAiApi and runs `dotnet run --project Evals`: exit code 1 fails the build.

record RagCase(string Name, string Question, string MustSay, int? NoteId);
record RagResponse(string Answer, string Provider, string Model, List<Source> Sources);   // what MyAiApi's /rag returns (Session 6)
record Source(int Id, string Topic, string Text, double Score);
