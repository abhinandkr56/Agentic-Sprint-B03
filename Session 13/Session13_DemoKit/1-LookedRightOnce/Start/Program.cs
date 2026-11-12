// Build 1 · LookedRightOnce (START): you ask, it answers, it looks right. Ship it?
//   dotnet run      (GROQ_API_KEY)      Paste a customer's question from chat, or press Enter for the default.
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;

// Session 3's chat client
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient();

const string Rules = """
    You answer questions from customers of our SaaS product.
    Use only the FACTS you are given. Reply in 1-2 sentences and sign as "Asha, Support".
    """;
// What Session 10's GetOrder returns for order 4411
const string Facts = "Order 4411: Pro plan, annual, Rs 11,988, paid by card on 28 Sep. Cancelled on 2 Oct. Refund of Rs 10,989 issued on 2 Oct, expected by 9 Oct.";

Console.Write("Question> ");
var question = Console.ReadLine() is { Length: > 0 } typed ? typed : "When will I get my refund for order 4411?";
List<ChatMessage> messages = [new(ChatRole.System, Rules), new(ChatRole.User, $"FACTS:\n{Facts}\n\nQUESTION:\n{question}")];
// ▼ EDIT 2: what a correct answer MUST say
// ▼ EDIT 3: what it costs

// One question, one answer, a quick read. Looks right? That's how most AI features get "tested".     ▼ EDIT 1
var response = await chat.GetResponseAsync(messages);
Console.WriteLine($"\n{response.Text}");
