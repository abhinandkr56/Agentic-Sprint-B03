// Build 4 · McpApi (FINAL): Session 7's /chat in one file: config-driven provider, the tool loop, one local tool.
// Tonight it becomes an MCP client: Learn's docs (HTTP) + Session 8's OrdersServer (stdio), and then a gate.
//   dotnet run   →   http://localhost:5080/chat?q=who is on call for payments      (or use McpApi.http)
using System.ClientModel;
using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Sessions 3–4: the provider comes from config. Groq by default; --AI:Provider=Gemini to swap.
var provider = builder.Configuration["AI:Provider"] ?? "Groq";
var ai = builder.Configuration.GetSection($"AI:{provider}");
var key = Environment.GetEnvironmentVariable(ai["KeyVariable"] ?? "") ?? "not-needed";

IChatClient client = new OpenAIClient(new ApiKeyCredential(key),
        new OpenAIClientOptions { Endpoint = new Uri(ai["Endpoint"]!) })
    .GetChatClient(ai["Model"]!)
    .AsIChatClient();
builder.Services.AddChatClient(client).UseFunctionInvocation();

const string Rules = """
    You are our team's assistant. Use the tools when a question needs the data they provide:
    GetOnCall for on-call rotas, GetOrder for customer orders, microsoft_docs_search for .NET and Azure questions.
    Don't guess arguments: ask. Answer in two or three sentences. When you used the docs, end with the learn.microsoft.com link.
    """;

// Session 7's local tool. MCP tools join this same list.
List<AITool> tools = [AIFunctionFactory.Create(GetOnCall, "GetOnCall")];

// EDIT 1: + Microsoft Learn's docs search. Connect once at startup, share it across requests, keep only what we need (Build 1).
await using var learn = await McpClient.CreateAsync(new HttpClientTransport(new() { Name = "learn", Endpoint = new Uri("https://learn.microsoft.com/api/mcp") }));
tools.AddRange((await learn.ListToolsAsync()).Where(t => t.Name == "microsoft_docs_search"));

// EDIT 2: + Session 8's OrdersServer. Build 3's lesson: start it with a minimal environment, so it never sees our keys.
await using var orders = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "orders", Command = "dotnet", Arguments = ["run", "--project", "../../OrdersServer"],
    InheritEnvironmentVariables = false, EnvironmentVariables = StdioClientTransportOptions.GetDefaultEnvironmentVariables(),
}));
tools.AddRange(await orders.ListToolsAsync());

// EDIT 3: the gate. Tools we've reviewed run freely; any other tool waits for a human (Session 7's ApprovalRequiredAIFunction).
string[] reviewed = ["GetOnCall", "microsoft_docs_search"];
tools = [.. tools.Select(t => reviewed.Contains(t.Name) ? t : new ApprovalRequiredAIFunction((AIFunction)t))];

var app = builder.Build();

app.MapGet("/", () => new { provider, model = ai["Model"], tools = tools.Select(t => t.Name) });

// Open in a browser:  http://localhost:5080/chat?q=who is on call for payments
app.MapGet("/chat", async (string q, IChatClient chat) =>
{
    List<ChatMessage> messages = [new(ChatRole.System, Rules), new(ChatRole.User, q)];
    var response = await chat.GetResponseAsync(messages, new ChatOptions { Tools = tools });
    var contents = response.Messages.SelectMany(m => m.Contents).ToList();
    return new
    {
        answer = response.Text,
        provider,
        toolCalls = contents.OfType<FunctionCallContent>().Select(c => $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})"),
        // Anything waiting for a human? (Session 7, Build 5. Session 12 sends the yes/no back.)
        needsApproval = contents.OfType<ToolApprovalRequestContent>()
            .Select(a => a.ToolCall is FunctionCallContent c ? $"{c.Name}({string.Join(", ", c.Arguments?.Values ?? [])})" : "?"),
    };
});

app.Run();

// Session 7's assignment tool: a stub on-call rota. In real life: the PagerDuty or Opsgenie API.
[Description("Gets who is on call right now for an engineering team. Use it when someone needs to page, wake up or reach the on-call engineer.")]
static string GetOnCall([Description("The team name: payments, platform or mobile")] string team)
{
    Console.WriteLine($"  [tool] GetOnCall({team})");
    return team.ToLowerInvariant() switch
    {
        "payments" => "Payments on call: Priya Nair, +91 98450 00001, until 9 am IST tomorrow.",
        "platform" => "Platform on call: Arjun Mehta, +91 98450 00002, until 9 am IST tomorrow.",
        "mobile" => "Mobile on call: Sara Thomas, +91 98450 00003, until 6 pm IST today.",
        _ => $"No on-call rota found for '{team}'. Teams: payments, platform, mobile.",
    };
}
