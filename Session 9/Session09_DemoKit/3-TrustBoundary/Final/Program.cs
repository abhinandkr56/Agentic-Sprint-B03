// Build 3 · TrustBoundary (FINAL): an MCP server is someone else's code, running as YOU, talking to YOUR model.
//   dotnet run      (Node 22.19+; the same "everything" test server as Session 8)
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var options = new StdioClientTransportOptions
{
    Name = "everything",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-everything@2026.8.31"],
    // EDIT 1: don't hand a third-party server your secrets. Start it with a minimal environment instead of ALL of yours.
    InheritEnvironmentVariables = false,
    EnvironmentVariables = StdioClientTransportOptions.GetDefaultEnvironmentVariables(),
};
await using var client = await McpClient.CreateAsync(new StdioClientTransport(options));

// This server has a debugging tool that returns every environment variable ITS process can see.
var result = await client.CallToolAsync("get-env");
var env = JsonNode.Parse(result.Content.OfType<TextContentBlock>().First().Text)!.AsObject();
var secrets = env.Where(v => Regex.IsMatch(v.Key, "(KEY|TOKEN|SECRET|PASSWORD)$", RegexOptions.IgnoreCase)).ToList();
Console.WriteLine($"The server process sees {env.Count} environment variables. Secrets among them: {(secrets.Count == 0 ? "none" : secrets.Count)}");
foreach (var (name, value) in secrets)
    Console.WriteLine($"  !! {name} = {Mask(value?.ToString())}");

// EDIT 2: what does this server get to say to YOUR model? Its instructions and every tool description are prompt text.
var tools = await client.ListToolsAsync();
Console.WriteLine($"\nInstructions for your model: {client.ServerInstructions?.Length ?? 0:N0} characters. They start:\n  \"{Short(client.ServerInstructions, 110)}\"");
Console.WriteLine($"Tool descriptions your model will read: {tools.Sum(t => t.Description.Length):N0} characters across {tools.Count} tools");

// EDIT 3: what does it claim about its own tools? Annotations are hints from the server, not guarantees.
Console.WriteLine("\nWhat the server SAYS about each tool:");
foreach (var tool in tools)
    Console.WriteLine($"  {tool.Name,-30} readOnly: {tool.ProtocolTool.Annotations?.ReadOnlyHint?.ToString() ?? "?",-6} destructive: {tool.ProtocolTool.Annotations?.DestructiveHint?.ToString() ?? "?"}");

// Never print a secret on a projector: first 4 characters only.
static string Mask(string? secret) => secret is { Length: > 4 } ? secret[..4] + new string('•', 12) : "••••";

// One line, at most `max` characters: long descriptions and results stay readable on a projector.
static string Short(string? text, int max = 90)
{
    var line = (text ?? "").ReplaceLineEndings(" ").Trim();
    return line.Length <= max ? line : line[..(max - 1)] + "…";
}
