namespace VisioWebTools.Tests;

using System.IO;
using VisioWebTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CipherTest
{
    [DataRow(@"../../../../public/samples/CipherMe.vsdx")]
    [DataRow(@"../../../samples/DocInfo.vsdx")]
    [DataTestMethod]
    public void CipherFile(string filePath)
    {
        var input = File.ReadAllBytes(filePath);

        var options = new CipherOptions
        {
            EnableCipherShapeText = true,
            EnableCipherShapeFields = true,
            EnableCipherPageNames = true,
            EnableCipherPropertyValues = true,
            EnableCipherPropertyLabels = true,
            EnableCipherMasters = true,
            EnableCipherUserRows = true,
            EnableCipherDocumentProperties = true,
        };

        var bytes = CipherService.Process(input, options);
        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 100);
    }

}