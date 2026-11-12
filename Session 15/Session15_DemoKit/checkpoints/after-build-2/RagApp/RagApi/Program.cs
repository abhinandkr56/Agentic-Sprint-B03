using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// The chat model: Groq by default; AI:Provider=Gemini swaps it. The key comes from the variable KeyVariable names.
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var chatKey = Environment.GetEnvironmentVariable(ai["KeyVariable"]!) ?? "not-set";

IChatClient chat = new OpenAIClient(new ApiKeyCredential(chatKey), new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient()
    .AsBuilder().UseOpenTelemetry().Build();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/ask", async (string q) =>
{
    var response = await chat.GetResponseAsync(q);
    return new { answer = response.Text, model = response.ModelId };
});

app.Run();
