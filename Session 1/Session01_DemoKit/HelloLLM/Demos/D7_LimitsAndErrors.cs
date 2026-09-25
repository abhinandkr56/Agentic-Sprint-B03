using System.Net.Http.Headers;

namespace HelloLLM.Demos;

/// DEMO 7 — Things WILL go wrong. Know what it looks like before production does.
public static class D7_LimitsAndErrors
{
    public static async Task Run(Llm llm, LlmConfig config)
    {
        // A) Output cut off by max_tokens
        Ui.Step("A) max_tokens = 12 on a long question");
        var r = await llm.AskAsync("Explain dependency injection in ASP.NET Core in detail.", maxTokens: 12);
        Console.WriteLine($"  \"{r.Choices[0].Message.Content.Trim()}…\"");
        Ui.Write(ConsoleColor.Red, $"  finish_reason = {r.Choices[0].FinishReason}\n");
        Ui.Dim("  → Always check finish_reason. 'length' means the answer is truncated.");

        // B) Wrong model name
        Ui.Step("B) A model that doesn't exist");
        try
        {
            var bad = new ChatRequest("gpt-9-ultra-imaginary", [ChatMessage.User("hi")]);
            await llm.ChatAsync(bad);
        }
        catch (LlmApiException ex) { ShowError(ex); }

        // C) Bad API key (separate client so we don't break the shared one)
        Ui.Step("C) An invalid API key");
        using var badClient = new HttpClient { BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/") };
        badClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "gsk_this_is_not_a_real_key");
        using var res = await badClient.PostAsync("chat/completions",
            Llm.Body(new ChatRequest(config.Model, [ChatMessage.User("hi")])));
        try { await Llm.EnsureOk(res); }
        catch (LlmApiException ex) { ShowError(ex); }

        Ui.Good("\nCommon ones: 400 bad request · 401 bad key · 404 unknown model · 413 too big · 429 rate limit · 5xx provider down");
        Ui.Good("Production answer: retries with backoff (Polly / Microsoft.Extensions.Http.Resilience), timeouts, logging.");
    }

    private static void ShowError(LlmApiException ex)
    {
        Ui.Write(ConsoleColor.Red, $"  HTTP {ex.Status}\n");
        Ui.Dim("  " + Json.Prettify(ex.Body).ReplaceLineEndings("\n  "));
    }
}
