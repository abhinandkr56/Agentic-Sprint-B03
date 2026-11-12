// TicketSorter · Session 02 live build (FINAL version: where you end up after the 3 edits)
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var http = new HttpClient();
http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("GROQ_API_KEY"));

// EDIT 1: role + allowed categories + constraints
// EDIT 2: exact JSON shape
var system = """
    You triage support tickets for an Indian SaaS company.
    Category must be one of: Billing, Bug, HowTo, FeatureRequest, Cancellation.
    Priority must be one of: Low, Medium, High. Money lost or app down = High.
    Respond with ONLY JSON in this shape:
    { "category": "...", "priority": "...", "summary": "max 8 words" }
    """;

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    var answer = await Ask(system, ticket);
    Console.WriteLine(answer);

    // EDIT 2: a real C# object
    var t = JsonSerializer.Deserialize<Triage>(answer,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    Console.WriteLine($"→ {t.Category} | {t.Priority} | {t.Summary}");
    if (t.Priority == "High") Console.WriteLine("🚨 Paging the on-call engineer!");
}

async Task<string> Ask(string system, string user)
{
    var body = new JsonObject
    {
        ["model"] = "llama-3.3-70b-versatile",
        ["temperature"] = 0.7,
        ["messages"] = new JsonArray(
            new JsonObject { ["role"] = "system", ["content"] = system },
            new JsonObject { ["role"] = "user", ["content"] = user }),
        ["response_format"] = new JsonObject { ["type"] = "json_object" }, // EDIT 2
    };
    var res = await http.PostAsync("https://api.groq.com/openai/v1/chat/completions",
        new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
    var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
    return json["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? json.ToJsonString();
}

record Triage(string Category, string Priority, string Summary); // EDIT 2
