using System.Drawing;
using ModelContextProtocol;
using VsdxTools;

namespace VisioWebTools.Mcp;

/// <summary>
/// The VsdxTools operations on bytes, shared by the local tools (file paths) and the hosted
/// tools (uploaded files). Errors a model can act on are thrown as <see cref="McpException"/>,
/// whose message reaches the model.
/// </summary>
public static class Operations
{
    public static string ExtractJson(byte[] vsdx, bool shapeText, bool shapeFields, bool propertyRows, bool userRows,
        bool masters, bool documentProperties, bool emptyShapes, bool connectors)
    {
        RequireVsdx(vsdx);
        return JsonExportService.Process(vsdx, new JsonExportOptions
        {
            IncludeShapeText = shapeText,
            IncludeShapeFields = shapeFields,
            IncludePropertyRows = propertyRows,
            IncludeUserRows = userRows,
            IncludeMasters = masters,
            IncludeDocumentProperties = documentProperties,
            IncludeEmptyShapes = emptyShapes,
            IncludeConnectors = connectors,
        });
    }

    public static byte[] SplitPages(byte[] vsdx)
    {
        RequireVsdx(vsdx);
        using var stream = new MemoryStream(vsdx);
        return SplitPagesService.SplitPages(stream);
    }

    public static byte[] ExtractImages(byte[] vsdx)
    {
        RequireVsdx(vsdx);
        using var stream = new MemoryStream(vsdx);
        return ExtractMediaService.ExtractMedia(stream);
    }

    public static byte[] AddPdfTooltips(byte[] pdf, byte[] vsdx, string color, string icon, int x, int y)
    {
        RequirePdf(pdf);
        RequireVsdx(vsdx);
        Color parsed;
        try { parsed = ColorTranslator.FromHtml(color); }
        catch (Exception) { throw new McpException($"Not a color: '{color}'. Use HTML format, e.g. #FFFFE0."); }

        using var pdfStream = new MemoryStream(pdf);
        using var vsdxStream = new MemoryStream(vsdx);
        return PdfService.Process(pdfStream, vsdxStream, new PdfOptions
        {
            Color = parsed,
            Icon = icon,
            HorizontalLocation = x,
            VerticalLocation = y,
        });
    }

    public static byte[] Cipher(byte[] vsdx, CipherOptions options)
    {
        RequireVsdx(vsdx);
        return CipherService.Process(vsdx, options);
    }

    public static TranslateOptions TranslateOptions(bool shapeText, bool shapeFields, bool pageNames,
        bool propertyValues, bool propertyLabels, bool userRows) => new()
    {
        EnableTranslateShapeText = shapeText,
        EnableTranslateShapeFields = shapeFields,
        EnableTranslatePageNames = pageNames,
        EnableTranslatePropertyValues = propertyValues,
        EnableTranslatePropertyLabels = propertyLabels,
        EnableTranslateUserRows = userRows,
    };

    public static string GetTranslatableText(byte[] vsdx, TranslateOptions options)
    {
        RequireVsdx(vsdx);
        return TranslateService.GetTranslationJson(vsdx, options);
    }

    public static byte[] ApplyTranslation(byte[] vsdx, TranslateOptions options, string translatedJson)
    {
        RequireVsdx(vsdx);
        try
        {
            return TranslateService.ApplyTranslationJson(vsdx, options, translatedJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new McpException($"translatedJson is not valid JSON in the shape get_translatable_text returns: {ex.Message}");
        }
    }

    public static void RequireVsdx(byte[] bytes)
    {
        if (!IsZip(bytes))
            throw new McpException("That is not a .vsdx file. Older .vsd files are not supported: save them as .vsdx in Visio first.");
        if (!IsVsdx(bytes))
            throw new McpException("That is a zip package, but not a Visio drawing (.vsdx): for example a Word or Excel file.");
    }

    public static bool IsZip(byte[] bytes) => bytes.Length >= 4 && bytes[0] == 'P' && bytes[1] == 'K';

    /// <summary>
    /// A Visio drawing: a zip package whose content types declare a Visio document part
    /// ("application/vnd.ms-visio.drawing.main+xml", or the macro-enabled variant).
    /// </summary>
    public static bool IsVsdx(byte[] bytes)
    {
        if (!IsZip(bytes))
            return false;
        try
        {
            using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes, writable: false), System.IO.Compression.ZipArchiveMode.Read);
            var types = zip.GetEntry("[Content_Types].xml");
            if (types is null || types.Length > 1024 * 1024)
                return false;
            using var reader = new StreamReader(types.Open());
            return reader.ReadToEnd().Contains("application/vnd.ms-visio.drawing", StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    public static void RequirePdf(byte[] bytes)
    {
        if (bytes.Length < 5 || !"%PDF-"u8.SequenceEqual(bytes.AsSpan(0, 5)))
            throw new McpException("That is not a PDF file.");
    }
}
