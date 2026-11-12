// Build 4 · McpExplorer (START): the official C# SDK does Build 3's plumbing. One client, any server.
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

// ▼ EDIT 1: what can it do? (tools)

// ▼ EDIT 2: call one: a .NET question from chat

// ▼ EDIT 3: what else does it offer? (resources, prompts)

// ▼ EDIT 4 is not code:  dotnet run -- everything

// One line, at most `max` characters: long descriptions and results stay readable on a projector.
static string Short(string? text, int max = 90)
{
    var line = (text ?? "").ReplaceLineEndings(" ").Trim();
    return line.Length <= max ? line : line[..(max - 1)] + "…";
}
