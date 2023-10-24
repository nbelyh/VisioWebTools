using System.Runtime.InteropServices.JavaScript;
using System.Drawing;
using VisioWebTools;

// Create a "Main" method. This is required by the tooling.
return;

public partial class FileProcessor
{
    // Make the method accessible from JS
    [JSExport]
    internal static byte[] SplitPages(byte[] vsdx)
    {
        return SplitPagesService.SplitPages(vsdx);
    }

    [JSExport]
    internal static byte[] ExtractImages(byte[] vsdx)
    {
        return ExtractMediaService.ExtractMedia(vsdx);
    }

    [JSExport]
    internal static byte[] AddTooltips(byte[] pdf, byte[] vsdx, string color, string icon, int x, int y)
    {
        var options = new PdfOptions
        {
            Color = ColorTranslator.FromHtml(color),
            Icon = icon,
            HorizontalLocation = x,
            VerticalLocation = y
        };
        return PdfUpdater.Process(pdf, vsdx, options);
    }
}
