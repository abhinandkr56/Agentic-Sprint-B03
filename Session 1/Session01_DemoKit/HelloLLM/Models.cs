using System.Text.Json;
using System.Text.Json.Serialization;

namespace HelloLLM;

// ── The OpenAI-compatible "chat completions" wire format, as plain C# records ──
// Groq, OpenAI, xAI, GitHub Models, Ollama… all accept (roughly) this shape.

public record ChatMessage(string Role, string Content)
{
    public static ChatMessage System(string text) => new("system", text);
    public static ChatMessage User(string text) => new("user", text);
    public static ChatMessage Assistant(string text) => new("assistant", text);
}

public record ChatRequest(string Model, IReadOnlyList<ChatMessage> Messages)
{
    public double? Temperature { get; init; }   // 0 = focused, higher = more random
    public int? MaxTokens { get; init; }         // hard cap on the *output* length
    public bool? Stream { get; init; }           // true = Server-Sent Events
}

public record ChatResponse(string Id, string Model, List<Choice> Choices, Usage? Usage);

public record Choice(int Index, ChatMessage Message, string? FinishReason);

public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens, double? TotalTime);

// Streaming chunks look like: data: {"choices":[{"delta":{"content":"Hel"}}]}
public record StreamChunk(List<StreamChoice>? Choices);
public record StreamChoice(StreamDelta? Delta, string? FinishReason);
public record StreamDelta(string? Content);

public static class Json
{
    /// snake_case ↔ PascalCase mapping: MaxTokens ↔ max_tokens, FinishReason ↔ finish_reason.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static string Prettify(string json)
    {
        try { return JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement, Pretty); }
        catch { return json; }
    }
}
