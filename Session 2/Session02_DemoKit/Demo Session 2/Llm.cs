using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PromptLab;

/// <summary>
/// The same raw-HttpClient LLM client idea as Session 1's HelloLLM, grown a little:
/// it now accepts a response_format (JSON mode / structured outputs).
/// Config comes from environment variables, never from code:
///   GROQ_API_KEY   (required)
///   LLM_BASE_URL   (default https://api.groq.com/openai/v1)
///   LLM_MODEL      (default llama-3.3-70b-versatile)   used by most demos
///   STRICT_MODEL   (default openai/gpt-oss-20b)         used by Demo 7 (json_schema, strict)
/// </summary>
public static class Llm
{
    public static readonly string BaseUrl =
        (Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "https://api.groq.com/openai/v1").TrimEnd('/');
    public static readonly string Model =
        Environment.GetEnvironmentVariable("LLM_MODEL") ?? "openai/gpt-oss-20b";
    public static readonly string StrictModel =
        Environment.GetEnvironmentVariable("STRICT_MODEL") ?? "openai/gpt-oss-20b";

    static string? ApiKey =>
        Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? Environment.GetEnvironmentVariable("LLM_API_KEY");

    public static bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
        if (HasKey)
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey!.Trim());
        return http;
    }

    /// <summary>One chat call. Returns the answer plus everything worth logging.</summary>
    public static async Task<ChatResult> ChatAsync(
        IEnumerable<Msg> messages,
        double temperature = 0.7,
        int maxTokens = 800,
        JsonObject? responseFormat = null,
        string? model = null,
        JsonObject? extra = null)
    {
        var body = new JsonObject
        {
            ["model"] = model ?? Model,
            ["messages"] = new JsonArray(messages
                .Select(m => (JsonNode)new JsonObject { ["role"] = m.Role, ["content"] = m.Content })
                .ToArray()),
            ["temperature"] = temperature,
            ["max_tokens"] = maxTokens,
        };
        if (responseFormat is not null) body["response_format"] = responseFormat.DeepClone();
        if (extra is not null)
            foreach (var kv in extra) body[kv.Key] = kv.Value?.DeepClone();

        var requestJson = body.ToJsonString();
        var sw = Stopwatch.StartNew();
        using var res = await Http.PostAsync($"{BaseUrl}/chat/completions",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));
        var raw = await res.Content.ReadAsStringAsync();
        sw.Stop();

        if (!res.IsSuccessStatusCode)
            throw new LlmException((int)res.StatusCode, raw);

        var parsed = JsonSerializer.Deserialize<ChatResponse>(raw, Json.Wire)
                     ?? throw new LlmException(0, "Empty response body");
        var choice = parsed.Choices[0];
        return new ChatResult(
            Content: choice.Message.Content ?? "",
            FinishReason: choice.FinishReason ?? "?",
            PromptTokens: parsed.Usage?.PromptTokens ?? 0,
            CompletionTokens: parsed.Usage?.CompletionTokens ?? 0,
            Elapsed: sw.Elapsed,
            RawRequest: requestJson,
            RawResponse: raw);
    }

    /// <summary>Convenience overload: one system prompt + one user message.</summary>
    public static Task<ChatResult> AskAsync(string? system, string user, double temperature = 0.7,
        int maxTokens = 800, JsonObject? responseFormat = null, string? model = null, JsonObject? extra = null)
    {
        var msgs = new List<Msg>();
        if (!string.IsNullOrWhiteSpace(system)) msgs.Add(Msg.System(system));
        msgs.Add(Msg.User(user));
        return ChatAsync(msgs, temperature, maxTokens, responseFormat, model, extra);
    }

    // ── response_format helpers ─────────────────────────────────────────────
    /// <summary>JSON mode: guarantees syntactically valid JSON. Says nothing about the shape.</summary>
    public static JsonObject JsonMode() => new() { ["type"] = "json_object" };

    /// <summary>Structured outputs: the provider constrains decoding to this JSON Schema.</summary>
    public static JsonObject JsonSchema(string name, string schemaJson, bool strict = true) => new()
    {
        ["type"] = "json_schema",
        ["json_schema"] = new JsonObject
        {
            ["name"] = name,
            ["strict"] = strict,
            ["schema"] = JsonNode.Parse(schemaJson),
        },
    };
}

public record Msg(string Role, string Content)
{
    public static Msg System(string c) => new("system", c);
    public static Msg User(string c) => new("user", c);
    public static Msg Assistant(string c) => new("assistant", c);
}

public record ChatResult(
    string Content, string FinishReason, int PromptTokens, int CompletionTokens,
    TimeSpan Elapsed, string RawRequest, string RawResponse)
{
    public string Stats => $"{PromptTokens} in · {CompletionTokens} out · {Elapsed.TotalSeconds:0.00}s · finish: {FinishReason}";
}

public class LlmException(int status, string body) : Exception($"HTTP {status}: {body}")
{
    public int Status { get; } = status;
    public string Body { get; } = body;
}
