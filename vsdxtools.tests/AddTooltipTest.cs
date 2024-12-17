namespace VsdxTools.Tests;

using System.IO;
using VsdxTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AddTooltipTest
{
    [TestMethod]
    public void AddTooltips()
    {
        using var vsdx = File.OpenRead(@"../../../../public/samples/DropMe.vsdx");
        using var pdf = File.OpenRead(@"../../../../public/samples/DropMe.pdf");
        var bytes = PdfUpdater.Process(pdf, vsdx, new PdfOptions { });

        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 1000);
    }

}