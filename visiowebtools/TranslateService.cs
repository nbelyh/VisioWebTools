using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace VisioWebTools
{
    public enum TranslationDirection
    {
        Get,
        Set
    }

    /// <summary>
    /// Service for generating random readable strings with specific constraints.
    /// </summary>
    public static class TranslateService
    {
        public static void ProcessPage(PackagePart pagePart, TranslateOptions options, PageInfo pageInfo, TranslationDirection direction)
        {
            var fileAccess = direction == TranslationDirection.Get ? FileAccess.Read : FileAccess.ReadWrite;
            var pageStream = pagePart.GetStream(FileMode.Open, fileAccess);
            var xmlPage = XDocument.Load(pageStream);

            var xmlShapes = xmlPage.XPathSelectElements("/v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
            foreach (var xmlShape in xmlShapes)
            {
                var shapeId = int.Parse(xmlShape.Attribute("ID").Value);

                pageInfo.Shapes ??= [];
                if (!pageInfo.Shapes.TryGetValue(shapeId, out var shapeInfo))
                    shapeInfo = new ShapeInfo();

                var initialized = false;
                if (options.EnableTranslateShapeText)
                    initialized = ProcessShapeText(xmlShape, shapeInfo, direction) || initialized;

                if (options.EnableTranslateShapeFields)
                    initialized = TranslateShapeFields(xmlShape, () => shapeInfo.FieldRows ??= [], direction) || initialized;

                if (options.EnableTranslatePropertyValues)
                    initialized = TranslatePropertyValues(xmlShape, () => shapeInfo.PropRows ??= [], direction) || initialized;

                if (options.EnableTranslatePropertyLabels)
                    initialized = TranslatePropertyLabels(xmlShape, () => shapeInfo.PropRows ?? [], direction) || initialized;

                if (options.EnableTranslateUserRows)
                    initialized = ProcessUserRows(xmlShape, () => pageInfo.UserRows ??= [], direction) || initialized;

                if (initialized && !pageInfo.Shapes.ContainsKey(shapeId))
                    pageInfo.Shapes.Add(shapeId, shapeInfo);
            }

            if (direction == TranslationDirection.Set)
            {
                pageStream.SetLength(0);
                using (var writer = new XmlTextWriter(pageStream, new UTF8Encoding(false)))
                {
                    xmlPage.Save(writer);
                }
            }
        }

        public static bool ShouldBeIgnored(string input)
        {
            return string.IsNullOrWhiteSpace(input) || Regex.IsMatch(input, @"^[\s\d\n\r\.]*$");
        }

        private static bool ProcessShapeText(XElement xmlShape, ShapeInfo shapeInfo, TranslationDirection direction)
        {
            var xmlText = xmlShape.XPathSelectElement("v:Text", VisioParser.NamespaceManager);
            if (xmlText == null)
                return false;

            var text = TranslateFormattedTextService.GetShapeText(xmlText);
            if (!ShouldBeIgnored(text?.PlainText))
            {
                switch (direction)
                {
                    case TranslationDirection.Get:
                        shapeInfo.Text = text.FormattedText;
                        return true;

                    case TranslationDirection.Set:
                        TranslateFormattedTextService.BuildXElements(xmlText, shapeInfo.Text);
                        return true;
                }
                return true;
            }

            return false;
        }

        private static bool ProcessUserRows(XElement xmlShape, Func<Dictionary<string, UserRowInfo>> getUserRows, TranslationDirection direction)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='User']/v:Row", VisioParser.NamespaceManager).ToList();
            var result = false;
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (!ShouldBeIgnored(attributeValue?.Value))
                {
                    var userRowInfo = DiagramInfoService.EnsureCollection(xmlRow, getUserRows);
                    switch (direction)
                    {
                        case TranslationDirection.Get:
                            userRowInfo.Value = attributeValue.Value;
                            result = true;
                            break;

                        case TranslationDirection.Set:
                            attributeValue.Value = userRowInfo.Value;
                            result = true;
                            break;
                    }
                }

                var xmlPrompt = xmlRow.XPathSelectElement("v:Cell[@N='Prompt']", VisioParser.NamespaceManager);
                var attributePrompt = xmlPrompt?.Attribute("V");
                if (!ShouldBeIgnored(attributePrompt?.Value))
                {
                    var userRowInfo = DiagramInfoService.EnsureCollection(xmlRow, getUserRows);
                    switch (direction)
                    {
                        case TranslationDirection.Get:
                            userRowInfo.Prompt = attributePrompt.Value;
                            result = true;
                            break;

                        case TranslationDirection.Set:
                            attributePrompt.Value = userRowInfo.Prompt;
                            result = true;
                            break;
                    }
                }
            }
            return result;
        }

        private static bool TranslateShapeFields(XElement xmlShape, Func<Dictionary<string, FieldInfo>> getShapeInfos, TranslationDirection direction)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Field']/v:Row", VisioParser.NamespaceManager).ToList();
            var result = false;
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value' and @U='STR']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (!ShouldBeIgnored(attributeValue?.Value))
                {
                    var fieldInfo = DiagramInfoService.EnsureCollection(xmlRow, getShapeInfos);
                    switch (direction)
                    {
                        case TranslationDirection.Get:
                            fieldInfo.Value = attributeValue.Value;
                            result = true;
                            break;

                        case TranslationDirection.Set:
                            attributeValue.Value = fieldInfo.Value;
                            result = true;
                            break;
                    }
                }
            }
            return result;
        }

        private static bool TranslatePropertyLabels(XElement xmlShape, Func<Dictionary<string, PropertyInfo>> getPropInfos, TranslationDirection direction)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
            var result = false;
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Label']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (!ShouldBeIgnored(attributeValue?.Value))
                {
                    var propertyInfo = DiagramInfoService.EnsureCollection(xmlRow, getPropInfos);
                    switch (direction)
                    {
                        case TranslationDirection.Get:
                            propertyInfo.Label = attributeValue.Value;
                            result = true;
                            break;

                        case TranslationDirection.Set:
                            attributeValue.Value = propertyInfo.Label;
                            result = true;
                            break;
                    }
                }
            }
            return result;
        }

        private static bool TranslatePropertyValues(XElement xmlShape, Func<Dictionary<string, PropertyInfo>> getPropInfos, TranslationDirection direction)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
            var result = false;
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
                            if (!ShouldBeIgnored(attributeValue?.Value))
                            {
                                var propertyInfo = DiagramInfoService.EnsureCollection(xmlRow, getPropInfos);
                                switch (direction)
                                {
                                    case TranslationDirection.Get:
                                        propertyInfo.Value = attributeValue.Value;
                                        result = true;
                                        break;

                                    case TranslationDirection.Set:
                                        attributeValue.Value = propertyInfo.Value;
                                        result = true;
                                        break;
                                }
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
                                if (!ShouldBeIgnored(attributeFormat?.Value))
                                {
                                    var propertyInfo = DiagramInfoService.EnsureCollection(xmlRow, getPropInfos);
                                    switch (direction)
                                    {
                                        case TranslationDirection.Get:
                                            propertyInfo.Format = attributeFormat.Value;
                                            result = true;
                                            break;

                                        case TranslationDirection.Set:
                                            attributeFormat.Value = propertyInfo.Format;
                                            result = true;
                                            break;
                                    }
                                }
                            }
                            break;
                        }
                }
            }
            return result;
        }

        public static void ProcessPages(Stream stream, TranslateOptions options, DocumentInfo documentInfo, TranslationDirection direction)
        {
            var fileAccess = direction == TranslationDirection.Get ? FileAccess.Read : FileAccess.ReadWrite;
            using (Package package = Package.Open(stream, FileMode.Open, fileAccess))
            {
                var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
                Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
                var documentPart = package.GetPart(docUri);

                var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
                Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
                var pagesPart = package.GetPart(pagesUri);

                var pagesStream = pagesPart.GetStream(FileMode.Open, fileAccess);
                var xmlPages = XDocument.Load(pagesStream);

                var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
                foreach (var pageRel in pageRels)
                {
                    var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);

                    var pageId = int.Parse(xmlPage.Attribute("ID").Value);
                    documentInfo.Pages ??= [];
                    if (!documentInfo.Pages.TryGetValue(pageId, out var pageInfo))
                    {
                        pageInfo = new PageInfo();
                        documentInfo.Pages.Add(pageId, pageInfo);
                    }

                    if (options.EnableTranslatePageNames)
                    {
                        var attributeName = xmlPage.Attribute("Name");

                        switch (direction)
                        {
                            case TranslationDirection.Get:
                                if (attributeName != null)
                                    pageInfo.Name = attributeName.Value;
                                break;

                            case TranslationDirection.Set:
                                if (attributeName != null)
                                    attributeName.Value = pageInfo.Name;
                                break;
                        }
                    }


                    Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
                    var pagePart = package.GetPart(pageUri);
                    ProcessPage(pagePart, options, pageInfo, direction);
                }

                if (direction == TranslationDirection.Set)
                {
                    pagesStream.SetLength(0);
                    using (var writer = new XmlTextWriter(pagesStream, new UTF8Encoding(false)))
                    {
                        xmlPages.Save(writer);
                    }
                    package.Flush();
                }
            }
        }

        public static byte[] ApplyTranslationJson(byte[] input, TranslateOptions options, string json)
        {
            using (var stream = new MemoryStream(input))
            {
                var documentInfo = JsonSerializer.Deserialize(json, DocumentInfoJsonContext.Context.DocumentInfo);

                ProcessPages(stream, options, documentInfo, TranslationDirection.Set);

                stream.Flush();
                return stream.ToArray();
            }
        }

        public static string GetTranslationJson(byte[] input, TranslateOptions options)
        {
            using (var stream = new MemoryStream(input))
            {
                var documentInfo = new DocumentInfo();
                ProcessPages(stream, options, documentInfo, TranslationDirection.Get);
                var json = JsonSerializer.Serialize(documentInfo, DocumentInfoJsonContext.Context.DocumentInfo);
                return json;
            }
        }
    }
}