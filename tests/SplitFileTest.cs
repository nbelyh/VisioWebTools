namespace VsdxTools.Tests;

using System.IO;
using VsdxTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SplitFileTest
{
    [DataTestMethod]
    [DataRow(@"../../../../public/samples/SplitMe.vsdx")]
    public void SplitPagesSampleFile(string file)
    {
        using var input = File.OpenRead(file);
        var bytes = SplitPagesService.SplitPages(input);

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        Assert.AreEqual(3, zip.Entries.Count);
        Assert.IsTrue(zip.GetEntry("Page-1.vsdx").Length > 1000);
    }
}