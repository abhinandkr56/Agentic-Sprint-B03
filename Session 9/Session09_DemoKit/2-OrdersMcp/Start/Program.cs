// Build 2 · OrdersMcp (START): Session 7's refund question, but GetOrder now lives in Session 8's OrdersServer.
// Same tool, same description, same answer. The difference: it runs in ANOTHER PROCESS, and we talk MCP to it.
//   dotnet run      (GROQ_API_KEY; the first run also builds ../../OrdersServer, so give it ~10 seconds)
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

// Session 7's rules, for the order tool
const string Rules = """
    You are the help-centre assistant for our SaaS product.
    Use GetOrder for questions about a specific order. Answer ONLY from what it returns, in one or two sentences.
    If you can't answer from a tool, reply: "I don't know. Please contact support."
    """;

// Session 8's OrdersServer, started as a child process: stdin/stdout carry MCP, stderr carries its logs.
// (Nothing starts yet: McpClient.CreateAsync starts the process.)
var transport = new StdioClientTransport(new()
{
    Name = "orders",
    Command = "dotnet",
    Arguments = ["run", "--project", "../../OrdersServer"],
    // ▼ EDIT 2: show us the server's log lines (its stderr)
});

var options = new ChatOptions();   // ▼ EDIT 1: connect, and hand the model the server's tools

while (true)
{
    Console.Write("\nQuestion> ");
    var question = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(question)) break;

    List<ChatMessage> messages = [new(ChatRole.System, Rules), new(ChatRole.User, question)];
    var response = await chat.GetResponseAsync(messages, options);

    // Every tool call the model made, across all round trips (Session 7, Build 4)
    foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
        Console.WriteLine($"  [mcp] {call.Name}({string.Join(", ", call.Arguments?.Values ?? [])})");
    Console.WriteLine($"\n{response.Text}\n  [{response.Usage?.InputTokenCount:N0} tokens in, all round trips]");
}
