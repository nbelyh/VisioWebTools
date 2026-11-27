using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace VisioWebTools.Mcp.Hosted;

/// <summary>
/// The MCP endpoint over streamable HTTP, stateless: each POST is handled by a fresh server, so any
/// instance can answer any request (Azure Functions scales out and restarts freely). Written on the
/// SDK's transport rather than MapMcp so it runs the same in ASP.NET Core and in Azure Functions.
/// </summary>
public static class McpHttpEndpoint
{
    public static async Task HandleAsync(HttpContext ctx)
    {
        var response = ctx.Response;
        if (!HttpMethods.IsPost(ctx.Request.Method))
        {
            // Stateless: no server-to-client stream (GET) and no sessions to end (DELETE).
            response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            response.Headers.Allow = "POST";
            return;
        }

        JsonRpcMessage? message;
        try
        {
            message = await JsonSerializer.DeserializeAsync(ctx.Request.Body,
                McpJsonUtilities.DefaultOptions.GetTypeInfo<JsonRpcMessage>(), ctx.RequestAborted);
        }
        catch (JsonException)
        {
            message = null;
        }
        if (message is null)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            await response.WriteAsync("Expected a JSON-RPC message.", ctx.RequestAborted);
            return;
        }

        var services = ctx.RequestServices;
        var loggers = services.GetRequiredService<ILoggerFactory>();
        var options = services.GetRequiredService<IOptions<McpServerOptions>>().Value;

        await using var transport = new StreamableHttpServerTransport(loggers) { Stateless = true };
        await using var server = McpServer.Create(transport, options, loggers, services);
        var running = server.RunAsync(ctx.RequestAborted);

        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache,no-store";
        if (!await transport.HandlePostRequestAsync(message, response.Body, ctx.RequestAborted))
        {
            // A notification or response: nothing to answer.
            response.ContentType = null;
            response.StatusCode = StatusCodes.Status202Accepted;
        }
        await transport.DisposeAsync();
        try { await running; } catch (OperationCanceledException) { }
    }
}
