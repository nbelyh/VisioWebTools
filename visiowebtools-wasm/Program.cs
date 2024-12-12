using System.Runtime.InteropServices.JavaScript;
using System.Drawing;
using VisioWebTools;
using System.Runtime.Versioning;
using System.Text.Json;

// Create a "Main" method. This is required by the tooling.
return;

public partial class FileProcessor
{
    // Make the method accessible from JS
    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] SplitPages(byte[] vsdx)
    {
        using (var stream = new MemoryStream(vsdx))
        {
            return SplitPagesService.SplitPages(stream);
        }
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] ExtractImages(byte[] vsdx)
    {
        using (var stream = new MemoryStream(vsdx))
        {
            return ExtractMediaService.ExtractMedia(stream);
        }
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] AddTooltips(byte[] pdf, byte[] vsdx, string color, string icon, int x, int y)
    {
        var options = new PdfOptions
        {
            Color = ColorTranslator.FromHtml(color),
            Icon = icon,
            HorizontalLocation = x,
            VerticalLocation = y
        };

        using (var pdfStream = new MemoryStream(pdf))
        using (var vsdxStream = new MemoryStream(vsdx))
        {
            return PdfUpdater.Process(pdfStream, vsdxStream, options);
        }
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] CipherFile(byte[] vsdx, string optionsJson)
    {
        var options = JsonSerializer.Deserialize(optionsJson, CipherOptionsJsonContext.Default.CipherOptions);
        return CipherFileService.Process(vsdx, options);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static string GetTranslationJson(byte[] vsdx, string optionsJson)
    {
        var options = JsonSerializer.Deserialize(optionsJson, TranslateOptionsJsonContext.Default.TranslateOptions);
        var translations = TranslateFileService.GetTranslationJson(vsdx, options);
        return translations;
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static byte[] ApplyTranslationJson(byte[] vsdx, string optionsJson, string json)
    {
        var options = JsonSerializer.Deserialize(optionsJson, TranslateOptionsJsonContext.Default.TranslateOptions);
        var bytes = TranslateFileService.ApplyTranslationJson(vsdx, options, json);
        return bytes;
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    internal static async Task<string> Translate(string json, string language, string? apiKey = null)
    {
        var translated = await OpenAIChatService.Translate(json, language, apiKey);
        return translated;
    }
}
