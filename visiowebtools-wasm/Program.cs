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
    internal static byte[] TranslateFile(byte[] vsdx, string optionsJson)
    {
        var options = JsonSerializer.Deserialize(optionsJson, TranslateOptionsJsonContext.Default.TranslateOptions);
        return TranslateFileService.Process(vsdx, options);
    }
}
