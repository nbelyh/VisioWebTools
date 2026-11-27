using VisioWebTools.Mcp;
using VisioWebTools.Mcp.Hosted;

// Hosted (HTTP, files uploaded through a widget) with --http; local (stdio, file paths) otherwise.
if (args.Contains("--http"))
{
    await HostedServer.RunAsync(args.Where(a => a != "--http").ToArray());
    return;
}

var builder = Host.CreateApplicationBuilder(args);

// stdout is reserved for the MCP protocol; send all logs to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<LocalTools>();

await builder.Build().RunAsync();
