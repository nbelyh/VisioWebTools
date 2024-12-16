namespace VisioWebTools.Tests;

using System.IO;
using VisioWebTools;
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

}