// Build 2 · AgentMemory (FINAL): does the agent remember what the customer said one message ago?
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
// EDIT 2: a session holds the conversation. The agent sends the WHOLE history every turn: that's the memory (and the bill).
AgentSession session = await agent.CreateSessionAsync();

while (true)
{
    Console.Write("\nCustomer> ");
    var message = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(message)) break;
    // EDIT 3: one agent, many sessions. A new session = a new customer with a blank memory.
    if (message == "/new") { session = await agent.CreateSessionAsync(); Console.WriteLine("  [new session]"); continue; }

    var response = await agent.RunAsync(message, session);   // EDIT 2
    Console.WriteLine(response.Text);
    Console.WriteLine($"  [{response.Usage?.InputTokenCount:N0} tokens in]");   // EDIT 1: what did that turn cost?
}
