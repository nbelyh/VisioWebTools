using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
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
}
