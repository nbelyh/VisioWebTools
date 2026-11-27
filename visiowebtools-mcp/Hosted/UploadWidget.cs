using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace VisioWebTools.Mcp.Hosted;

/// <summary>
/// The upload box, an MCP App (the MCP "ui" extension): upload_file names this page in
/// _meta.ui.resourceUri, and hosts that render MCP Apps load it into a sandboxed iframe and hand
/// it the tool's result. Built from widget/ into one HTML file, embedded in this assembly.
/// </summary>
public static class UploadWidget
{
    public const string MimeType = "text/html;profile=mcp-app";

    public static readonly string Html = Load();

    /// <summary>
    /// Hosts cache a ui:// page by its address, so the address carries a hash of the page:
    /// a new build is a new address.
    /// </summary>
    public static readonly string Uri =
        $"ui://visiowebtools/upload-{Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Html)))[..10]}.html";

    private static string Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("upload.html")
            ?? throw new InvalidOperationException("The upload widget is not embedded: build widget/ (npm run build).");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// The page may fetch only what its policy declares: its own server, for the upload.
    /// </summary>
    public static JsonObject Meta(string serverOrigin) => new()
    {
        ["ui"] = new JsonObject
        {
            ["csp"] = new JsonObject
            {
                ["connectDomains"] = new JsonArray(serverOrigin),
                ["resourceDomains"] = new JsonArray(),
            },
            ["prefersBorder"] = true,
        },
    };

    public static McpServerResource CreateResource() => McpServerResource.Create(
        (RequestContext<ReadResourceRequestParams> ctx) =>
        {
            var links = ctx.Services!.GetRequiredService<Links>();
            return new TextResourceContents { Uri = Uri, MimeType = MimeType, Text = Html, Meta = Meta(links.Origin) };
        },
        new McpServerResourceCreateOptions { UriTemplate = Uri, Name = "upload", Title = "Upload a Visio file", MimeType = MimeType });

    public static McpServerTool CreateUploadTool() => McpServerTool.Create(
        typeof(HostedTools).GetMethod(nameof(HostedTools.UploadFile))!,
        ctx => ActivatorUtilities.CreateInstance<HostedTools>(ctx.Services!),
        new McpServerToolCreateOptions
        {
            Name = "upload_file",
            Title = "Upload a Visio file",
            Description = HostedTools.UploadDescription,
            ReadOnly = true,
            Destructive = false,
            Meta = new JsonObject { ["ui"] = new JsonObject { ["resourceUri"] = Uri } },
        });
}
