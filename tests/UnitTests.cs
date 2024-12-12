namespace azure_function_test;

using System.IO;
using VisioWebTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading.Tasks;

[TestClass]
public class SplitFileTest
{
    [TestMethod]
    public void SplitPagesSampleFile()
    {
        using var input = File.OpenRead(@"../../../../public/samples/SplitMe.vsdx");
        var bytes = SplitPagesService.SplitPages(input);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        Assert.AreEqual(3, zip.Entries.Count);
        Assert.IsTrue(zip.GetEntry("Page-1.vsdx").Length > 1000);
    }

    [TestMethod]
    public void ExtractImageSampleFile()
    {
        using var input = File.OpenRead(@"../../../../public/samples/ImageSample.vsdx");
        var bytes = ExtractMediaService.ExtractMedia(input);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        Assert.AreEqual(3, zip.Entries.Count);
        Assert.IsTrue(zip.GetEntry("pageid_4_shapeid_2_image3.png").Length > 1000);
    }

    [TestMethod]
    public void AddTooltips()
    {
        using var vsdx = File.OpenRead(@"../../../../public/samples/DropMe.vsdx");
        using var pdf = File.OpenRead(@"../../../../public/samples/DropMe.pdf");
        var bytes = PdfUpdater.Process(pdf, vsdx, new PdfOptions { });

        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 1000);
    }

    [TestMethod]
    public void TranslateFile()
    {
        var input = File.ReadAllBytes(@"../../../../public/samples/TranslateMe.vsdx");

        var options = new TranslateOptions
        {
            EnableTranslateShapeText = true,
            EnableTranslateShapeFields = true,
            EnableTranslatePageNames = true,
            EnableTranslatePropertyValues = true,
            EnableTranslatePropertyLabels = true
        };

        var json = TranslateFileService.GetTranslationJson(input, options);

        // var translated = await OpenAIChatService.Translate(json, "German");

        var bytes = TranslateFileService.ApplyTranslationJson(input, options, json);

        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 100);

        // File.WriteAllBytes(@"../../../../public/samples/_.vsdx", bytes);
    }

    [TestMethod]
    public void CipherFile()
    {
        var input = File.ReadAllBytes(@"../../../../public/samples/CipherMe.vsdx");

        var options = new CipherOptions
        {
            EnableCipherShapeText = true,
            EnableCipherShapeFields = true,
            EnableCipherPageNames = true,
            EnableCipherPropertyValues = true,
            EnableCipherPropertyLabels = true,
            EnableCipherMasters = true
        };

        var bytes = CipherFileService.Process(input, options);
        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 100);
        // File.WriteAllBytes(@"../../../../public/samples/_.vsdx", bytes);
    }
}