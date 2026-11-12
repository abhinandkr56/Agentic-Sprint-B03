// Build 2 · AgentMemory (START): does the agent remember what the customer said one message ago?
//   dotnet run      (GROQ_API_KEY)      Try: "Hi, I'm Priya, my order is 4411"  then  "What's my order number?"
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
    You are Asha, the support assistant for our SaaS product. Reply in one or two friendly sentences.
    If you don't know something about the customer, say so: never guess.
    """;

// Build 1's agent
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules);
// ▼ EDIT 2: a session: this conversation's memory

while (true)
{
    Console.Write("\nCustomer> ");
    var message = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(message)) break;
    // ▼ EDIT 3: /new = a new customer

    var response = await agent.RunAsync(message);   // ▼ EDIT 2: pass the session
    Console.WriteLine(response.Text);
    // ▼ EDIT 1: what did that turn cost?
}
