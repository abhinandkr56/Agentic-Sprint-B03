// ─────────────────────────────────────────────────────────────
//  Session 01 · ASSIGNMENT STARTER
//  Goal: call an LLM API directly with HttpClient, print the response,
//        and write down what a request/response actually looks like.
//  Fill in the TODOs. No NuGet packages needed.
// ─────────────────────────────────────────────────────────────
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
             ?? throw new Exception("Set GROQ_API_KEY first (https://console.groq.com/keys)");

using var http = new HttpClient { BaseAddress = new Uri("https://api.groq.com/openai/v1/") };
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

// TODO 1: Write the JSON body. You need "model" and a "messages" array.
//         Try model "llama-3.3-70b-versatile". Add a "system" and a "user" message.
var requestJson = """
{
  "model": "TODO",
  "messages": [
    { "role": "user", "content": "TODO: ask something about your real work" }
  ]
}
""";

// TODO 2: POST it to "chat/completions" and read the body as a string.
using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
// var response = await http.PostAsync(...);
// var responseJson = await response.Content.ReadAsStringAsync();

// TODO 3: Print the status code and the raw JSON.

// TODO 4: Pull out ONLY the answer text using JsonDocument:
//   using var doc = JsonDocument.Parse(responseJson);
//   var answer = doc.RootElement.GetProperty("choices")[0]
//                   .GetProperty("message").GetProperty("content").GetString();

// TODO 5: Print usage.prompt_tokens, usage.completion_tokens and choices[0].finish_reason.

// STRETCH (pick one):
//   a) Change temperature to 0 and to 1.5 — run 3 times each. What changes?
//   b) Send a 2nd request asking "what did I just ask you?" — does it know? Why not?
//   c) Set "max_tokens": 10 — what is finish_reason now?
//   d) Set "stream": true and read the response line by line.

Console.WriteLine("Done! Now write your notes in NOTES.md");
