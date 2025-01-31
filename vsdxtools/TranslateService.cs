using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.Json;
using System.Collections.Generic;
using VsdxTools.Models;
using VsdxTools.Serialization;

namespace VsdxTools;

/// <summary>
/// Service for generating random readable strings with specific constraints.
/// </summary>
public class TranslateService
{
    enum TranslationDirection
    {
        Get,
        Set
    }

    class TranslationContext
    {
        public DocumentInfo DocumentInfo { get; set; }
        public TranslateOptions Options { get; set; }
        public TranslationDirection Direction { get; set; }

        public Dictionary<string, string> TranslatedText { get; } = [];

        public Dictionary<string, string> TranslatedUserRowPrompts { get; } = [];
        public Dictionary<string, string> TranslatedPageNames { get; } = [];
        public Dictionary<string, string> TranslatedPropertyLabels { get; } = [];
        public Dictionary<string, string> TranslatedPropertyFormats { get; } = [];
    }

    private static void ProcessPage(PackagePart pagePart, PageInfo pageInfo, TranslationContext context)
    {
        var fileAccess = context.Direction == TranslationDirection.Get ? FileAccess.Read : FileAccess.ReadWrite;
        var pageStream = pagePart.GetStream(FileMode.Open, fileAccess);
        var xmlPage = XDocument.Load(pageStream);

        var xmlShapes = xmlPage.XPathSelectElements("v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
        foreach (var xmlShape in xmlShapes)
        {
            ShapeInfo ensureShape() => DiagramInfoService.EnsureCollection(xmlShape, () => pageInfo.Shapes ??= []);

            if (context.Options.EnableTranslateShapeText)
                ProcessShapeText(xmlShape, ensureShape, context);

            if (context.Options.EnableTranslateShapeFields)
                TranslateShapeFields(xmlShape, () => ensureShape().FieldRows ??= [], context);

            if (context.Options.EnableTranslatePropertyValues)
                TranslatePropertyValues(xmlShape, () => ensureShape().PropRows ??= [], context);

            if (context.Options.EnableTranslatePropertyLabels)
                TranslatePropertyLabels(xmlShape, () => ensureShape().PropRows ?? [], context);

            if (context.Options.EnableTranslateUserRows)
                ProcessUserRows(xmlShape, () => pageInfo.UserRows ??= [], context);
        }

        if (context.Direction == TranslationDirection.Set)
        {
            VisioParser.FlushStream(xmlPage, pageStream);
        }
    }

    private static void ProcessValue(
        TranslationDirection translationDirection,
        Dictionary<string, string> translatedValues,
        Action<string> setJsonValue, Func<string> getShapeValue,
        Action<string> setShapeValue, Func<string> getJsonValue)
    {
        var shapeValue = getShapeValue();
        switch (translationDirection)
        {
            case TranslationDirection.Get:
                if (translatedValues.TryAdd(shapeValue, shapeValue))
                    setJsonValue(shapeValue);
                break;

            case TranslationDirection.Set:
                if (!translatedValues.TryGetValue(shapeValue, out var jsonValue))
                {
                    jsonValue = getJsonValue();
                    translatedValues.Add(shapeValue, jsonValue);
                }
                setShapeValue(jsonValue);
                break;
        }

    }

    private static void ProcessShapeText(XElement xmlShape, Func<ShapeInfo> ensureShape, TranslationContext context)
    {
        var xmlText = xmlShape.XPathSelectElement("v:Text", VisioParser.NamespaceManager);
        if (xmlText == null)
            return;

        var text = FormattedTextService.GetShapeText(xmlText);
        if (VisioParser.IsTextValue(text?.PlainText))
        {
            ProcessValue(context.Direction, context.TranslatedText,
                (value) => ensureShape().Text = value,
                () => text.FormattedText,
                (value) => FormattedTextService.BuildXElements(xmlText, value),
                () => ensureShape().Text
            );
        }
    }

    private static void ProcessUserRows(XElement xmlShape, Func<Dictionary<string, UserRowInfo>> ensureUserRow, TranslationContext context)
    {
        var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='User']/v:Row", VisioParser.NamespaceManager).ToList();
        foreach (var xmlRow in xmlRows)
        {
            var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
            var attributeValue = xmlValue?.Attribute("V");
            if (VisioParser.IsTextValue(attributeValue?.Value))
            {
                UserRowInfo ensureRow() => DiagramInfoService.EnsureCollection(xmlRow, ensureUserRow);
                ProcessValue(context.Direction, context.TranslatedText,
                    (value) => ensureRow().Value = value,
                    () => attributeValue.Value,
                    (value) => attributeValue.Value = value,
                    () => ensureRow().Value
                );
            }

            var xmlPrompt = xmlRow.XPathSelectElement("v:Cell[@N='Prompt']", VisioParser.NamespaceManager);
            var attributePrompt = xmlPrompt?.Attribute("V");
            if (VisioParser.IsTextValue(attributePrompt?.Value))
            {
                UserRowInfo ensureRow() => DiagramInfoService.EnsureCollection(xmlRow, ensureUserRow);
                ProcessValue(context.Direction, context.TranslatedUserRowPrompts,
                    (value) => ensureRow().Prompt = value,
                    () => attributePrompt.Value,
                    (value) => attributePrompt.Value = value,
                    () => ensureRow().Prompt
                );
            }
        }
    }

