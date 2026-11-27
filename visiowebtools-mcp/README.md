# VisioWebTools MCP server

A [Model Context Protocol](https://modelcontextprotocol.io) server that exposes the `VsdxTools` library
to AI assistants. It runs in two modes:

- **Local** (default, stdio): for Claude Desktop, Claude Code, Cursor, VS Code… Tools read and write files on
  the user's computer by path. Nothing leaves the computer except what the assistant reads.
- **Hosted** (`--http`, streamable HTTP at `/mcp`): for chats on the web (claude.ai, ChatGPT), which can only
  reach servers on the internet. The user drops files into an upload box shown in the chat; files are stored
  for 1 hour and results come back as download links.

## Tools

| Tool | Local | Hosted | What it does |
|---|---|---|---|
| `upload_file` | | ✓ | Shows the upload box (an MCP App widget); the model is told each file's `file_id` |
| `extract_json` | ✓ | ✓ | Pages, shapes, text, shape data and connections as JSON |
| `split_pages` | ✓ | ✓ | One .vsdx per page, as a ZIP |
| `add_pdf_tooltips` | ✓ | ✓ | Adds the Visio shape comments to a PDF exported from Visio |
| `get_translatable_text` / `apply_translation` | ✓ | ✓ | Two-step translation: the assistant translates, no OpenAI key needed |
| `extract_images` | ✓ | | Embedded media as a ZIP |
| `cipher_file` | ✓ | | Scrambles the text, e.g. for bug reports |

## How files reach the hosted server

Files attached to a chat do not reach MCP servers, and having the model type a binary file out is slow and
expensive, so file bytes never pass through the model:

1. `upload_file` creates a signed upload link (30 minutes, up to 5 files of 25 MB) and shows the upload
   box from `widget/`, an [MCP App](https://github.com/modelcontextprotocol/ext-apps) the chat loads into a
   sandboxed iframe.
2. The user drops a file; the box `PUT`s it to `/files/upload/{batch}` and tells the model the `file_id`
   (`updateModelContext`).
3. Tools take `file_id`s and return results as signed download links (`/files/{id}/{name}`), plus a
   `file_id` for chaining.

Chats that show no widgets get a browser upload page and a `curl -T` command in the tool result instead.
There are no accounts: ids are random and links carry an HMAC of what they grant and until when.

## Build & run

Building needs Node.js: the build bundles `widget/` (npm) and embeds it.

```sh
dotnet build visiowebtools-mcp
```

**Local**, in Claude Code:

```sh
claude mcp add visio -- dotnet run --project /path/to/VisioWebTools/visiowebtools-mcp
```

Claude Desktop (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "visio": { "command": "dotnet", "args": ["run", "--project", "/path/to/VisioWebTools/visiowebtools-mcp"] }
  }
}
```

**Hosted**, locally against the [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) storage emulator:

```sh
npx azurite-blob --location .azurite &
dotnet run --project visiowebtools-mcp -- --http --urls http://localhost:5080
claude mcp add --transport http visio-hosted http://localhost:5080/mcp
```

## Hosted in production: the Azure Function app

The hosted server runs inside the existing Function app (`azure_function/McpFunctions.cs`), next to the
website's API, and deploys with it (Azure Pipelines):

| URL | |
|---|---|
| `https://visiowebtools.azurewebsites.net/api/mcp` | The MCP endpoint, for Claude: Settings → Connectors → Add custom connector |
| `/api/files/upload/{batch}` | Uploads (`PUT`), and the upload page for chats without widgets (`GET`) |
| `/api/files/{id}/{name}` | Result downloads |

A timer function deletes expired files every 10 minutes. With no settings, the server uses the function app's
own storage account (`AzureWebJobsStorage`, container `mcp-files`) and derives its link-signing key from that
connection string, so nothing has to be configured, as long as `AzureWebJobsStorage` is a connection string.
If the app uses identity-based storage instead, set `Mcp__StorageConnectionString`: the MCP endpoints then
answer with an error saying so, while the website's functions keep working. To run the same layout locally:
`dotnet run --project visiowebtools-mcp -- --http --Mcp:BasePath=/api`.

## Hosted configuration

Settings come from the `Mcp` configuration section; as environment variables / app settings: `Mcp__Name`.

| Setting | Default | |
|---|---|---|
| `Mcp__StorageConnectionString` | the function app's `AzureWebJobsStorage`; otherwise `UseDevelopmentStorage=true` (Azurite) | The Azure Storage account for files |
| `Mcp__Container` | `mcp-files` | Created on first use |
| `Mcp__SigningKey` | derived from the storage connection string | Base64. Signs upload and download links; set it to make links independent of the storage key |
| `Mcp__PublicBaseUrl` | `https://` + the app's host name in Azure; the request's address elsewhere | Scheme and host for links, without the base path. Set it for a custom domain or behind a proxy |
| `Mcp__BasePath` | `/api` in the function app, empty in the dev server | The route prefix links include |
| `Mcp__FileLifetime` | `01:00:00` | How long files are kept |
| `Mcp__UploadLinkLifetime` | `00:30:00` | |
| `Mcp__MaxFileBytes` / `Mcp__MaxFilesPerUpload` | 25 MB / 5 | |

Expired files are refused at once and deleted every 10 minutes. As a backstop, a lifecycle management rule on
the storage account can delete blobs in `mcp-files` 1 day after creation (Azure's rules count in days).
