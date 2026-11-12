// TicketSorter · Session 02 live build (START version: open this before the call)
// Paste a support ticket from chat, get it sorted. We improve it live in 3 small edits.
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var http = new HttpClient();
http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("GROQ_API_KEY"));

// ▼ EDIT 1 and EDIT 2 happen here
var system = "Sort this support ticket.";

while (true)
{
    Console.Write("\nTicket> ");
    var ticket = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(ticket)) break;

    var answer = await Ask(system, ticket);
    Console.WriteLine(answer);
    // ▼ EDIT 2 adds the typed object here
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
        // ▼ EDIT 2 adds one line here
    };
    var res = await http.PostAsync("https://api.groq.com/openai/v1/chat/completions",
        new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
    var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;
    return json["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? json.ToJsonString();
}

// ▼ EDIT 2 adds a record here