    private static void TranslateShapeFields(XElement xmlShape, Func<Dictionary<string, FieldInfo>> ensureField, TranslationContext context)
    {
        var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Field']/v:Row", VisioParser.NamespaceManager).ToList();
        foreach (var xmlRow in xmlRows)
        {
            var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value' and @U='STR']", VisioParser.NamespaceManager);
            var attributeValue = xmlValue?.Attribute("V");
            if (VisioParser.IsTextValue(attributeValue?.Value))
            {
                FieldInfo ensureRow() => DiagramInfoService.EnsureCollection(xmlRow, ensureField);
                ProcessValue(context.Direction, context.TranslatedText,
                    (value) => ensureRow().Value = value,
                    () => attributeValue.Value,
                    (value) => attributeValue.Value = value,
                    () => ensureRow().Value
                );
            }
        }
    }

    private static void TranslatePropertyLabels(XElement xmlShape, Func<Dictionary<string, PropertyInfo>> ensureProp, TranslationContext context)
    {
        var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
        foreach (var xmlRow in xmlRows)
        {
            var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Label']", VisioParser.NamespaceManager);
            var attributeValue = xmlValue?.Attribute("V");
            if (VisioParser.IsTextValue(attributeValue?.Value))
            {
                PropertyInfo ensureRow() => DiagramInfoService.EnsureCollection(xmlRow, ensureProp);
                ProcessValue(context.Direction, context.TranslatedPropertyLabels,
                    (value) => ensureRow().Label = value,
                    () => attributeValue.Value,
                    (value) => attributeValue.Value = value,
                    () => ensureRow().Label
                );
            }
        }
    }

    private static void TranslatePropertyValues(XElement xmlShape, Func<Dictionary<string, PropertyInfo>> ensureProp, TranslationContext context)
    {
        var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
        foreach (var xmlRow in xmlRows)
        {
            var xmlType = xmlRow.XPathSelectElement("v:Cell[@N='Type']", VisioParser.NamespaceManager);
            var typeValue = xmlType?.Attribute("V")?.Value ?? "0";
            if (!int.TryParse(typeValue, out int type))
                type = 0;

            switch (type)
            {
                case 0:  /* String */
                    {
                        var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
                        var attributeValue = xmlValue?.Attribute("V");
                        if (VisioParser.IsTextValue(attributeValue?.Value))
                        {
                            PropertyInfo ensureRow() => DiagramInfoService.EnsureCollection(xmlRow, ensureProp);
                            ProcessValue(context.Direction, context.TranslatedText,
                                (value) => ensureRow().Value = value,
                                () => attributeValue.Value,
                                (value) => attributeValue.Value = value,
                                () => ensureRow().Value
                            );
                        }
                        break;
                    }

                case 1:  /* Fixed List */
                case 4:  /* Variable List */
                    {
                        var xmlFormat = xmlRow.XPathSelectElement("v:Cell[@N='Format']", VisioParser.NamespaceManager);
                        if (xmlFormat != null)
                        {
                            var attributeFormat = xmlFormat.Attribute("V");
                            if (VisioParser.IsTextValue(attributeFormat?.Value))
                            {
                                PropertyInfo ensureRow() => DiagramInfoService.EnsureCollection(xmlRow, ensureProp);
                                ProcessValue(context.Direction, context.TranslatedPropertyFormats,
                                    (value) => ensureRow().Format = value,
                                    () => attributeFormat.Value,
                                    (value) => attributeFormat.Value = value,
                                    () => ensureRow().Format
                                );
                            }
                        }
                        break;
                    }
            }
        }
    }

