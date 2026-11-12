using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
// ── TODO 2 · builder.AddServiceDefaults();   (after you created ServiceDefaults: RUN.md step 2)

// The chat model: Groq by default; AI:Provider=Gemini swaps it. The key comes from the variable KeyVariable names.
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var chatKey = Environment.GetEnvironmentVariable(ai["KeyVariable"]!) ?? "not-set";

IChatClient chat = new OpenAIClient(new ApiKeyCredential(chatKey), new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient();   // ── TODO 3 · .AsBuilder().UseOpenTelemetry().Build() (and AddSource in ServiceDefaults)

// ── TODO 5 · the embedding model (Gemini) and the vector store (PostgresVectorStore, a Note class, EnsureCollectionExistsAsync)

var app = builder.Build();
// ── TODO 2 · app.MapDefaultEndpoints();
// ── TODO 6 · POST /notes: UpsertAsync the note

app.MapGet("/ask", async (string q) =>
{
    // ── TODO 7 · SearchAsync the 3 closest notes first, then answer from them only
    var response = await chat.GetResponseAsync(q);
    return new { answer = response.Text, model = response.ModelId };
});

app.Run();
