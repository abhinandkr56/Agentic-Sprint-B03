// Bonus · Metrics: show, don't type. Not in the 60-minute plan.
// Traces tell ONE request's story. Metrics add them up: tokens and seconds per call, as histograms, over time.
// M.E.AI's UseOpenTelemetry() records both from the same calls: gen_ai.client.token.usage and gen_ai.client.operation.duration.
//   Dashboard running · dotnet run (GROQ_API_KEY) → dashboard → Metrics → Experimental.Microsoft.Extensions.AI
//   Try it with   OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true   set: the prompts appear in the traces (no code change).
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var resource = ResourceBuilder.CreateDefault().AddService("TokenMeter");
using var tracing = Sdk.CreateTracerProviderBuilder().SetResourceBuilder(resource)
    .AddSource("Experimental.Microsoft.Extensions.AI").AddOtlpExporter().Build();
using var metrics = Sdk.CreateMeterProviderBuilder().SetResourceBuilder(resource)
    .AddMeter("Experimental.Microsoft.Extensions.AI")   // same name as the ActivitySource
    .AddOtlpExporter()
    .AddConsoleExporter()                                // and printed here when the app exits
    .Build();

// Session 3's chat client
IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("openai/gpt-oss-120b")
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry()
    .Build();

string[] questions =
[
    "In one sentence: what is a span?",
    "In one sentence: what is a trace?",
    "Explain OpenTelemetry to a C# developer in three short paragraphs.",
    "One word: is OTLP a protocol or a product?",
    "List five things you'd put on a span for an AI call, one line each.",
];
foreach (var q in questions)
{
    var r = await chat.GetResponseAsync(q);
    Console.WriteLine($"{r.Usage?.InputTokenCount,5} in {r.Usage?.OutputTokenCount,5} out   {q}");
}
Console.WriteLine("\nMetrics below (console exporter); the same numbers are on the dashboard's Metrics page.\n");
