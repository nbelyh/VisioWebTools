using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VisioWebTools.Mcp.Hosted;

namespace VisioWebToolsAzureFunctions;

/// <summary>
/// The hosted MCP server (visiowebtools-mcp, Hosted/): /api/mcp for MCP clients such as claude.ai, and
/// /api/files/... for the files users upload through its widget and download as results.
/// </summary>
public class McpFunctions(FileStore store, IHttpContextAccessor http, ILogger<McpFunctions> log)
{
    /// <summary>
    /// The Functions runtime does not set the HTTP context accessor, which links use to take the server's
    /// address when Mcp:PublicBaseUrl is not set (e.g. locally under func start).
    /// </summary>
    private HttpContext Bind(HttpRequest req) => http.HttpContext = req.HttpContext;

    [Function("Mcp")]
    public async Task<IActionResult> Mcp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "delete", "options", Route = "mcp")] HttpRequest req)
    {
        await McpHttpEndpoint.HandleAsync(Bind(req));
        return new EmptyResult();
    }

    [Function("McpUpload")]
    public async Task<IActionResult> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", "get", "options", Route = "files/upload/{batch}")] HttpRequest req,
        string batch)
    {
        var ctx = Bind(req);
        var result = HttpMethods.IsPut(req.Method) ? await FileEndpoints.Upload(ctx, batch)
            : HttpMethods.IsGet(req.Method) ? FileEndpoints.UploadPage(ctx, batch)
            : FileEndpoints.Preflight(ctx);
        await result.ExecuteAsync(ctx);
        return new EmptyResult();
    }

    // Ids are 22+ characters: the constraint keeps GET files/upload/{batch} (the upload page) from
    // matching this route, which the Functions host would otherwise pick over McpUpload.
    [Function("McpDownload")]
    public async Task<IActionResult> Download(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files/{id:minlength(22)}/{name}")] HttpRequest req,
        string id)
    {
        var ctx = Bind(req);
        var result = await FileEndpoints.Download(ctx, id);
        await result.ExecuteAsync(ctx);
        return new EmptyResult();
    }

    /// <summary>Deletes expired files every 10 minutes: a background service would not run while the app is idle.</summary>
    [Function("McpFileCleanup")]
    public async Task Cleanup([TimerTrigger("0 */10 * * * *")] TimerInfo timer, CancellationToken ct)
    {
        var deleted = await store.DeleteExpiredAsync(ct);
        if (deleted > 0)
            log.LogInformation("Deleted {Count} expired MCP files.", deleted);
    }
}
