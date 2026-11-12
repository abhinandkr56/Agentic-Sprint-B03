// Build 4 · McpApi (START): Session 7's /chat in one file: config-driven provider, the tool loop, one local tool.
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

// ▼ EDIT 1: + Microsoft Learn's docs search (a remote MCP server)

// ▼ EDIT 2: + Session 8's OrdersServer (a local MCP server), with a minimal environment

// ▼ EDIT 3: the gate: an MCP tool nobody has reviewed waits for a human

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
