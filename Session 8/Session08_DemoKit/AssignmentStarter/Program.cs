// ─────────────────────────────────────────────────────────────────────────────
// Session 08 assignment starter · Understanding the Model Context Protocol
//
// Syllabus: "Explore one existing MCP server with an inspector tool. Write down what tools
//            and resources it exposes, and what each one does."
//
// Two halves: the MCP Inspector (TODO 1) and this small C# explorer (TODO 2–4). Then EXPLORATION.md (TODO 5).
// It runs as-is and connects to Microsoft Learn's MCP server (no key, no sign-up):
//   dotnet run                      → Microsoft Learn (remote, Streamable HTTP)
//   dotnet run -- everything        → stretch S1: the "everything" test server (stdio, Node 22.19+)
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

// ── TODO 1 · Explore it with the MCP Inspector first (no code) ──────────────────
//   npx @modelcontextprotocol/inspector
//   Transport: Streamable HTTP · URL: https://learn.microsoft.com/api/mcp · Connect
//   List the tools, run each one once, open the Resources and Prompts tabs. Note what you see in EXPLORATION.md.

// ── TODO 2 · Every tool, with its parameters (Build 4, EDIT 1) ──────────────────
// For each tool print: name, description, and its parameters from tool.JsonSchema
// (the "properties" object: name + type; and which are "required").
var tools = await client.ListToolsAsync();
Console.WriteLine($"\n{tools.Count} tools (TODO 2: print the parameters too)");

// ── TODO 3 · Call every tool once (Build 4, EDIT 2) ─────────────────────────────
// Learn has three tools. Call each with arguments you choose:
//   microsoft_docs_search        { "query": "..." }
//   microsoft_docs_fetch         { "url": "<a learn.microsoft.com URL from the search results>" }
//   microsoft_code_sample_search { "query": "...", "language": "csharp" }
// Print the size of each result and its first ~300 characters (Short helper below).

// ── TODO 4 · Resources and prompts (Build 4, EDIT 3) ────────────────────────────
// Check client.ServerCapabilities first. List resources (Uri, Name, MimeType) and prompts (Name, Description).
// "Not offered" is a finding: write it down.

// ── TODO 5 · EXPLORATION.md ─────────────────────────────────────────────────────
// A table of every tool and resource: what it does, its inputs, what came back, when a model should use it.

// ── STRETCH (pick one or more) ──────────────────────────────────────────────────
// S1. dotnet run -- everything: compare tools / resources / prompts / protocol version with Learn.
// S2. Build 5's OrdersServer in the Inspector: tools AND a resource. Add a second tool (e.g. GetCustomer).
// S3. Print each tool's annotations (readOnly, destructive…). Which tools could change something? (Session 9 digs in.)
// S4. Endpoint "https://learn.microsoft.com/api/mcp?maxTokenBudget=2000" (experimental): how much smaller are results?
// S5. The filesystem server, limited to ONE folder:  npx -y @modelcontextprotocol/server-filesystem <folder>
//     Which tools could write or delete? What stops it touching other folders?
// S6. Agentic RAG in YOUR API (Build 1): Session 7's stretch S1, SearchNotes(query) as a second tool on /chat.

// One line, at most `max` characters: long descriptions and results stay readable on a projector.
static string Short(string? text, int max = 90)
{
    var line = (text ?? "").ReplaceLineEndings(" ").Trim();
    return line.Length <= max ? line : line[..(max - 1)] + "…";
}
