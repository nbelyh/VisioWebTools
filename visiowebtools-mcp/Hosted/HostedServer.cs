namespace VisioWebTools.Mcp.Hosted;

/// <summary>
/// The hosted MCP server: streamable HTTP at /mcp (stateless), plus the file endpoints under /files.
/// <see cref="AddVisioMcpHosting"/> is shared with the Azure Function app, which hosts it in production.
/// </summary>
public static class HostedServer
{
    public const string Instructions =
        "Tools for Microsoft Visio (.vsdx) files: read a diagram, translate it, split its pages, add its shape " +
        "comments to an exported PDF. Files reach these tools only through upload_file, which shows the user a box " +
        "to drop the file into: start there whenever the user wants to work on a file, even one they attached to the chat. " +
        "Results come back as download links; give them to the user.";

    /// <summary>The hosted server's services: settings from the "Mcp" section, storage, links, tools.</summary>
    public static IServiceCollection AddVisioMcpHosting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HostedOptions>()
            .Bind(configuration.GetSection("Mcp"))
            // In Azure the emulator cannot work, and links would be signed with a key derived from its
            // well-known connection string. Checked when the MCP server is first used, so a misconfigured
            // MCP server fails loudly without taking the rest of a function app down with it.
            .Validate(o => !(o.UsesDevelopmentStorage && HostedOptions.RunningInAzure),
                "The MCP server has no storage account: set Mcp__StorageConnectionString (or AzureWebJobsStorage as a " +
                "connection string) in the app settings. Identity-based storage is not supported.");
        services.AddHttpContextAccessor();
        services.AddSingleton<Links>();
        services.AddSingleton<FileStore>();
        services
            .AddMcpServer(o => o.ServerInstructions = Instructions)
            .WithTools<HostedTools>()
            .WithTools([UploadWidget.CreateUploadTool()])
            .WithResources([UploadWidget.CreateResource()]);
        return services;
    }

    /// <summary>Runs the hosted server on its own (development; production runs in the Azure Function app).</summary>
    public static async Task RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddVisioMcpHosting(builder.Configuration);
        builder.Services.AddHostedService<FileCleanup>();

        var app = builder.Build();
        // Under Mcp:BasePath, as in Azure Functions ("/api"), so links and routes agree.
        var routes = app.MapGroup(builder.Configuration["Mcp:BasePath"] ?? "");
        routes.Map("/mcp", McpHttpEndpoint.HandleAsync);
        routes.MapFileEndpoints();
        app.MapGet("/", () => "VisioWebTools MCP server. Connect an MCP client to /mcp.");
        await app.RunAsync();
    }
}
