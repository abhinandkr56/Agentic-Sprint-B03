// Build 4 · McpExplorer (FINAL): the official C# SDK does Build 3's plumbing. One client, any server.
//   dotnet run                → Microsoft Learn's MCP server: remote, Streamable HTTP, no key, no sign-up
//   dotnet run -- everything  → the "everything" test server: a local process over stdio (Node 22.19+)
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var target = args.FirstOrDefault() ?? "learn";
IClientTransport transport = target == "everything"
    ? new StdioClientTransport(new() { Name = "everything", Command = "npx", Arguments = ["-y", "@modelcontextprotocol/server-everything@2026.8.31"] })
    : new HttpClientTransport(new() { Name = "learn", Endpoint = new Uri("https://learn.microsoft.com/api/mcp") });

// Connect: the SDK negotiates the protocol version for us (Build 3's EDIT 1 + EDIT 2, automatically).
await using var client = await McpClient.CreateAsync(transport);
Console.WriteLine($"Connected to {client.ServerInfo.Name} {client.ServerInfo.Version} · protocol {client.NegotiatedProtocolVersion}");

// EDIT 1: what can it do? Every tool: a name, a description, a JSON schema. Like Session 7, but discovered.
var tools = await client.ListToolsAsync();
Console.WriteLine($"\n{tools.Count} tools:");
foreach (var tool in tools)
    Console.WriteLine($"  • {tool.Name,-30} {Short(tool.Description, 70)}");

// EDIT 2: call one: a .NET question from chat. No LLM yet: WE pick the tool and the arguments.
var question = "How do I add health checks to an ASP.NET Core app?";
if (target == "learn")
{
    var result = await client.CallToolAsync("microsoft_docs_search", new Dictionary<string, object?> { ["query"] = question });
    var text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(b => b.Text));
    Console.WriteLine($"\nmicrosoft_docs_search → {text.Length:N0} characters back:\n  {Short(text, 300)}");
}

// EDIT 3: what else does it offer? Ask only for what the server said it supports (its capabilities).
if (client.ServerCapabilities.Resources is null) Console.WriteLine("\nResources: not offered");
else foreach (var r in await client.ListResourcesAsync()) Console.WriteLine($"  resource  {r.Uri}");
if (client.ServerCapabilities.Prompts is null) Console.WriteLine("Prompts: not offered");
else foreach (var p in await client.ListPromptsAsync()) Console.WriteLine($"  prompt    {p.Name}: {Short(p.Description, 60)}");

// EDIT 4 is not code:  dotnet run -- everything

// One line, at most `max` characters: long descriptions and results stay readable on a projector.
static string Short(string? text, int max = 90)
{
    var line = (text ?? "").ReplaceLineEndings(" ").Trim();
    return line.Length <= max ? line : line[..(max - 1)] + "…";
}
