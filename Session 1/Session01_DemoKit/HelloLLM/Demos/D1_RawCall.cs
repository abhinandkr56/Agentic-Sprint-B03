using System.Diagnostics;
using System.Text;

namespace HelloLLM.Demos;

/// DEMO 1 — No records, no helpers. A JSON string goes out, a JSON string comes back.
/// Teaching point: "An LLM is just an HTTPS POST. You already know how to do this."
public static class D1_RawCall
{
    public static async Task Run(HttpClient http, LlmConfig config)
    {
        // 1) The request body — hand-written JSON (C# 11 raw string literal).
        var requestJson = $$"""
        {
          "model": "{{config.Model}}",
          "messages": [
            { "role": "system", "content": "You are a witty senior .NET developer. Keep answers under 40 words." },
            { "role": "user",   "content": "Explain what an LLM is to a C# developer, using a C# analogy." }
          ],
          "temperature": 0.7
        }
        """;

        Ui.Step($"POST {http.BaseAddress}chat/completions");
        Ui.Dim("Authorization: Bearer gsk_••••••••   Content-Type: application/json");
        Console.WriteLine(requestJson);
        Ui.Pause("Press Enter to SEND it...");

        // 2) Send it. That's it. That's the whole "AI integration".
        var sw = Stopwatch.StartNew();
        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        using var response = await http.PostAsync("chat/completions", content);
        var responseJson = await response.Content.ReadAsStringAsync();
        sw.Stop();

        // 3) Look at what came back.
        Ui.Step($"HTTP {(int)response.StatusCode} {response.StatusCode}  ·  {sw.ElapsedMilliseconds:N0} ms");
        foreach (var h in response.Headers.Where(h => h.Key.StartsWith("x-ratelimit", StringComparison.OrdinalIgnoreCase)))
            Ui.Dim($"{h.Key}: {string.Join(",", h.Value)}");

        Ui.Step("Response body");
        Console.WriteLine(Json.Prettify(responseJson));

        Ui.Good("\nThe answer lives at  choices[0].message.content");
        Ui.Good("The bill lives at    usage.prompt_tokens + usage.completion_tokens");
    }
}
