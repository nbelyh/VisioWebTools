using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Xml.Linq;
using System.Xml.XPath;

namespace VisioWebTools
{
    /// <summary>
    /// Service for generating random readable strings with specific constraints.
    /// </summary>
    public static class TranslateFileService
    {
        public static void ProcessPage(PackagePart pagePart, TranslateOptions options, PageInfo pageInfo)
        {
            var pageStream = pagePart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            var xmlPage = XDocument.Load(pageStream);

            var xmlShapes = xmlPage.XPathSelectElements("/v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
            foreach (var xmlShape in xmlShapes)
            {
                var shapeId = int.Parse(xmlShape.Attribute("ID").Value);

                pageInfo.Shapes ??= [];
                if (!pageInfo.Shapes.TryGetValue(shapeId, out var shapeInfo))
                {
                    shapeInfo = new ShapeInfo();
                }

                var initialized = false;
                if (options.EnableTranslateShapeText)
                    initialized = ProcessShapeText(xmlShape, shapeInfo) || initialized;

                if (options.EnableTranslateShapeFields)
                    initialized = TranslateShapeFields(xmlShape, shapeInfo) || initialized;

                if (options.EnableTranslatePropertyValues)
                    initialized = TranslatePropertyValues(xmlShape, shapeInfo) || initialized;

                if (options.EnableTranslatePropertyLabels)
                    initialized = TranslatePropertyLabels(xmlShape, shapeInfo) || initialized;

                if (initialized && !pageInfo.Shapes.ContainsKey(shapeId))
                    pageInfo.Shapes.Add(shapeId, shapeInfo);
            }

            // pageStream.SetLength(0);
            // using (var writer = new XmlTextWriter(pageStream, new UTF8Encoding(false)))
            // {
            //     xmlPage.Save(writer);
            // }
        }

        public static bool ShouldBeIgnored(string input)
        {
            return Regex.IsMatch(input, @"^[\s\d\n\r]*$");
        }

        private static bool ProcessShapeText(XElement xmlShape, ShapeInfo shapeInfo)
        {
            var xmlText = xmlShape.XPathSelectElement("v:Text", VisioParser.NamespaceManager);
            if (xmlText == null)
                return false;

            var text = TranslateService.GetShapeText(xmlText);
            if (!ShouldBeIgnored(text.PlainText))
            {
                shapeInfo.Text = text.FormattedText;
                return true;
            }

            return false;
        }

        private static FieldInfo EnsureFieldInfo(ShapeInfo shapeInfo, XElement xmlRow)
        {
            var rowName = xmlRow.Attribute("N")?.Value;
            shapeInfo.FieldInfos ??= [];
            if (!shapeInfo.FieldInfos.TryGetValue(rowName, out var fieldInfo))
            {
                fieldInfo = new FieldInfo();
                shapeInfo.FieldInfos.Add(rowName, fieldInfo);
            }

            return fieldInfo;
        }

        private static bool TranslateShapeFields(XElement xmlShape, ShapeInfo shapeInfo)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Field']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value' and @U='STR']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (attributeValue != null && !ShouldBeIgnored(attributeValue.Value))
                {
                    var fieldInfo = EnsureFieldInfo(shapeInfo, xmlRow);
                    fieldInfo.Value = attributeValue.Value;
                    return true;
                    // attributeValue.Value = translateService.GenerateReadableRandomString(attributeValue.Value);
                }
            }
            return false;
        }

        private static PropertyInfo EnsurePropertyInfo(ShapeInfo shapeInfo, XElement xmlRow)
        {
            var rowName = xmlRow.Attribute("N")?.Value;
            shapeInfo.PropertyInfos ??= [];
            if (!shapeInfo.PropertyInfos.TryGetValue(rowName, out var propertyInfo))
            {
                propertyInfo = new PropertyInfo();
                shapeInfo.PropertyInfos.Add(rowName, propertyInfo);
            }

            return propertyInfo;
        }

        private static bool TranslatePropertyLabels(XElement xmlShape, ShapeInfo shapeInfo)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Label']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (attributeValue != null && !ShouldBeIgnored(attributeValue.Value))
                {
                    var propertyInfo = EnsurePropertyInfo(shapeInfo, xmlRow);
                    propertyInfo.Label = attributeValue.Value;
                    return true;
                    // attributeValue.Value = translateService.GenerateReadableRandomString(attributeValue.Value);
                }
            }
            return false;
        }

        private static bool TranslatePropertyValues(XElement xmlShape, ShapeInfo shapeInfo)
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
                            if (attributeValue != null && !ShouldBeIgnored(attributeValue.Value))
                            {
                                var propertyInfo = EnsurePropertyInfo(shapeInfo, xmlRow);
                                propertyInfo.Value = attributeValue.Value;
                                return true;
                                // 
                                // attributeValue.Value = translateService.GenerateReadableRandomString(attributeValue.Value);
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
                                if (attributeFormat != null && !ShouldBeIgnored(attributeFormat.Value))
                                {
                                    var propertyInfo = EnsurePropertyInfo(shapeInfo, xmlRow);
                                    propertyInfo.Format = attributeFormat?.Value;
                                    return true;
                                    // var items = attributeFormat.Split(';');
                                    // if (items.Length > 0)
                                    // {
                                    //     var newItems = items.Select(x => translateService.GenerateReadableRandomString(x)).ToArray();
                                    //     xmlFormat.Attribute("V").Value = string.Join(";", newItems);
                                    // }
                                }
                            }
                            break;
                        }
                }
            }
            return false;
        }

        public static void ProcessPages(Stream stream, TranslateOptions options, DocumentInfo documentInfo)
        {
            using (Package package = Package.Open(stream, FileMode.Open, FileAccess.ReadWrite))
            {
                var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
                Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
                var documentPart = package.GetPart(docUri);

                var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
                Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
                var pagesPart = package.GetPart(pagesUri);

                var pagesStream = pagesPart.GetStream(FileMode.Open, FileAccess.ReadWrite);
                var xmlPages = XDocument.Load(pagesStream);

                documentInfo.Pages = [];
                var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
                foreach (var pageRel in pageRels)
                {
                    var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);
                    var attributeName = xmlPage.Attribute("Name");
                    var attributeNameU = xmlPage.Attribute("NameU");

                    if (options.EnableTranslatePageNames)
                    {
                        // if (attributeName != null)
                        //     attributeName.Value = translateService.GenerateReadableRandomString(attributeName.Value);
                        // var attributeNameU = xmlPage.Attribute("NameU");
                        // if (attributeNameU != null)
                        //     attributeNameU.Value = translateService.GenerateReadableRandomString(attributeNameU.Value);
                    }

                    var pageId = int.Parse(xmlPage.Attribute("ID").Value);
                    var pageInfo = new PageInfo
                    {
                        Name = attributeName?.Value,
                        NameU = attributeNameU?.Value,
                    };

                    documentInfo.Pages.Add(pageId, pageInfo);

                    Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
                    var pagePart = package.GetPart(pageUri);
                    ProcessPage(pagePart, options, pageInfo);
                }

                // pagesStream.SetLength(0);
                // using (var writer = new XmlTextWriter(pagesStream, new UTF8Encoding(false)))
                // {
                //     xmlPages.Save(writer);
                // }
                // package.Flush();
            }
        }

        public static byte[] Process(byte[] input, TranslateOptions options)
        {
            using (var stream = new MemoryStream(input))
            {
                var documentInfo = new DocumentInfo();
                ProcessPages(stream, options, documentInfo);

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true, 
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                };

                var context = new DocumentInfoJsonContext(jsonOptions);
                var json = JsonSerializer.Serialize(documentInfo, context.DocumentInfo);
                return Encoding.UTF8.GetBytes(json);
                // stream.Flush();
                // return stream.ToArray();
            }
        }
    }
}