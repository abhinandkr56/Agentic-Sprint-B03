// Build 4 · LocalTickets (FINAL: where you end up after the edits)
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

IChatClient cloud = Client("https://api.groq.com/openai/v1", Environment.GetEnvironmentVariable("GROQ_API_KEY")!, "llama-3.3-70b-versatile");
IChatClient local = Client("http://localhost:11434/v1", "ollama", "llama3.2:1b");   // EDIT 1

// EDIT 2: small models need clearer prompts. Session 2's few-shot examples, back on duty.
const string Rules = """
    Triage this support ticket for an Indian SaaS company.
    Priority: High = money lost or the app is down. Medium = broken, but there is a workaround. Low = questions and ideas.
    Examples:
    "UPI payment failed but the money was deducted" → Billing, High
    "The app crashes when I upload a photo, I can use the website instead" → Bug, Medium
    "How do I export my invoices to Excel?" → HowTo, Low
    "Please add dark mode" → FeatureRequest, Low
    """;

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    var c = (await cloud.GetResponseAsync<Triage>($"{Rules}\n\nTicket: {ticket}", useJsonSchemaResponseFormat: false)).Result;
    Console.WriteLine($"☁  70B → {c.Category,-14} {c.Priority,-6} {c.Summary}");

    // EDIT 1: the same call on the 1B laptop model, and NO flag: Ollama accepts a native JSON Schema
    var l = (await local.GetResponseAsync<Triage>($"{Rules}\n\nTicket: {ticket}")).Result;
    Console.WriteLine($"💻  1B → {l.Category,-14} {l.Priority,-6} {l.Summary}");
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
