namespace VsdxTools.Tests;

using System.IO;
using VsdxTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TranslateTest
{
    [DataTestMethod]
    [DataRow(@"../../../../public/samples/TranslateMe.vsdx")]
    public void TranslateFile(string file)
    {
        var input = File.ReadAllBytes(file);

        var options = new TranslateOptions
        {
            EnableTranslateShapeText = true,
            EnableTranslateShapeFields = true,
            EnableTranslatePageNames = true,
            EnableTranslatePropertyValues = true,
            EnableTranslatePropertyLabels = true,
            EnableTranslateUserRows = true
        };

        var json = TranslateService.GetTranslationJson(input, options);

        // var translated = await OpenAIChatService.Translate(json, "German");

        var bytes = TranslateService.ApplyTranslationJson(input, options, json);

        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 100);

        // File.WriteAllBytes(@"../../../../public/samples/_.vsdx", bytes);
    }

    [DataTestMethod]
    [DataRow(@"../../../../public/samples/ExtractFromMe.vsdx")]
    public void TestApplyTranslation(string folder)
    {
        var json = File.ReadAllText("../../../samples/json1.json");
        var input = File.ReadAllBytes(folder);

        var options = new TranslateOptions
        {
            EnableTranslateShapeText = true,
            EnableTranslatePropertyValues = true,
        };

        var bytes = TranslateService.ApplyTranslationJson(input, options, json);

        Assert.IsNotNull(bytes);
        Assert.IsTrue(bytes.Length > 100);
   }

}