using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace VisioWebTools.Mcp.Hosted;

/// <summary>
/// The tools of the hosted server. Files come in through upload_file (the user drops them into a
/// widget, which sends them straight to the server) and are named by file_id; results go out as
/// download links. File bytes never pass through the model's context.
/// </summary>
[McpServerToolType]
public sealed class HostedTools(FileStore store, Links links, IOptions<HostedOptions> options)
{
    private const string FileIdHelp = "file_id of an uploaded .vsdx file (from upload_file).";

    public const string UploadDescription =
        "Show the user a box in the chat to drop their Visio file (.vsdx) or PDF into. This is the only way a file " +
        "reaches these tools: files attached to the chat do not reach this server, and file contents cannot be passed " +
        "as text. Call it first, ask the user to drop the file into the box, and you are told the file_id once they " +
        "have. The result also has a browser link and a curl command, for chats that show no box. Files are kept for 1 hour.";

    /// <summary>Registered by hand (see <see cref="HostedServer"/>): it carries the widget in its _meta.</summary>
    public CallToolResult UploadFile()
    {
        var o = options.Value;
        var batch = Links.NewId();
        var expires = DateTimeOffset.UtcNow + o.UploadLinkLifetime;
        var url = links.UploadUrl(batch, expires);
        var text =
            $"An upload box is shown to the user. Ask them to drop the .vsdx file (and for add_pdf_tooltips also the PDF) into it; " +
            $"you are told each file's file_id when it arrives. If the user sees no box, they can open this page in a browser " +
            $"and paste back the file id it shows: {url}\n" +
            $"From a terminal (e.g. Claude Code): curl -sT <file> \"{url}\" prints the file_id as JSON.\n" +
            $"The link takes up to {o.MaxFilesPerUpload} files of up to {o.MaxFileBytes / 1048576} MB until {expires:u}; files are kept for {o.FileLifetime.TotalMinutes:0} minutes.";
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = text }],
            StructuredContent = JsonSerializer.SerializeToElement(new
            {
                uploadUrl = url,
                maxFiles = o.MaxFilesPerUpload,
                maxBytes = o.MaxFileBytes,
                expiresAt = expires,
            }),
        };
    }

    [McpServerTool(Name = "extract_json", ReadOnly = true, Idempotent = true)]
    [Description("Reads an uploaded Visio file and returns its content as JSON: pages, shapes, shape text, fields, shape data (properties), user cells, masters, connections and document properties. Use it to understand or answer questions about a diagram.")]
    public async Task<string> ExtractJson(
        [Description(FileIdHelp)] string fileId,
        bool includeShapeText = true,
        bool includeShapeFields = true,
        bool includePropertyRows = true,
        bool includeUserRows = false,
        bool includeMasters = true,
        bool includeDocumentProperties = true,
        [Description("Include shapes that have no text or data")] bool includeEmptyShapes = false,
        bool includeConnectors = true,
        CancellationToken ct = default)
    {
        var file = await Load(fileId, ct);
        return Operations.ExtractJson(file.Content, includeShapeText, includeShapeFields, includePropertyRows,
            includeUserRows, includeMasters, includeDocumentProperties, includeEmptyShapes, includeConnectors);
    }

    [McpServerTool(Name = "split_pages", Destructive = false)]
    [Description("Splits an uploaded multi-page Visio file into single-page .vsdx files, delivered as a ZIP download.")]
    public async Task<string> SplitPages([Description(FileIdHelp)] string fileId, CancellationToken ct = default)
    {
        var file = await Load(fileId, ct);
        return await Deliver(Derive(file, "-pages", ".zip"), Operations.SplitPages(file.Content), "Split the diagram into one file per page.", ct);
    }

    [McpServerTool(Name = "add_pdf_tooltips", Destructive = false)]
    [Description("Takes a PDF exported from Visio and the original .vsdx (both uploaded), and adds the Visio shape comments to the PDF as tooltips (annotations) over the matching shapes. Delivers the new PDF as a download.")]
    public async Task<string> AddPdfTooltips(
        [Description("file_id of the uploaded PDF exported from Visio.")] string pdfFileId,
        [Description("file_id of the uploaded original .vsdx file.")] string vsdxFileId,
        [Description("Annotation color, HTML format, e.g. '#FFFFE0'")] string color = "#FFFFE0",
        [Description("Annotation icon: Note, Comment, Help, Insert, Key, NewParagraph, Paragraph")] string icon = "Note",
        [Description("Horizontal icon position relative to the shape (0 = left)")] int x = 0,
        [Description("Vertical icon position relative to the shape (0 = top)")] int y = 0,
        CancellationToken ct = default)
    {
        var pdf = await Load(pdfFileId, ct);
        var vsdx = await Load(vsdxFileId, ct);
        var bytes = Operations.AddPdfTooltips(pdf.Content, vsdx.Content, color, icon, x, y);
        return await Deliver(Derive(pdf, "-tooltips", ".pdf"), bytes, "Added the shape comments to the PDF.", ct);
    }

    [McpServerTool(Name = "get_translatable_text", ReadOnly = true, Idempotent = true)]
    [Description("Step 1 of translating a diagram: returns a JSON document with all translatable strings of an uploaded Visio file. Translate the string values yourself (keep keys, structure and the {xx0} formatting markers unchanged), then pass it to apply_translation.")]
    public async Task<string> GetTranslatableText(
        [Description(FileIdHelp)] string fileId,
        bool shapeText = true,
        bool shapeFields = true,
        bool pageNames = true,
        bool propertyValues = true,
        bool propertyLabels = true,
        bool userRows = false,
        CancellationToken ct = default)
    {
        var file = await Load(fileId, ct);
        return Operations.GetTranslatableText(file.Content,
            Operations.TranslateOptions(shapeText, shapeFields, pageNames, propertyValues, propertyLabels, userRows));
    }

    [McpServerTool(Name = "apply_translation", Destructive = false)]
    [Description("Step 2 of translating a diagram: writes the translated JSON (from get_translatable_text, values translated) into a copy of the uploaded Visio file and delivers it as a download. Strings left out keep their original text. Use the same option flags as in step 1.")]
    public async Task<string> ApplyTranslation(
        [Description(FileIdHelp)] string fileId,
        [Description("The translated JSON, in the structure get_translatable_text returned.")] string translatedJson,
        bool shapeText = true,
        bool shapeFields = true,
        bool pageNames = true,
        bool propertyValues = true,
        bool propertyLabels = true,
        bool userRows = false,
        CancellationToken ct = default)
    {
        var file = await Load(fileId, ct);
        var bytes = Operations.ApplyTranslation(file.Content,
            Operations.TranslateOptions(shapeText, shapeFields, pageNames, propertyValues, propertyLabels, userRows),
            translatedJson);
        return await Deliver(Derive(file, "-translated", ".vsdx"), bytes, "Translated the diagram.", ct);
    }

    private async Task<StoredFile> Load(string fileId, CancellationToken ct) =>
        await store.GetAsync(fileId?.Trim() ?? "", ct)
        ?? throw new McpException($"No file with file_id '{fileId}': it was not uploaded, or it expired (files are kept for {options.Value.FileLifetime.TotalMinutes:0} minutes). Call upload_file and ask the user to drop it again.");

    private static string Derive(StoredFile input, string suffix, string ext) =>
        Path.GetFileNameWithoutExtension(input.Name) + suffix + ext;

    /// <summary>Stores a result and describes it with its download link.</summary>
    private async Task<string> Deliver(string name, byte[] bytes, string what, CancellationToken ct)
    {
        var id = Links.NewId();
        if (!await store.TryAddAsync(id, name, bytes, ct))
            throw new McpException("Could not store the result; try again.");
        var expires = DateTimeOffset.UtcNow + store.Lifetime;
        var url = links.DownloadUrl(id, name, expires);
        return $"{what}\nDownload for the user: [{name}]({url})\n" +
               $"The link works for {store.Lifetime.TotalMinutes:0} minutes. Its file_id {id} can be passed to the other tools.";
    }
}
