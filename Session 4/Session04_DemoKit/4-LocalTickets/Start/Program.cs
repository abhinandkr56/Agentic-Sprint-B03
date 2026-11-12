// Build 4 · LocalTickets (START: open this before the call)
// Session 3's TypedTickets. Groq's 70B Llama needed useJsonSchemaResponseFormat: false.
// What will a 1B model on this laptop need?
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient cloud = Client("https://api.groq.com/openai/v1", Environment.GetEnvironmentVariable("GROQ_API_KEY")!, "llama-3.3-70b-versatile");
// ▼ EDIT 1: a local client

// ▼ EDIT 2: small models need clearer prompts, so add rules and examples
const string Rules = "Triage this support ticket for an Indian SaaS company. Money lost or app down = High priority.";

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    var c = (await cloud.GetResponseAsync<Triage>($"{Rules}\n\nTicket: {ticket}", useJsonSchemaResponseFormat: false)).Result;
    Console.WriteLine($"☁  70B → {c.Category,-14} {c.Priority,-6} {c.Summary}");

    // ▼ EDIT 1: ask the local model the same thing (and leave the flag out)
}

// Build 1's three lines, with the details passed in
IChatClient Client(string endpoint, string key, string model) =>
    new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
        .GetChatClient(model)
        .AsIChatClient();

// Session 3's shape. Enums = the model can only pick these values.
enum Category { Billing, Bug, HowTo, FeatureRequest, Cancellation }
enum Priority { Low, Medium, High }
record Triage(Category Category, Priority Priority, string Summary);
