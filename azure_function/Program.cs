using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VisioWebTools.Mcp.Hosted;

var builder = FunctionsApplication.CreateBuilder(args);

// ASP.NET Core integration: the MCP functions work on HttpContext (McpFunctions.cs).
builder.ConfigureFunctionsWebApplication();

builder.Services.AddVisioMcpHosting(builder.Configuration);
builder.Services.PostConfigure<HostedOptions>(o =>
{
    // Functions serve HTTP under its route prefix ("api" unless host.json says otherwise).
    o.BasePath = builder.Configuration["Mcp:BasePath"] ?? "/api";
    // Without a storage account of its own, the MCP server uses the function app's.
    if (builder.Configuration["Mcp:StorageConnectionString"] is null && builder.Configuration["AzureWebJobsStorage"] is { Length: > 0 } jobs)
        o.StorageConnectionString = jobs;
    // Links must be https on the app's public name. The worker gets requests proxied from the Functions
    // host, so the request's own scheme and host are not reliable there.
    if (string.IsNullOrEmpty(o.PublicBaseUrl) && HostedOptions.RunningInAzure
        && Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") is { Length: > 0 } host)
        o.PublicBaseUrl = $"https://{host}";
});

builder.Build().Run();
