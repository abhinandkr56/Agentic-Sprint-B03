// Build 3 · Unplugged (START: open this before the call)
// What happens to your app when the internet goes away?
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

    // ▼ EDIT 2: try the cloud first; if it fails, answer on this laptop
    var response = await cloud.GetResponseAsync(question);
    Console.WriteLine($"[{response.ModelId}] {response.Text}");
}

// ▼ EDIT 1 is not code. Go "offline" in THIS terminal only (Zoom keeps working), then run again:
//     PowerShell:  $env:HTTPS_PROXY = "http://127.0.0.1:9";  dotnet run --no-restore
//     bash / zsh:  export HTTPS_PROXY=http://127.0.0.1:9;    dotnet run --no-restore
// ▼ EDIT 3 is not code either. Back online:
//     PowerShell:  Remove-Item Env:HTTPS_PROXY               bash / zsh:  unset HTTPS_PROXY

// Build 1's three lines, with the details passed in
IChatClient Client(string endpoint, string key, string model) =>
    new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
        .GetChatClient(model)
        .AsIChatClient();
