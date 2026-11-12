// Build 2 · RememberMe (FINAL): does the agent remember you from one message to the next? From one DAY to the next?
//   dotnet run      (GROQ_API_KEY)      Chat as a customer; an empty line quits. Then run it again.
using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client. One client, many agents: each agent adds its own instructions on top.
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

const string Rules = """
    You answer customers of our SaaS product. Look up orders with GetOrder when you need facts.
    Reply in 1-2 sentences and sign as "Asha, Support".
    """;
AIAgent agent = chat.AsAIAgent(name: "SupportAgent", instructions: Rules, tools: [AIFunctionFactory.Create(GetOrder, "GetOrder")]);
// EDIT 1: ONE session for the whole conversation. It holds the history the agent resends every turn (Session 10).
// EDIT 2: yesterday's conversation, if there is one. A session is just data: save it, load it, on any machine.
AgentSession session = File.Exists("session.json")
    ? await agent.DeserializeSessionAsync(JsonDocument.Parse(File.ReadAllText("session.json")).RootElement)
    : await agent.CreateSessionAsync();
Console.WriteLine(File.Exists("session.json") ? "(loaded session.json: welcome back)" : "(new conversation)");

while (true)
{
    Console.Write("\nCustomer> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) break;
    // EDIT 3: "forget me": delete what we stored and start again. (Privacy laws give customers this right.)
    if (line == "/forget")
    {
        File.Delete("session.json");
        session = await agent.CreateSessionAsync();
        Console.WriteLine("  (forgotten: session.json deleted, new session)");
        continue;
    }

    var response = await agent.RunAsync(line, session);
    Console.WriteLine($"\n{response.Text}");
    // EDIT 2: save it after every turn. Open session.json: that's everything the agent "remembers".
    File.WriteAllText("session.json", (await agent.SerializeSessionAsync(session)).GetRawText());
}

// Session 7's tool, unchanged: a stub order system. It only READS.
[Description("Gets an order's status, amount and refund details. Only call it when the customer gives an order number; otherwise ask for it.")]
static string GetOrder([Description("The order number, for example 4411")] int orderId)
{
    Console.WriteLine($"  [tool] GetOrder({orderId})");
    return orderId switch
    {
        4411 => "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.",
        4412 => "Order 4412: Starter plan, monthly, Rs 499, paid by UPI on 1 Oct. Active.",
        4413 => "Order 4413: SMS pack add-on, Rs 299, paid by UPI on 3 Oct. Delivered.",
        _ => $"No order {orderId} found.",
    };
}
