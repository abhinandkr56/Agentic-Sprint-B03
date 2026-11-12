// Build 2 · OneTrace (FINAL): one ticket, two AI calls (triage, then a reply). Build 1's tracing: walls of console text.
//   Dashboard running (README: one docker run) · dotnet run (GROQ_API_KEY) · run it 2-3 times with tickets from chat
using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Build 1's tracing: M.E.AI's spans, printed to the console
using var tracing = Sdk.CreateTracerProviderBuilder()
    .AddSource("Experimental.Microsoft.Extensions.AI")
    .AddSource("SupportDesk")   // EDIT 2: listen to OUR spans too
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SupportDesk"))   // EDIT 1: who is sending
    .AddOtlpExporter(configure: x=> x.Endpoint = new Uri(""))   // EDIT 1: OTLP to localhost:4317: the dashboard
    .Build();
var desk = new ActivitySource("SupportDesk");   // EDIT 2: where our spans come from

// Session 3's chat client
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry()
    .Build();

Console.Write("Ticket> ");
var ticket = Console.ReadLine() is { Length: > 0 } typed ? typed : "My invoice says Rs 11,988 but I cancelled order 4411 on 2 Oct. Where is my refund? priya@example.com";
using var span = desk.StartActivity("handle ticket");   // EDIT 2: one parent: both AI calls become its children

// Step 1: triage
var triage = await chat.GetResponseAsync([new(ChatRole.System, "Classify this support ticket. Answer with ONE word: Billing, Bug or HowTo."), new(ChatRole.User, ticket)]);
var category = triage.Text.Trim();
span?.SetTag("ticket.category", category);   // EDIT 3: business context you can filter on

// Step 2: the reply
var reply = await chat.GetResponseAsync([new(ChatRole.System, $"Reply to this support ticket ({category}) in 1-2 sentences. Sign as \"Asha, Support\"."), new(ChatRole.User, ticket)]);
Console.WriteLine($"\n[{category}] {reply.Text}");
span?.SetTag("ticket.tokens", (triage.Usage?.TotalTokenCount ?? 0) + (reply.Usage?.TotalTokenCount ?? 0));   // EDIT 3: cost per ticket
