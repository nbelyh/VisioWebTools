namespace VsdxTools.Tests;

using System.IO;
using VsdxTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ExtractIagesTest
{
    [DataTestMethod]
    [DataRow(@"../../../../public/samples/ImageSample.vsdx")]
    public void ExtractImageSampleFile(string file)
    {
        using var input = File.OpenRead(file);
        var bytes = ExtractMediaService.ExtractMedia(input);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        Assert.AreEqual(3, zip.Entries.Count);
        Assert.IsTrue(zip.GetEntry("pageid_4_shapeid_2_image3.png").Length > 1000);
    }
}