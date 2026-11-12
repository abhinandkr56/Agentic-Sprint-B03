// Build 3 · Unplugged (FINAL: where you end up after the edits)
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient cloud = Client("https://api.groq.com/openai/v1", Environment.GetEnvironmentVariable("GROQ_API_KEY")!, "llama-3.3-70b-versatile");
IChatClient local = Client("http://localhost:11434/v1", "ollama", "llama3.2:1b");

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    // EDIT 2: cloud first; if it fails for any reason, answer on this laptop.
    // (Offline, the SDK retries 4 times, then throws an AggregateException, so catch Exception, not just ClientResultException.)
    ChatResponse response;
    try
    {
        response = await cloud.GetResponseAsync(question);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"☁ cloud failed ({ex.GetType().Name}) → answering on this laptop");
        response = await local.GetResponseAsync(question);
    }
    Console.WriteLine($"[{response.ModelId}] {response.Text}");
}

// EDIT 1 is not code. Go "offline" in THIS terminal only (Zoom keeps working), then run again:
//     PowerShell:  $env:HTTPS_PROXY = "http://127.0.0.1:9";  dotnet run --no-restore
//     bash / zsh:  export HTTPS_PROXY=http://127.0.0.1:9;    dotnet run --no-restore
// EDIT 3 is not code either. Back online:
//     PowerShell:  Remove-Item Env:HTTPS_PROXY               bash / zsh:  unset HTTPS_PROXY

// Build 1's three lines, with the details passed in
IChatClient Client(string endpoint, string key, string model) =>
    new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
        .GetChatClient(model)
        .AsIChatClient();
