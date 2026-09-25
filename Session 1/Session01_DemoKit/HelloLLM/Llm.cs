using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HelloLLM;

/// <summary>
/// A deliberately tiny wrapper around HttpClient. ~50 lines is all it takes to talk to an LLM.
/// (In Session 3 we replace this with Microsoft.Extensions.AI's IChatClient.)
/// </summary>
public sealed class Llm(HttpClient http, LlmConfig config)
{
    public string Model => config.Model;

    public async Task<(ChatResponse Response, TimeSpan Elapsed)> ChatAsync(ChatRequest request)
    {
        var sw = Stopwatch.StartNew();
        using var res = await http.PostAsync("chat/completions", Body(request));
        await EnsureOk(res);
        var body = await res.Content.ReadFromJsonAsync<ChatResponse>(Json.Options)
                   ?? throw new InvalidOperationException("Empty response body");
        return (body, sw.Elapsed);
    }

    /// Convenience: one user message → answer text.
    public async Task<ChatResponse> AskAsync(string prompt, double? temperature = null, int? maxTokens = null, string? system = null)
    {
        var messages = new List<ChatMessage>();
        if (system is not null) messages.Add(ChatMessage.System(system));
        messages.Add(ChatMessage.User(prompt));
        var (r, _) = await ChatAsync(new ChatRequest(config.Model, messages) { Temperature = temperature, MaxTokens = maxTokens });
        return r;
    }

    /// Streams the answer token-by-token. Yields each text fragment as it arrives.
    public async IAsyncEnumerable<string> StreamAsync(IReadOnlyList<ChatMessage> messages, double? temperature = null)
    {
        var request = new ChatRequest(config.Model, messages) { Stream = true, Temperature = temperature };
        using var msg = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = Body(request)
        };
        // ResponseHeadersRead = don't wait for the whole body; start reading immediately.
        using var res = await http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead);
        await EnsureOk(res);

        using var stream = await res.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (!line.StartsWith("data:")) continue;          // skip blank keep-alive lines
            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") yield break;                 // end-of-stream marker
            var chunk = JsonSerializer.Deserialize<StreamChunk>(data, Json.Options);
            var text = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(text)) yield return text;
        }
    }

    /// Serialize the record to JSON (snake_case) with a proper Content-Length.
    public static StringContent Body(ChatRequest request) =>
        new(JsonSerializer.Serialize(request, Json.Options), Encoding.UTF8, "application/json");

    public static async Task EnsureOk(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;
        var body = await res.Content.ReadAsStringAsync();
        throw new LlmApiException((int)res.StatusCode, body);
    }
}

public sealed class LlmApiException(int status, string body)
    : Exception($"HTTP {status}: {body}")
{
    public int Status { get; } = status;
    public string Body { get; } = body;
}
