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
                ShapeInfo getShapeInfo() => DiagramInfoService.EnsureCollection(xmlShape, () => pageInfo.Shapes ??= []);

                if (options.EnableTranslateShapeText)
                    ProcessShapeText(xmlShape, getShapeInfo, direction);

                if (options.EnableTranslateShapeFields)
                    TranslateShapeFields(xmlShape, () => getShapeInfo().FieldRows ??= [], direction);

                if (options.EnableTranslatePropertyValues)
                    TranslatePropertyValues(xmlShape, () => getShapeInfo().PropRows ??= [], direction);

                if (options.EnableTranslatePropertyLabels)
                    TranslatePropertyLabels(xmlShape, () => getShapeInfo().PropRows ?? [], direction);

                if (options.EnableTranslateUserRows)
                    ProcessUserRows(xmlShape, () => pageInfo.UserRows ??= [], direction);
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

        private static void ProcessShapeText(XElement xmlShape, Func<ShapeInfo> getShapeInfo, TranslationDirection direction)
        {
            var xmlText = xmlShape.XPathSelectElement("v:Text", VisioParser.NamespaceManager);
            if (xmlText == null)
                return;

            var text = FormattedTextService.GetShapeText(xmlText);
            if (!ShouldBeIgnored(text?.PlainText))
            {
                var shapeInfo = getShapeInfo();
                switch (direction)
                {
                    case TranslationDirection.Get:
                        shapeInfo.Text = text.FormattedText;
                        break;

                    case TranslationDirection.Set:
                        FormattedTextService.BuildXElements(xmlText, shapeInfo.Text);
                        break;
                }
            }
        }

        private static void ProcessUserRows(XElement xmlShape, Func<Dictionary<string, UserRowInfo>> getUserRows, TranslationDirection direction)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='User']/v:Row", VisioParser.NamespaceManager).ToList();
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
                            break;

                        case TranslationDirection.Set:
                            attributeValue.Value = userRowInfo.Value;
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
                            break;

                        case TranslationDirection.Set:
                            attributePrompt.Value = userRowInfo.Prompt;
                            break;
                    }
                }
            }
        }

        private static void TranslateShapeFields(XElement xmlShape, Func<Dictionary<string, FieldInfo>> getShapeInfos, TranslationDirection direction)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Field']/v:Row", VisioParser.NamespaceManager).ToList();
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
                            break;

                        case TranslationDirection.Set:
                            attributeValue.Value = fieldInfo.Value;
                            break;
                    }
                }
            }
        }

        private static void TranslatePropertyLabels(XElement xmlShape, Func<Dictionary<string, PropertyInfo>> getPropInfos, TranslationDirection direction)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
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
                            break;

                        case TranslationDirection.Set:
                            attributeValue.Value = propertyInfo.Label;
                            break;
                    }
                }
            }
        }

        private static void TranslatePropertyValues(XElement xmlShape, Func<Dictionary<string, PropertyInfo>> getPropInfos, TranslationDirection direction)
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
                            if (!ShouldBeIgnored(attributeValue?.Value))
                            {
                                var propertyInfo = DiagramInfoService.EnsureCollection(xmlRow, getPropInfos);
                                switch (direction)
                                {
                                    case TranslationDirection.Get:
                                        propertyInfo.Value = attributeValue.Value;
                                        break;

                                    case TranslationDirection.Set:
                                        attributeValue.Value = propertyInfo.Value;
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
                                            break;

                                        case TranslationDirection.Set:
                                            attributeFormat.Value = propertyInfo.Format;
                                            break;
                                    }
                                }
                            }
                            break;
                        }
                }
            }
        }

        public static void ProcessPages(Package package, PackagePart documentPart, TranslateOptions options, Func<Dictionary<string, PageInfo>> getPagesInfo, TranslationDirection direction)
        {
            var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
            Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
            var pagesPart = package.GetPart(pagesUri);

            var fileAccess = direction == TranslationDirection.Get ? FileAccess.Read : FileAccess.ReadWrite;
            var pagesStream = pagesPart.GetStream(FileMode.Open, fileAccess);
            var xmlPages = XDocument.Load(pagesStream);

            var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
            foreach (var pageRel in pageRels)
            {
                var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);

                var pageInfo = DiagramInfoService.EnsureCollection(xmlPage, getPagesInfo);

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

                if (direction == TranslationDirection.Set)
                {
                    pagesStream.SetLength(0);
                    using (var writer = new XmlTextWriter(pagesStream, new UTF8Encoding(false)))
                    {
                        xmlPages.Save(writer);
                    }
                }
            }
        }

        public static void ProcessDocument(Stream stream, TranslateOptions options, DocumentInfo documentInfo, TranslationDirection direction)
        {
             var fileAccess = direction == TranslationDirection.Get ? FileAccess.Read : FileAccess.ReadWrite;
            using (Package package = Package.Open(stream, FileMode.Open, fileAccess))
            {
                var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
                Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
                var documentPart = package.GetPart(docUri);

                ProcessPages(package, documentPart, options, () => documentInfo.Pages ??= [], direction);

                if (direction == TranslationDirection.Set)
                    package.Flush();
            }
       }

        public static byte[] ApplyTranslationJson(byte[] input, TranslateOptions options, string json)
        {
            using (var stream = new MemoryStream(input))
            {
                var documentInfo = JsonSerializer.Deserialize(json, DocumentInfoJsonContext.Context.DocumentInfo);

                ProcessDocument(stream, options, documentInfo, TranslationDirection.Set);

                stream.Flush();
                return stream.ToArray();
            }
        }

        public static string GetTranslationJson(byte[] input, TranslateOptions options)
        {
            using (var stream = new MemoryStream(input))
            {
                var documentInfo = new DocumentInfo();

                ProcessDocument(stream, options, documentInfo, TranslationDirection.Get);
                
                var json = JsonSerializer.Serialize(documentInfo, DocumentInfoJsonContext.Context.DocumentInfo);
                return json;
            }
        }
    }
}