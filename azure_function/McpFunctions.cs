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
public class McpFunctions(FileStore store, ILogger<McpFunctions> log)
{
    [Function("Mcp")]
    public async Task<IActionResult> Mcp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "delete", Route = "mcp")] HttpRequest req)
    {
        await McpHttpEndpoint.HandleAsync(req.HttpContext);
        return new EmptyResult();
    }

    [Function("McpUpload")]
    public async Task<IActionResult> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", "get", "options", Route = "files/upload/{batch}")] HttpRequest req,
        string batch)
    {
        var ctx = req.HttpContext;
        var result = HttpMethods.IsPut(req.Method) ? await FileEndpoints.Upload(ctx, batch)
            : HttpMethods.IsGet(req.Method) ? FileEndpoints.UploadPage(ctx, batch)
            : FileEndpoints.Preflight(ctx);
        await result.ExecuteAsync(ctx);
        return new EmptyResult();
    }

    [Function("McpDownload")]
    public async Task<IActionResult> Download(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files/{id}/{name}")] HttpRequest req,
        string id)
    {
        var result = await FileEndpoints.Download(req.HttpContext, id);
        await result.ExecuteAsync(req.HttpContext);
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
