// Build 3 · TypedTickets (FINAL: where you end up after the edits)
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient chat = new OpenAIClient(
        new ApiKeyCredential(Environment.GetEnvironmentVariable("GROQ_API_KEY")!),
        new OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
    .GetChatClient("llama-3.3-70b-versatile")
    .AsIChatClient();

const string Rules = "Triage this support ticket for an Indian SaaS company. Money lost or app down = High priority.";

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    // EDIT 1: ask for a Triage object.  EDIT 2: Llama on Groq has no native JSON Schema, so: false
    var response = await chat.GetResponseAsync<Triage>($"{Rules}\n\nTicket: {ticket}",
        useJsonSchemaResponseFormat: false);
    var t = response.Result;
    Console.WriteLine($"→ {t.Category} | {t.Priority} | {t.Summary}");

    // EDIT 3: plain C# on the answer
    if (t.Priority == Priority.High) Console.WriteLine("🚨 Paging the on-call engineer!");
}

// EDIT 1: the shape we want back. Enums = the model can only pick these values.
enum Category { Billing, Bug, HowTo, FeatureRequest, Cancellation }
enum Priority { Low, Medium, High }
record Triage(Category Category, Priority Priority, string Summary);
