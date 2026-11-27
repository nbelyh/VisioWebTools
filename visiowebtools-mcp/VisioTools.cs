using System.ComponentModel;
using System.Drawing;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using VsdxTools;

namespace VisioWebTools.Mcp;

/// <summary>
/// MCP tools wrapping the VsdxTools library. All tools work on local file paths,
/// since this server runs locally next to the MCP client (stdio transport).
/// </summary>
[McpServerToolType]
public static class VisioTools
{
    [McpServerTool(Name = "extract_json", ReadOnly = true, Idempotent = true)]
    [Description("Reads a Visio VSDX file and returns its content as JSON: pages, shapes, shape text, fields, shape data (properties), user cells, masters, connections and document properties. Use this to understand or answer questions about a diagram.")]
    public static string ExtractJson(
        [Description("Absolute path to the input .vsdx file")] string vsdxPath,
        [Description("Include shape text")] bool includeShapeText = true,
        [Description("Include text fields")] bool includeShapeFields = true,
        [Description("Include shape data (property) rows")] bool includePropertyRows = true,
        [Description("Include user-defined cells")] bool includeUserRows = false,
        [Description("Include master (stencil) information")] bool includeMasters = true,
        [Description("Include document properties")] bool includeDocumentProperties = true,
        [Description("Include shapes that have no text/data")] bool includeEmptyShapes = false,
        [Description("Include connector shapes and connections")] bool includeConnectors = true)
    {
        var options = new JsonExportOptions
        {
            IncludeShapeText = includeShapeText,
            IncludeShapeFields = includeShapeFields,
            IncludePropertyRows = includePropertyRows,
            IncludeUserRows = includeUserRows,
            IncludeMasters = includeMasters,
            IncludeDocumentProperties = includeDocumentProperties,
            IncludeEmptyShapes = includeEmptyShapes,
            IncludeConnectors = includeConnectors,
        };
        return JsonExportService.Process(ReadInput(vsdxPath), options);
    }

    [McpServerTool(Name = "split_pages", Destructive = false)]
    [Description("Splits a multi-page Visio VSDX file into separate single-page files, returned as a ZIP archive.")]
    public static string SplitPages(
        [Description("Absolute path to the input .vsdx file")] string vsdxPath,
        [Description("Path for the output .zip file. Defaults to '<input>-pages.zip' next to the input.")] string? outputPath = null)
    {
        using var stream = File.OpenRead(CheckExists(vsdxPath));
        var bytes = SplitPagesService.SplitPages(stream);
        return WriteOutput(outputPath ?? Derive(vsdxPath, "-pages", ".zip"), bytes);
    }

    [McpServerTool(Name = "extract_images", Destructive = false)]
    [Description("Extracts embedded media (images) from a Visio VSDX file into a ZIP archive.")]
    public static string ExtractImages(
        [Description("Absolute path to the input .vsdx file")] string vsdxPath,
        [Description("Path for the output .zip file. Defaults to '<input>-media.zip' next to the input.")] string? outputPath = null)
    {
        using var stream = File.OpenRead(CheckExists(vsdxPath));
        var bytes = ExtractMediaService.ExtractMedia(stream);
        return WriteOutput(outputPath ?? Derive(vsdxPath, "-media", ".zip"), bytes);
    }

    [McpServerTool(Name = "add_pdf_tooltips", Destructive = false)]
    [Description("Takes a PDF exported from Visio plus the original VSDX, and adds the Visio shape comments as PDF annotations (tooltips) over the matching shapes.")]
    public static string AddPdfTooltips(
        [Description("Absolute path to the PDF exported from Visio")] string pdfPath,
        [Description("Absolute path to the original .vsdx file")] string vsdxPath,
        [Description("Path for the output PDF. Defaults to '<pdf>-tooltips.pdf'.")] string? outputPath = null,
        [Description("Annotation color, HTML format, e.g. '#FFFFE0'")] string color = "#FFFFE0",
        [Description("Annotation icon: Note, Comment, Help, Insert, Key, NewParagraph, Paragraph")] string icon = "Note",
        [Description("Horizontal icon position relative to shape (0 = left)")] int x = 0,
        [Description("Vertical icon position relative to shape (0 = top)")] int y = 0)
    {
        var options = new PdfOptions
        {
            Color = ColorTranslator.FromHtml(color),
            Icon = icon,
            HorizontalLocation = x,
            VerticalLocation = y,
        };
        using var pdf = File.OpenRead(CheckExists(pdfPath));
        using var vsdx = File.OpenRead(CheckExists(vsdxPath));
        var bytes = PdfService.Process(pdf, vsdx, options);
        return WriteOutput(outputPath ?? Derive(pdfPath, "-tooltips", ".pdf"), bytes);
    }

