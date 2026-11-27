namespace VsdxTools.Tests;

using System.IO;
using VsdxTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The WASM build and the MCP server pass uploaded files as `new MemoryStream(bytes)`, which cannot grow.
/// The read-only operations must work on such a stream (unlike the ones that rewrite the package).
/// </summary>
[TestClass]
public class FixedSizeStreamTest
{
    private static MemoryStream FixedSize(string path) => new(File.ReadAllBytes(path));

    [TestMethod]
    public void SplitPages()
    {
        using var input = FixedSize(@"../../../../public/samples/SplitMe.vsdx");
        var bytes = SplitPagesService.SplitPages(input);
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        Assert.AreEqual(3, zip.Entries.Count);
    }

    [TestMethod]
    public void ExtractMedia()
    {
        using var input = FixedSize(@"../../../../public/samples/ImageSample.vsdx");
        var bytes = ExtractMediaService.ExtractMedia(input);
        Assert.IsTrue(bytes.Length > 0);
    }

    [TestMethod]
    public void AddTooltips()
    {
        using var vsdx = FixedSize(@"../../../../public/samples/DropMe.vsdx");
        using var pdf = FixedSize(@"../../../../public/samples/DropMe.pdf");
        var bytes = PdfService.Process(pdf, vsdx, new PdfOptions { });
        Assert.IsTrue(bytes.Length > 1000);
    }
}
