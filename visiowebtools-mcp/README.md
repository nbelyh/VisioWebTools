# VisioWebTools MCP server

A [Model Context Protocol](https://modelcontextprotocol.io) server that exposes the `VsdxTools` library
to AI assistants (Claude Desktop, Claude Code, VS Code, etc.). Runs locally over stdio and works on local file paths.

## Tools

| Tool | What it does |
|---|---|
| `extract_json` | Reads a VSDX and returns pages/shapes/text/shape data/connections as JSON |
| `split_pages` | Splits a multi-page VSDX into single-page files (ZIP) |
| `extract_images` | Extracts embedded media into a ZIP |
| `add_pdf_tooltips` | Adds Visio shape comments as tooltips to a PDF exported from Visio |
| `cipher_file` | Scrambles text in a VSDX so it can be shared safely |
| `get_translatable_text` / `apply_translation` | Two-step translation: the assistant translates the JSON itself, no OpenAI key needed |

Output files default to `<input>-<suffix>.<ext>` next to the input; pass `outputPath` to override.

## Build & run

```sh
dotnet build visiowebtools-mcp
```

Claude Code:

```sh
claude mcp add visio -- dotnet run --project /path/to/VisioWebTools/visiowebtools-mcp
```

Claude Desktop (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "visio": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/VisioWebTools/visiowebtools-mcp"]
    }
  }
}
```

It is also set up as a .NET tool (`PackAsTool`), so after `dotnet pack` it can be installed with
`dotnet tool install -g UnmanagedVisio.VisioWebTools.Mcp` and run as `visio-mcp`.
