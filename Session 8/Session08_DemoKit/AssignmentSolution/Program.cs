// ─────────────────────────────────────────────────────────────────────────────
// Session 08 assignment SOLUTION · Understanding the Model Context Protocol
//
// TODO 2–4 done for Microsoft Learn's MCP server, plus stretch S1 (everything server) and S3 (annotations).
//   dotnet run                      → Microsoft Learn (remote, Streamable HTTP)
//   dotnet run -- everything        → the "everything" test server (stdio, Node 22.19+)
// See EXPLORATION.md for the written half.
// ─────────────────────────────────────────────────────────────────────────────
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var target = args.FirstOrDefault() ?? "learn";
IClientTransport transport = target == "everything"
    ? new StdioClientTransport(new() { Name = "everything", Command = "npx", Arguments = ["-y", "@modelcontextprotocol/server-everything@2026.8.31"] })
    : new HttpClientTransport(new() { Name = "learn", Endpoint = new Uri("https://learn.microsoft.com/api/mcp") });

await using var client = await McpClient.CreateAsync(transport);
Console.WriteLine($"Connected to {client.ServerInfo.Name} {client.ServerInfo.Version} · protocol {client.NegotiatedProtocolVersion}");

// TODO 2 · every tool, with its parameters (from the JSON schema) and, stretch S3, its annotations
var tools = await client.ListToolsAsync();
Console.WriteLine($"\n{tools.Count} tools");
foreach (var tool in tools)
{
    var required = tool.JsonSchema.TryGetProperty("required", out var req) ? req.EnumerateArray().Select(r => r.GetString()).ToHashSet() : [];
    var parameters = tool.JsonSchema.TryGetProperty("properties", out var props)
        ? props.EnumerateObject().Select(p => $"{p.Name}: {(p.Value.TryGetProperty("type", out var t) ? t.ToString() : "?")}{(required.Contains(p.Name) ? "" : "?")}")
        : [];
    var hints = tool.ProtocolTool.Annotations is { } a ? $" · readOnly {a.ReadOnlyHint?.ToString() ?? "?"} · destructive {a.DestructiveHint?.ToString() ?? "?"}" : "";
    Console.WriteLine($"  • {tool.Name}({string.Join(", ", parameters)}){hints}\n      {Short(tool.Description, 110)}");
}

// TODO 3 · call every tool once
if (target == "learn")
{
    var search = await Call("microsoft_docs_search", new() { ["query"] = "ASP.NET Core health checks" });
    // The search results contain learn.microsoft.com links: fetch the first one we find.
    var url = System.Text.RegularExpressions.Regex.Match(search, @"https://learn\.microsoft\.com/[^\s""')\]]+").Value;
    if (url.Length > 0) await Call("microsoft_docs_fetch", new() { ["url"] = url });
    await Call("microsoft_code_sample_search", new() { ["query"] = "IHealthCheck custom health check", ["language"] = "csharp" });
}
else
{
    await Call("echo", new() { ["message"] = "hello from the Session 8 explorer" });
    await Call("get-sum", new() { ["a"] = 7, ["b"] = 35 });
}

// TODO 4 · resources and prompts, only if the server says it supports them
if (client.ServerCapabilities.Resources is null) Console.WriteLine("\nResources: not offered");
else
{
    var resources = await client.ListResourcesAsync();
    Console.WriteLine($"\n{resources.Count} resources");
    foreach (var r in resources) Console.WriteLine($"  • {r.Uri} ({r.MimeType}) {Short(r.Description, 60)}");
    var templates = await client.ListResourceTemplatesAsync();
    foreach (var t in templates) Console.WriteLine($"  • template {t.UriTemplate}");
    if (resources.Count > 0)
    {
        var first = await client.ReadResourceAsync(resources[0].Uri);
        var body = first.Contents.OfType<TextResourceContents>().FirstOrDefault()?.Text;
        Console.WriteLine($"  read {resources[0].Uri} → {Short(body, 120)}");
    }
}
if (client.ServerCapabilities.Prompts is null) Console.WriteLine("Prompts: not offered");
else foreach (var p in await client.ListPromptsAsync()) Console.WriteLine($"  prompt {p.Name}: {Short(p.Description, 70)}");

// Calls a tool, prints how much came back and the start of it, returns the full text.
async Task<string> Call(string name, Dictionary<string, object?> arguments)
{
    var result = await client.CallToolAsync(name, arguments);
    var text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(b => b.Text));
    Console.WriteLine($"\n{name}({string.Join(", ", arguments.Select(a => $"{a.Key}: {a.Value}"))}) → {(result.IsError == true ? "ERROR, " : "")}{text.Length:N0} characters");
    Console.WriteLine($"  {Short(text, 300)}");
    return text;
}

// One line, at most `max` characters: long descriptions and results stay readable on a projector.
static string Short(string? text, int max = 90)
{
    var line = (text ?? "").ReplaceLineEndings(" ").Trim();
    return line.Length <= max ? line : line[..(max - 1)] + "…";
}
