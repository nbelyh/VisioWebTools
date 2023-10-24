namespace azure_function_test;

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SplitFileTest
{
    [TestMethod]
    public void SplitPagesSampleFile()
    {
        using var input = File.OpenRead(@"../../../../public/samples/SplitPages.vsdx");
        var bytes = VisioWebTools.SplitPagesService.SplitPages(input);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        Assert.AreEqual(3, zip.Entries.Count);
        Assert.IsTrue(zip.GetEntry("Page-1.vsdx").Length > 1000);
    }

    [TestMethod]
    public void ExtractImageSampleFile()
    {
        using var input = File.OpenRead(@"../../../../public/samples/ImageSample.vsdx");
        var bytes = VisioWebTools.ExtractMediaService.ExtractMedia(input);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        Assert.AreEqual(3, zip.Entries.Count);
        Assert.IsTrue(zip.GetEntry("pageid_4_shapeid_2_image3.png").Length > 1000);
    }

    [TestMethod]
    public void AddTooltips()
    {
        using var vsdx = File.OpenRead(@"../../../../public/samples/Drawing1.vsdx");
        using var pdf = File.OpenRead(@"../../../../public/samples/Drawing1.pdf");
        var bytes = VisioWebTools.PdfUpdater.Process(pdf, vsdx, new VisioWebTools.PdfOptions { });

        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 1000);
    }
}