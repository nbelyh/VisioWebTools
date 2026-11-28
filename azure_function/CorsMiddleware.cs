using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace VisioWebToolsAzureFunctions;

/// <summary>
/// CORS for every HTTP function, in code rather than in the app's Azure CORS setting (which must stay
/// off: it answers all preflights itself, before they reach the app, and allows only listed origins).
/// <list type="bullet">
/// <item>The MCP server (/api/mcp, /api/files/...): any origin. Its upload widget runs on a chat host's
/// sandbox origin (e.g. a random *.claudemcpcontent.com), and its signed links are the credential.</item>
/// <item>The website's API: only the website (setting Cors:AllowedOrigins, comma-separated).</item>
/// </list>
/// Preflights are answered here; each function lists "options" among its methods so they arrive.
/// </summary>
public sealed class CorsMiddleware(IConfiguration configuration) : IFunctionsWorkerMiddleware
{
    private readonly HashSet<string> websiteOrigins = new(
        (configuration["Cors:AllowedOrigins"] ?? "https://webtools.unmanagedvisio.com")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        StringComparer.OrdinalIgnoreCase);

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is null)
        {
            await next(context);
            return;
        }

        var request = http.Request;
        var origin = request.Headers.Origin.ToString();
        var isMcp = IsMcpPath(request.Path);
        var allowed = isMcp ? "*" : websiteOrigins.Contains(origin) ? origin : null;

        if (allowed is not null && origin.Length > 0)
        {
            var headers = http.Response.Headers;
            // Set when the response starts: functions may replace headers while they run.
            http.Response.OnStarting(() =>
            {
                headers.AccessControlAllowOrigin = allowed;
                if (!isMcp)
                    headers.Vary = "Origin";
                return Task.CompletedTask;
            });
        }

        // Every OPTIONS is answered here: the functions only take it so that preflights reach this code.
        if (HttpMethods.IsOptions(request.Method))
        {
            var response = http.Response;
            if (allowed is not null && request.Headers.ContainsKey("Access-Control-Request-Method"))
            {
                response.Headers.AccessControlAllowMethods = isMcp ? "GET, POST, PUT, DELETE, OPTIONS" : "POST, OPTIONS";
                var requested = request.Headers.AccessControlRequestHeaders.ToString();
                if (requested.Length > 0)
                    response.Headers.AccessControlAllowHeaders = requested;
                response.Headers.AccessControlMaxAge = "3600";
            }
            // A preflight from an origin that is not allowed gets no CORS headers, so the browser refuses.
            response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }

    private static bool IsMcpPath(PathString path) =>
        path.StartsWithSegments("/api/mcp", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/api/files", StringComparison.OrdinalIgnoreCase);
}
