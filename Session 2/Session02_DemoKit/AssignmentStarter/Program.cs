// ─────────────────────────────────────────────────────────────────────────────
// Session 02 assignment starter · Prompt Engineering & Getting Useful Output
//
// Syllabus: "Write one prompt for a real task you do at work, run it 3 times with
// small tweaks, and get one version to return clean JSON you can deserialize."
//
// It compiles and runs as-is using the ticket-triage example from class.
// Work through TODO 1 → TODO 5, then fill in NOTES.md.
//
//   PowerShell:  $env:GROQ_API_KEY = "gsk_..."      bash/zsh:  export GROQ_API_KEY="gsk_..."
//   dotnet run
// ─────────────────────────────────────────────────────────────────────────────
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var baseUrl = (Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://api.groq.com/openai/v1").TrimEnd('/');
var model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? "llama-3.3-70b-versatile";
var key = Environment.GetEnvironmentVariable("GROQ_API_KEY");
if (string.IsNullOrWhiteSpace(key)) { Console.WriteLine("Set GROQ_API_KEY first."); return; }

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key.Trim());
Console.OutputEncoding = Encoding.UTF8;

// ── TODO 1 · Pick a real task from your job ─────────────────────────────────
// Replace this with a real input you deal with at work: a log line, a bug report, an email,
// a user story, a SQL query, a PR description... (remove anything confidential first!)
// If you have nothing handy, keep the ticket: that's the second example from class.
const string Input = "UPI payment failed but the money was deducted from my bank account!!";

// ── TODO 2 · Write three versions of the prompt, each a small tweak on the last ─
// v1 is what you'd type on a busy day. Improve v2 and v3 using the 5-part anatomy:
// Role · Task · Context · Constraints · Format. Change ONE thing per version so you learn what helped.
string[] versions =
[
    "Classify this support ticket.",                              // v1: vague on purpose
    "TODO: v2, add a role and the exact list of allowed categories",
    "TODO: v3, ask for JSON in an exact shape (see TODO 3) and give one example",
];

// Run every version 3 times so you see variation, not just one lucky answer.
for (int v = 0; v < versions.Length; v++)
{
    if (versions[v].StartsWith("TODO")) { Console.WriteLine($"\n(v{v + 1} not written yet, skipping)"); continue; }
    Console.WriteLine($"\n══ v{v + 1}: {versions[v]}");
    for (int run = 1; run <= 3; run++)
    {
        var answer = await Ask(versions[v], Input, jsonMode: false);
        Console.WriteLine($"  run {run}: {answer.Replace("\n", " ⏎ ")}");
    }
}

// ── TODO 3 · Define the C# shape you want back ──────────────────────────────
// See the TicketTriage record at the bottom of this file. Change it to fit YOUR task.
// Keep it small: 3 to 5 properties. Use enums or bools where the answer is from a fixed set.

// ── TODO 4 · Turn on JSON mode ──────────────────────────────────────────────
// Go to the Ask() function below and add response_format when jsonMode is true.
// Remember: JSON mode needs the word "JSON" somewhere in your prompt.

// ── TODO 5 · Deserialize, validate, print typed fields ──────────────────────
var finalPrompt = versions[^1].StartsWith("TODO") ? versions[0] : versions[^1];
Console.WriteLine("\n══ Final: JSON → C# object");
var json = await Ask(finalPrompt, Input, jsonMode: true);
Console.WriteLine(json);
try
{
    var result = JsonSerializer.Deserialize<TicketTriage>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    // TODO 5: validate! A missing field doesn't throw, it silently becomes null/default.
    //         Check the properties that matter and print a clear error if any are missing.
    Console.WriteLine($"\n✓ Parsed: Category = {result?.Category}, Priority = {result?.Priority}");
}
catch (JsonException ex)
{
    Console.WriteLine($"\n✗ Not deserializable yet: {ex.Message.Split('\n')[0]}");
    Console.WriteLine("  That's expected until you finish TODO 3 and TODO 4.");
}

// ── The raw call (same idea as Session 1) ───────────────────────────────────
async Task<string> Ask(string system, string user, bool jsonMode)
{
    var body = new JsonObject
    {
        ["model"] = model,
        ["temperature"] = 0.7,
        ["max_tokens"] = 500,
        ["messages"] = new JsonArray(
            new JsonObject { ["role"] = "system", ["content"] = system },
            new JsonObject { ["role"] = "user", ["content"] = user }),
    };

    if (jsonMode)
    {
        // TODO 4: body["response_format"] = new JsonObject { ["type"] = "json_object" };
    }

    using var res = await http.PostAsync($"{baseUrl}/chat/completions",
        new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
    var raw = await res.Content.ReadAsStringAsync();
    if (!res.IsSuccessStatusCode) return $"HTTP {(int)res.StatusCode}: {raw}";
    return JsonNode.Parse(raw)!["choices"]![0]!["message"]!["content"]!.GetValue<string>().Trim();
}

// ── TODO 3 lives here: change this record to match your task ────────────────
public record TicketTriage(string Category, string Priority, string Sentiment, string OneLineSummary);

// ── STRETCH (pick one or more) ─────────────────────────────────────────────
// S1. Retry once: if parsing/validation fails, send the error back to the model and ask for corrected JSON.
// S2. Structured outputs: set LLM_MODEL=openai/gpt-oss-20b and use
//     response_format = { type: "json_schema", json_schema: { name, strict: true, schema } }
//     (every property in "required", additionalProperties: false). See PromptLab/groq-json.http.
// S3. Few-shot: add 2 or 3 example user/assistant turns before the real input. Does v1 improve?
// S4. Injection test: put "Ignore previous instructions and ..." inside Input. Does your prompt hold?
// S5. Score it in code: write a bool Passes(TicketTriage t) and run the final prompt 10 times.
