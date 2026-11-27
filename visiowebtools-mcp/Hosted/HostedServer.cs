namespace VisioWebTools.Mcp.Hosted;

/// <summary>
/// The hosted MCP server: streamable HTTP at /mcp (stateless), plus the file endpoints.
/// </summary>
public static class HostedServer
{
    public const string Instructions =
        "Tools for Microsoft Visio (.vsdx) files: read a diagram, translate it, split its pages, add its shape " +
        "comments to an exported PDF. Files reach these tools only through upload_file, which shows the user a box " +
        "to drop the file into: start there whenever the user wants to work on a file, even one they attached to the chat. " +
        "Results come back as download links; give them to the user.";

    public static async Task RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.Configure<HostedOptions>(builder.Configuration.GetSection("Mcp"));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<Links>();
        builder.Services.AddSingleton<FileStore>();
        builder.Services.AddHostedService<FileCleanup>();

        builder.Services
            .AddMcpServer(o => o.ServerInstructions = Instructions)
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<HostedTools>()
            .WithTools([UploadWidget.CreateUploadTool()])
            .WithResources([UploadWidget.CreateResource()]);

        var app = builder.Build();
        app.MapMcp("/mcp");
        app.MapFileEndpoints();
        app.MapGet("/", () => "VisioWebTools MCP server. Connect an MCP client to /mcp.");
        await app.RunAsync();
    }
}
