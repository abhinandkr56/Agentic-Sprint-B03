namespace HelloLLM;

/// <summary>
/// All settings come from environment variables so no key is ever committed to git.
///   GROQ_API_KEY   (required)  – from https://console.groq.com/keys
///   LLM_MODEL      (optional)  – default llama-3.3-70b-versatile
///   LLM_BASE_URL   (optional)  – default https://api.groq.com/openai/v1
/// Because Groq speaks the OpenAI wire format, pointing LLM_BASE_URL at
/// OpenAI, xAI (Grok), GitHub Models or Ollama works with the same code.
/// </summary>
public sealed record LlmConfig(string ApiKey, string Model, string BaseUrl)
{
    public const string DefaultBaseUrl = "https://api.groq.com/openai/v1";
    public const string DefaultModel = "openai/gpt-oss-120b";

    public static LlmConfig? FromEnvironment()
    {
        var key = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                  ?? Environment.GetEnvironmentVariable("LLM_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
        {
            Ui.Error("GROQ_API_KEY is not set.");
            Console.WriteLine("""

              1. Create a free key at https://console.groq.com/keys
              2. Set it in your terminal:
                   PowerShell :  $env:GROQ_API_KEY = "gsk_..."
                   bash/zsh   :  export GROQ_API_KEY="gsk_..."
              3. dotnet run
            """);
            return null;
        }

        var model = Environment.GetEnvironmentVariable("LLM_MODEL");
        var baseUrl = Environment.GetEnvironmentVariable("LLM_BASE_URL");
        return new LlmConfig(
            key.Trim(),
            string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim(),
            string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim());
    }
}
