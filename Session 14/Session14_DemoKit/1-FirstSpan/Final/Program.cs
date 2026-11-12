// Build 1 · FirstSpan (FINAL): one AI call. The log line says it worked. What did it actually DO?
//   dotnet run      (GROQ_API_KEY)      Paste a support ticket from chat, or press Enter for the default.
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Trace;

const string Model = "openai/gpt-oss-120b";   // EDIT 3: one character missing (from chat)
// EDIT 1: turn on tracing. Listen to M.E.AI's spans; print each one when it ends.
using var tracing = Sdk.CreateTracerProviderBuilder()
    .AddSource("Experimental.Microsoft.Extensions.AI")
    .AddConsoleExporter()
    .Build();

// Session 3's chat client
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient(Model)
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry(configure: o => o.EnableSensitiveData = true)   // EDIT 2: the prompt and the reply go INTO the span
    .Build();

Console.Write("Ticket> ");
var ticket = Console.ReadLine() is { Length: > 0 } typed ? typed : "Hi, I'm Priya (priya@example.com). Order 4411 was cancelled on 2 Oct. Where is my refund?";
List<ChatMessage> messages = [new(ChatRole.System, "Reply to this support ticket in 1-2 sentences. Sign as \"Asha, Support\"."), new(ChatRole.User, ticket)];

// What we've had since Session 1: the answer, and a log line
var timer = Stopwatch.StartNew();
try
{
    var response = await chat.GetResponseAsync(messages);
    Console.WriteLine($"\n{response.Text}");
    Console.WriteLine($"\n[log] {DateTime.Now:HH:mm:ss} replied in {timer.ElapsedMilliseconds} ms");
}
catch (Exception ex)
{
    Console.WriteLine($"\n[log] {DateTime.Now:HH:mm:ss} failed after {timer.ElapsedMilliseconds} ms: {ex.GetType().Name}");
}