    private static void ProcessPages(Package package, PackagePart documentPart, TranslationContext context)
    {
        var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
        Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
        var pagesPart = package.GetPart(pagesUri);

        var fileAccess = context.Direction == TranslationDirection.Get ? FileAccess.Read : FileAccess.ReadWrite;
        var pagesStream = pagesPart.GetStream(FileMode.Open, fileAccess);
        var xmlPages = XDocument.Load(pagesStream);

        var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
        foreach (var pageRel in pageRels)
        {
            var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);

            var pageInfo = DiagramInfoService.EnsureCollection(xmlPage, () => context.DocumentInfo.Pages ??= []);

            if (context.Options.EnableTranslatePageNames)
            {
                var attributeName = xmlPage.Attribute("Name");
                if (VisioParser.IsTextValue(attributeName?.Value))
                {
                    ProcessValue(context.Direction, context.TranslatedPageNames,
                        (value) => pageInfo.Name = value,
                        () => attributeName.Value,
                        (value) => attributeName.Value = value,
                        () => pageInfo.Name
                    );
                }
            }

            var pageSheet = xmlPage.XPathSelectElement("v:PageSheet", VisioParser.NamespaceManager);
            if (pageSheet != null)
            {
                if (context.Options.EnableTranslatePropertyValues)
                    TranslatePropertyValues(pageSheet, () => pageInfo.PropRows ??= [], context);

                if (context.Options.EnableTranslateUserRows)
                    ProcessUserRows(pageSheet, () => pageInfo.UserRows ??= [], context);
            }

            Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
            var pagePart = package.GetPart(pageUri);

            ProcessPage(pagePart, pageInfo, context);
        }

        if (context.Direction == TranslationDirection.Set)
            VisioParser.FlushStream(xmlPages, pagesStream);
    }

    private static void ProcessDocument(Stream stream, TranslationContext context)
    {
        var fileAccess = context.Direction == TranslationDirection.Get ? FileAccess.Read : FileAccess.ReadWrite;
        using (Package package = Package.Open(stream, FileMode.Open, fileAccess))
        {
            var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
            Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
            var documentPart = package.GetPart(docUri);

            var documentStream = documentPart.GetStream(FileMode.Open);
            var xmlDocument = XDocument.Load(documentStream);

            ProcessPages(package, documentPart, context);

            var documentSheet = xmlDocument.XPathSelectElement("/v:VisioDocument/v:DocumentSheet", VisioParser.NamespaceManager);
            if (documentSheet != null)
            {
                if (context.Options.EnableTranslatePropertyValues)
                    TranslatePropertyValues(documentSheet, () => context.DocumentInfo.PropRows ??= [], context);

                if (context.Options.EnableTranslatePropertyLabels)
                    TranslatePropertyLabels(documentSheet, () => context.DocumentInfo.PropRows ??= [], context);

                if (context.Options.EnableTranslateUserRows)
                    ProcessUserRows(documentSheet, () => context.DocumentInfo.UserRows ??= [], context);
            }

            if (context.Direction == TranslationDirection.Set)
            {
                VisioParser.FlushStream(xmlDocument, documentStream);
                package.Flush();
            }
        }
    }

    public static byte[] ApplyTranslationJson(byte[] input, TranslateOptions options, string json)
    {
        using (var stream = new MemoryStream(input))
        {
            var context = new TranslationContext
            {
                DocumentInfo = JsonSerializer.Deserialize(json, DocumentInfoJsonContext.Context.DocumentInfo),
                Options = options,
                Direction = TranslationDirection.Set
            };

            ProcessDocument(stream, context);

            stream.Flush();
            return stream.ToArray();
        }
    }

    public static string GetTranslationJson(byte[] input, TranslateOptions options)
    {
        using (var stream = new MemoryStream(input))
        {
            var context = new TranslationContext
            {
                DocumentInfo = new(),
                Options = options,
                Direction = TranslationDirection.Get
            };

            ProcessDocument(stream, context);

            var json = JsonSerializer.Serialize(context.DocumentInfo, DocumentInfoJsonContext.Context.DocumentInfo);
            return json;
        }
    }
}
