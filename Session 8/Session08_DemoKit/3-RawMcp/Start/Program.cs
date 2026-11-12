// Build 3 · RawMcp (START): an MCP server is just a program you talk JSON to, one line at a time.
// No SDK, no NuGet package: Process + System.Text.Json. (Session 1 did the same for an LLM over HTTP.)
//   needs Node 22.19+ (npx). First run downloads the server, give it ~10 seconds.
using System.Diagnostics;
using System.Text.Json.Nodes;

// Start the official "everything" test server as a child process. Its stdin and stdout ARE the connection.
// (On Windows, npx is a .cmd script, so it has to go through cmd /c.)
var psi = OperatingSystem.IsWindows()
    ? new ProcessStartInfo("cmd", "/c npx -y @modelcontextprotocol/server-everything@2026.8.31")
    : new ProcessStartInfo("npx", "-y @modelcontextprotocol/server-everything@2026.8.31");
psi.RedirectStandardInput = psi.RedirectStandardOutput = psi.RedirectStandardError = true;
Process server = Process.Start(psi)!;
Console.WriteLine($"Server started (pid {server.Id}). We talk JSON-RPC over its stdin/stdout.\n");

var nextId = 1;

// Send one JSON-RPC message as ONE line. Requests get an id; then we read lines until the reply with
// that id arrives. Anything else the server sends meanwhile is a notification (no id): we show it and move on.
async Task<JsonNode?> Send(string method, JsonObject? parameters = null, bool notification = false)
{
    var message = new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method };
    if (!notification) message["id"] = nextId;
    if (parameters != null) message["params"] = parameters;
    var line = message.ToJsonString();
    Console.WriteLine($">> {Short(line, 150)}");
    await server.StandardInput.WriteLineAsync(line);
    await server.StandardInput.FlushAsync();
    if (notification) return null;

    var id = nextId++;
    while (await server.StandardOutput.ReadLineAsync() is { } reply)
    {
        var node = JsonNode.Parse(reply);
        if (node?["id"]?.GetValue<int>() == id) { Console.WriteLine($"<< {Short(reply, 150)}\n"); return node; }
        Console.WriteLine($"   (notification: {node?["method"]})");
    }
    return null;
}

// ▼ EDIT 1: the 2026 way: "what are you?", no handshake

// ▼ EDIT 2: the 2025 way: the initialize handshake

// ▼ EDIT 3: what tools does it have?

// ▼ EDIT 4: call one: two numbers from chat

server.Kill(entireProcessTree: true);

// One line, at most `max` characters: long descriptions and results stay readable on a projector.
static string Short(string? text, int max = 90)
{
    var line = (text ?? "").ReplaceLineEndings(" ").Trim();
    return line.Length <= max ? line : line[..(max - 1)] + "…";
}
