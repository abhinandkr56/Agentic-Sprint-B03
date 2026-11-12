// Build 1 · McpTools (START): Session 8's Learn client meets Session 7's tool loop.
// In Session 8, WE picked the MCP tool and its arguments. Tonight the MODEL picks.
//   dotnet run      (GROQ_API_KEY + internet access to learn.microsoft.com)
using System.ClientModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

// Session 7's chat client, with the tool loop on. (Groq retired llama-3.3-70b-versatile: gpt-oss-120b from tonight.)
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

// Session 8, Build 4: an MCP client for Microsoft Learn (remote, Streamable HTTP, no key)
await using var learn = await McpClient.CreateAsync(new HttpClientTransport(new() { Name = "learn", Endpoint = new Uri("https://learn.microsoft.com/api/mcp") }));
Console.WriteLine($"Connected to {learn.ServerInfo.Name}");

var options = new ChatOptions();   // ▼ EDIT 1: hand the model Learn's tools
// ▼ EDIT 2: give it only the tool it needs
Console.WriteLine($"The model can see {options.Tools?.Count ?? 0} tool(s): {string.Join(", ", options.Tools?.Select(t => t.Name) ?? [])}");

// ▼ EDIT 3: the rules: answer from the docs, cite the page

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    List<ChatMessage> messages = [new(ChatRole.User, question)];   // ▼ EDIT 3: rules first
    var response = await chat.GetResponseAsync(messages, options);

    // Every tool call the model made, across all round trips (Session 7, Build 4)
    foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
        Console.WriteLine($"  [mcp] {call.Name}({string.Join(", ", call.Arguments?.Values ?? [])})");
    Console.WriteLine($"\n{response.Text}\n  [{response.Usage?.InputTokenCount:N0} tokens in, all round trips]");
}
