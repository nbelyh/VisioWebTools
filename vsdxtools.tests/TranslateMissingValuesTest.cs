#nullable enable

namespace VsdxTools.Tests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using VsdxTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Values missing from the translated JSON (e.g. dropped by an LLM) must keep
/// their original text instead of being erased.
/// </summary>
[TestClass]
public class TranslateMissingValuesTest
{
    private const string File1 = @"../../../../public/samples/TranslateMe.vsdx";
    private const string File2 = @"../../../../public/samples/ExtractFromMe.vsdx";

    private static readonly TranslateOptions AllOptions = new()
    {
        EnableTranslateShapeText = true,
        EnableTranslateShapeFields = true,
        EnableTranslatePageNames = true,
        EnableTranslatePropertyValues = true,
        EnableTranslatePropertyLabels = true,
        EnableTranslateUserRows = true
    };

    [TestMethod]
    [DataRow(File1)]
    [DataRow(File2)]
    public void EmptyJsonKeepsEverything(string file)
    {
        var input = File.ReadAllBytes(file);
        var original = TranslateService.GetTranslationJson(input, AllOptions);

        foreach (var json in new[] { "{}", "{\"pages\":{}}", "null" })
        {
            var output = TranslateService.ApplyTranslationJson(input, AllOptions, json);
            var after = TranslateService.GetTranslationJson(output, AllOptions);
            Assert.AreEqual(original, after, $"content changed when applying '{json}'");
        }
    }

    [TestMethod]
    [DataRow(File1)]
    [DataRow(File2)]
    public void MissingValuesKeepOriginal(string file)
    {
        var input = File.ReadAllBytes(file);
        var original = JsonNode.Parse(TranslateService.GetTranslationJson(input, AllOptions))!;

        // "translate" every other string, drop the rest from the JSON
        var translated = original.DeepClone();
        var strings = StringValues(translated).Where(v => v.Parent is JsonObject).ToList();
        Assert.IsTrue(strings.Count >= 2, "sample has too few strings");

        var dropped = new HashSet<string>();
        for (var i = 0; i < strings.Count; i++)
        {
            var v = strings[i];
            if (i % 2 == 1)
            {
                dropped.Add(v.GetPath());
                ((JsonObject)v.Parent!).Remove(v.GetPropertyName());
            }
            else
                v.ReplaceWith(JsonValue.Create("XX " + v.GetValue<string>()));
        }

        var output = TranslateService.ApplyTranslationJson(input, AllOptions, translated.ToJsonString());
        var after = JsonNode.Parse(TranslateService.GetTranslationJson(output, AllOptions))!;

        foreach (var v in StringValues(original))
        {
            var path = v.GetPath();
            var actual = Resolve(after, path)?.GetValue<string>();
            var expected = dropped.Contains(path) ? v.GetValue<string>() : "XX " + v.GetValue<string>();
            Assert.AreEqual(expected, actual, $"at {path}");
        }
    }

    private static IEnumerable<JsonValue> StringValues(JsonNode node) => node switch
    {
        JsonObject o => o.ToList().SelectMany(kv => kv.Value is null ? [] : StringValues(kv.Value)),
        JsonArray a => a.Where(x => x is not null).SelectMany(x => StringValues(x!)),
        JsonValue v when v.TryGetValue<string>(out _) => [v],
        _ => []
    };

    private static JsonNode? Resolve(JsonNode root, string path)
    {
        var node = root;
        foreach (var part in path.TrimStart('$').Split('.', System.StringSplitOptions.RemoveEmptyEntries))
        {
            var key = part.StartsWith("['") ? part[2..^2] : part;
            node = (node as JsonObject)?[key];
            if (node is null) return null;
        }
        return node;
    }
}