    [McpServerTool(Name = "cipher_file", Destructive = false)]
    [Description("Obfuscates (scrambles) the text in a Visio VSDX file so it can be shared safely, e.g. for bug reports, while keeping the diagram structure.")]
    public static string CipherFile(
        [Description("Absolute path to the input .vsdx file")] string vsdxPath,
        [Description("Path for the output .vsdx. Defaults to '<input>-ciphered.vsdx'.")] string? outputPath = null,
        bool shapeText = true,
        bool shapeFields = true,
        bool pageNames = true,
        bool propertyValues = true,
        bool propertyLabels = true,
        bool masters = true,
        bool userRows = true,
        bool documentProperties = true)
    {
        var options = new CipherOptions
        {
            EnableCipherShapeText = shapeText,
            EnableCipherShapeFields = shapeFields,
            EnableCipherPageNames = pageNames,
            EnableCipherPropertyValues = propertyValues,
            EnableCipherPropertyLabels = propertyLabels,
            EnableCipherMasters = masters,
            EnableCipherUserRows = userRows,
            EnableCipherDocumentProperties = documentProperties,
        };
        var bytes = CipherService.Process(ReadInput(vsdxPath), options);
        return WriteOutput(outputPath ?? Derive(vsdxPath, "-ciphered", ".vsdx"), bytes);
    }

    [McpServerTool(Name = "get_translatable_text", ReadOnly = true, Idempotent = true)]
    [Description("Step 1 of translating a diagram: returns a JSON document with all translatable strings of a VSDX file. Translate the string values in this JSON (keep keys and structure unchanged), then pass it to apply_translation.")]
    public static string GetTranslatableText(
        [Description("Absolute path to the input .vsdx file")] string vsdxPath,
        bool shapeText = true,
        bool shapeFields = true,
        bool pageNames = true,
        bool propertyValues = true,
        bool propertyLabels = true,
        bool userRows = false)
    {
        return TranslateService.GetTranslationJson(ReadInput(vsdxPath), TranslateOpts(shapeText, shapeFields, pageNames, propertyValues, propertyLabels, userRows));
    }

    [McpServerTool(Name = "apply_translation", Destructive = false)]
    [Description("Step 2 of translating a diagram: writes translated strings (the JSON returned by get_translatable_text, with values translated) back into a copy of the VSDX file. Use the same option flags as in step 1.")]
    public static string ApplyTranslation(
        [Description("Absolute path to the original .vsdx file")] string vsdxPath,
        [Description("The translated JSON, same structure as returned by get_translatable_text")] string translatedJson,
        [Description("Path for the output .vsdx. Defaults to '<input>-translated.vsdx'.")] string? outputPath = null,
        bool shapeText = true,
        bool shapeFields = true,
        bool pageNames = true,
        bool propertyValues = true,
        bool propertyLabels = true,
        bool userRows = false)
    {
        var bytes = TranslateService.ApplyTranslationJson(ReadInput(vsdxPath), TranslateOpts(shapeText, shapeFields, pageNames, propertyValues, propertyLabels, userRows), translatedJson);
        return WriteOutput(outputPath ?? Derive(vsdxPath, "-translated", ".vsdx"), bytes);
    }

    private static TranslateOptions TranslateOpts(bool shapeText, bool shapeFields, bool pageNames, bool propertyValues, bool propertyLabels, bool userRows) => new()
    {
        EnableTranslateShapeText = shapeText,
        EnableTranslateShapeFields = shapeFields,
        EnableTranslatePageNames = pageNames,
        EnableTranslatePropertyValues = propertyValues,
        EnableTranslatePropertyLabels = propertyLabels,
        EnableTranslateUserRows = userRows,
    };

    private static string CheckExists(string path)
    {
        if (!File.Exists(path))
            throw new McpException($"File not found: {path}");
        return path;
    }

    private static byte[] ReadInput(string path) => File.ReadAllBytes(CheckExists(path));

    private static string Derive(string input, string suffix, string ext)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(input))!;
        return Path.Combine(dir, Path.GetFileNameWithoutExtension(input) + suffix + ext);
    }

    private static string WriteOutput(string path, byte[] bytes)
    {
        File.WriteAllBytes(path, bytes);
        return $"Written {bytes.Length} bytes to {Path.GetFullPath(path)}";
    }
}
