namespace azure_function_test;

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SplitFileTest
{
    [TestMethod]
    public void RemovePage()
    {
        using var input = File.OpenRead(@"C:\Projects\VisioWebTools\public\samples\SplitPages.vsdx");

        using var ms = new MemoryStream();

        input.CopyTo(ms);
        var bytes = VisioWebTools.SplitPagesService.SplitFile(ms);

        File.WriteAllBytes(@"C:\Projects\VisioWebTools\public\samples\Pages.zip", bytes);
    }
}