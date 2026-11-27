using System.Net;
using Microsoft.Extensions.Options;

namespace VisioWebTools.Mcp.Hosted;

/// <summary>
/// Plain HTTP endpoints for the files themselves, so file bytes never pass through the model:
/// <list type="bullet">
/// <item>PUT /files/upload/{batch}: the upload widget (or curl -T) sends a file; answers its file_id.</item>
/// <item>GET /files/upload/{batch}: a plain upload page, for chats that show no widget.</item>
/// <item>GET /files/{id}/{name}: a result, for the user to download.</item>
/// </list>
/// Links are signed and expire (see <see cref="Links"/>).
/// </summary>
public static class FileEndpoints
{
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/files/upload/{batch}", ["OPTIONS"], (HttpContext ctx) => Preflight(ctx));
        app.MapPut("/files/upload/{batch}", (string batch, HttpContext ctx) => Upload(ctx, batch));
        app.MapGet("/files/upload/{batch}", (string batch, HttpContext ctx) => UploadPage(ctx, batch));
        app.MapGet("/files/{id}/{name}", (string id, HttpContext ctx) => Download(ctx, id));
    }

    public static IResult Preflight(HttpContext ctx)
    {
        AllowCors(ctx);
        return Results.NoContent();
    }

    /// <summary>The widget runs in a sandboxed iframe of the chat host: its origin is the host's, or "null".</summary>
    private static void AllowCors(HttpContext ctx)
    {
        ctx.Response.Headers.AccessControlAllowOrigin = "*";
        ctx.Response.Headers.AccessControlAllowMethods = "PUT, GET, OPTIONS";
        ctx.Response.Headers.AccessControlAllowHeaders = "Content-Type";
        ctx.Response.Headers.AccessControlMaxAge = "3600";
    }

    public static async Task<IResult> Upload(HttpContext ctx, string batch)
    {
        var links = ctx.RequestServices.GetRequiredService<Links>();
        var store = ctx.RequestServices.GetRequiredService<FileStore>();
        var options = ctx.RequestServices.GetRequiredService<IOptions<HostedOptions>>();
        AllowCors(ctx);
        var q = ctx.Request.Query;
        if (!Links.IsValidId(batch) || !links.VerifyUpload(batch, q["exp"], q["sig"]))
            return Results.Text("This upload link is not valid or has expired. Ask for a new one.", statusCode: 403);

        var max = options.Value.MaxFileBytes;
        if (ctx.Request.ContentLength > max)
            return TooLarge(max);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await ctx.Request.Body.ReadAsync(chunk, ctx.RequestAborted)) > 0)
        {
            if (buffer.Length + read > max)
                return TooLarge(max);
            buffer.Write(chunk, 0, read);
        }
        var content = buffer.ToArray();

        var ext = KindOf(content);
        if (ext is null)
            return Results.Text("Only .vsdx and .pdf files can be uploaded.", statusCode: 415);
        var name = CleanName(q["name"], ext);

        // A link takes a few files: each lands in the first free slot, written once.
        for (var slot = 0; slot < options.Value.MaxFilesPerUpload; slot++)
        {
            var id = batch + (char)('a' + slot);
            if (await store.TryAddAsync(id, name, content, ctx.RequestAborted))
                return Results.Json(new { fileId = id, name, size = content.Length });
        }
        return Results.Text($"This upload link has taken its {options.Value.MaxFilesPerUpload} files. Ask for a new one.", statusCode: 409);
    }

    private static IResult TooLarge(int max) =>
        Results.Text($"The file is over the {max / 1048576} MB limit.", statusCode: 413);

    /// <summary>The kind of file, by its first bytes: a .vsdx is a zip package.</summary>
    private static string? KindOf(byte[] content) =>
        content.Length >= 4 && content[0] == 'P' && content[1] == 'K' ? ".vsdx"
        : content.Length >= 5 && "%PDF-"u8.SequenceEqual(content.AsSpan(0, 5)) ? ".pdf"
        : null;

    /// <summary>A file name to show and download as: no path, no control characters, the right extension.</summary>
    private static string CleanName(string? name, string ext)
    {
        var clean = new string(Path.GetFileName(name ?? "").Where(c => !char.IsControl(c) && c is not '"' and not '\\').ToArray()).Trim();
        if (clean.Length > 100)
            clean = clean[..100];
        if (clean.Length == 0)
            clean = "file";
        return clean.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? clean : Path.GetFileNameWithoutExtension(clean) + ext;
    }

    public static async Task<IResult> Download(HttpContext ctx, string id)
    {
        var links = ctx.RequestServices.GetRequiredService<Links>();
        var store = ctx.RequestServices.GetRequiredService<FileStore>();
        var q = ctx.Request.Query;
        if (!Links.IsValidId(id) || !links.VerifyDownload(id, q["exp"], q["sig"]))
            return Results.Text("This download link is not valid or has expired.", statusCode: 403);
        var file = await store.GetAsync(id, ctx.RequestAborted);
        if (file is null)
            return Results.Text("This file is no longer available: files are kept for a limited time.", statusCode: 404);
        ctx.Response.Headers.CacheControl = "no-store";
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.File(file.Content, ContentType(file.Name), file.Name);
    }

    private static string ContentType(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".vsdx" => "application/vnd.ms-visio.drawing",
        ".pdf" => "application/pdf",
        ".zip" => "application/zip",
        ".json" => "application/json",
        _ => "application/octet-stream",
    };

    /// <summary>A plain page to upload files, for chats that do not show the upload widget.</summary>
    public static IResult UploadPage(HttpContext ctx, string batch)
    {
        var links = ctx.RequestServices.GetRequiredService<Links>();
        var q = ctx.Request.Query;
        if (!Links.IsValidId(batch) || !links.VerifyUpload(batch, q["exp"], q["sig"]))
            return Results.Text("This upload link is not valid or has expired. Ask the assistant for a new one.", statusCode: 403);
        var url = WebUtility.HtmlEncode(links.UploadUrl(batch, DateTimeOffset.FromUnixTimeSeconds(long.Parse(q["exp"]!))));
        ctx.Response.Headers.CacheControl = "no-store";
        ctx.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'self'";
        return Results.Content(UploadPageHtml.Replace("{{URL}}", url), "text/html; charset=utf-8");
    }

    private const string UploadPageHtml = """
        <!doctype html>
        <html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Upload a Visio file</title>
        <style>
          :root { color-scheme: light dark; }
          body { font: 15px/1.5 system-ui, sans-serif; max-width: 560px; margin: 40px auto; padding: 0 16px; }
          #drop { border: 2px dashed #8c959f; border-radius: 10px; padding: 32px 16px; text-align: center; }
          #drop.over { border-color: #0969da; }
          li { margin: 6px 0; } code { font-size: 14px; user-select: all; padding: 1px 4px; border: 1px solid #8c959f55; border-radius: 4px; }
          .err { color: #cf222e; } small { color: #6e7781; }
        </style></head>
        <body data-url="{{URL}}">
        <h1>Upload a Visio file</h1>
        <div id="drop"><p><strong>Drop a .vsdx or .pdf file here</strong></p>
        <input id="pick" type="file" accept=".vsdx,.pdf" multiple></div>
        <ul id="files"></ul>
        <p><small>Copy the file id back into the chat. Files are kept for 1 hour, then deleted.</small></p>
        <script>
          const url = document.body.dataset.url, list = document.getElementById('files'), drop = document.getElementById('drop');
          async function send(file) {
            const li = document.createElement('li'); li.textContent = file.name + ': uploading…'; list.append(li);
            try {
              const res = await fetch(url + '&name=' + encodeURIComponent(file.name), { method: 'PUT', body: file });
              if (!res.ok) throw new Error(await res.text());
              const done = await res.json();
              li.textContent = done.name + ': file id ';
              const code = document.createElement('code'); code.textContent = done.fileId; li.append(code);
            } catch (e) { li.className = 'err'; li.textContent = file.name + ': ' + e.message; }
          }
          const take = async (files) => { for (const f of files) await send(f); };
          drop.ondragover = (e) => { e.preventDefault(); drop.className = 'over'; };
          drop.ondragleave = () => drop.className = '';
          drop.ondrop = (e) => { e.preventDefault(); drop.className = ''; take(e.dataTransfer.files); };
          document.getElementById('pick').onchange = (e) => take(e.target.files);
        </script>
        </body></html>
        """;
}
