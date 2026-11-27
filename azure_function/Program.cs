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
});

builder.Build().Run();
