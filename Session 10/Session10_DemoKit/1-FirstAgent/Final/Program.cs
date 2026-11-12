// Build 1 · FirstAgent (FINAL): Session 3's IChatClient. Tonight it becomes an AIAgent.
//   dotnet run      (GROQ_API_KEY)      Type a customer message; an empty line quits.
using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client. No UseFunctionInvocation() tonight: the agent adds the tool loop itself (Build 1).
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

const string Rules = """
    You are Asha, the support assistant for our SaaS product.
    Reply in two or three friendly sentences. Never promise dates or refunds you can't see.
    Sign every reply as "Asha, Support".
    """;

// EDIT 1: an agent = the same chat client + instructions + a name. One line.
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules);
// EDIT 2: what did the agent build for us? Session 7's tool loop, without us asking.
Console.WriteLine($"{agent.Name} is ready. Tool loop built in: {agent.GetService<FunctionInvokingChatClient>() is not null}");

while (true)
{
    Console.Write("\nCustomer> ");
    var message = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(message)) break;

    // EDIT 3: stream it. Same agent, same instructions; the words arrive as they're generated.
    await foreach (var update in agent.RunStreamingAsync(message))
        Console.Write(update);
    Console.WriteLine();
}
