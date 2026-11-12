// Build 1 · LookedRightOnce (FINAL): you ask, it answers, it looks right. Ship it?
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
// EDIT 2: an expected answer: the one fact a correct reply MUST contain
const string MustSay = "9 Oct";
var passed = 0;
// EDIT 3: what evaluating costs: every run is a real call
var timer = Stopwatch.StartNew();
long tokens = 0;

// EDIT 1: the SAME question, 5 times. Same model, same prompt, same facts.
for (var run = 1; run <= 5; run++)
{
    var response = await chat.GetResponseAsync(messages);
    Console.WriteLine($"\n--- run {run} ---\n{response.Text}");
    // EDIT 2: check it, like a unit test would
    var ok = response.Text.Contains(MustSay);
    if (ok) passed++;
    Console.WriteLine(ok ? "  PASS" : $"  FAIL: doesn't say \"{MustSay}\"");
    tokens += response.Usage?.TotalTokenCount ?? 0;   // EDIT 3
}
Console.WriteLine($"\n{passed}/5 passed");   // EDIT 2: a pass RATE, not a pass
Console.WriteLine($"5 runs: {tokens:N0} tokens, {timer.Elapsed.TotalSeconds:N1} s. 50 test cases x 5 runs = {tokens * 50:N0} tokens.");   // EDIT 3
