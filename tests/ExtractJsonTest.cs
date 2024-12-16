namespace VisioWebTools.Tests;

using System.IO;
using VisioWebTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ExtractJsonTest
{
    [DataTestMethod]
    [DataRow(@"../../../../public/samples/ExtractFromMe.vsdx")]
    [DataRow(@"../../../samples/Drawing1.vsdx")]
    [DataRow(@"../../../samples/DocInfo.vsdx")]
    public void ExtractJson(string file)
    {
        var input = File.ReadAllBytes(file);

        var options = new JsonExportOptions
        {
            IncludePropertyRows = true,
            IncludeShapeText = true,
            IncludeShapeFields = true,
            IncludeUserRows = true,
        };

        var json = JsonExportService.Process(input, options);
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Length > 50);

        // File.WriteAllText(@"../../../../public/samples/_.json", json);
    }
}