# Build 2 · Inspector: meet an MCP server without writing code

The MCP Inspector is the official developer tool for MCP servers: a web UI (plus a CLI) that connects to a server
and lets you list and call everything it offers. Needs **Node 22.19 or newer** (`node --version`).

## STEP 1 · Start it and connect to Microsoft Learn

```bash
npx @modelcontextprotocol/inspector
```

It prints a URL with a one-time token and opens your browser. Choose **Streamable HTTP**, URL
`https://learn.microsoft.com/api/mcp`, **Connect**. No key, no sign-up.

## STEP 2 · Tools: list them, run one

**Tools → List Tools**: three tools. Click `microsoft_docs_search`, paste a .NET question from chat into `query`, **Run**.
Compare the description you see with what Session 7's `[Description]` looked like: same idea, someone else wrote it.

## STEP 3 · Resources and Prompts

Open the **Resources** and **Prompts** tabs. Learn offers neither: it's a tools-only server.

## STEP 4 · A local server over stdio

Disconnect. Transport **STDIO**, command `npx`, arguments `-y @modelcontextprotocol/server-everything@2026.8.31`, **Connect**.
Now 14 tools, 7 resources (+2 templates), 4 prompts. Read a resource. Run `get-sum`.
Open the protocol monitor / history: every click is a JSON-RPC message (Build 3 does it by hand).

## Shortcuts

```bash
# open a stdio server straight away
npx @modelcontextprotocol/inspector npx -y @modelcontextprotocol/server-everything@2026.8.31

# the same servers from a config file (same "mcpServers" format VS Code and Claude use)
npx @modelcontextprotocol/inspector --config mcp.json --server everything
npx @modelcontextprotocol/inspector --config mcp.json --server orders      # Build 5's server (run from the kit root)

# a .NET server: run it from its own folder. Don't put --project on the Inspector's command line:
# the Inspector parses that flag itself and dotnet never sees it. (In the web UI's form fields it's fine.)
cd 5-OrdersServer/Final && npx @modelcontextprotocol/inspector dotnet run

# the CLI: no browser, JSON out (good for scripts and for checking a server in CI)
npx @modelcontextprotocol/inspector --cli npx -y @modelcontextprotocol/server-everything@2026.8.31 --method tools/list
npx @modelcontextprotocol/inspector --cli npx -y @modelcontextprotocol/server-everything@2026.8.31 --method tools/call --tool-name get-sum --tool-arg a=7 b=35
```

Fallback if the browser UI misbehaves: the CLI commands above show the same data.
