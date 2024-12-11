using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using SixLabors.ImageSharp;

namespace VisioWebTools
{
    class FieldInfo 
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    class PropertyInfo 
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
    }

    class ShapeInfo 
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public List<FieldInfo> FieldInfos { get; set; }
        public List<PropertyInfo> PropertyInfos { get; set; }
    }

    class PageInfo 
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameU { get; set; }
        public List<ShapeInfo> Shapes { get; set; }
    }

    class DocumentInfo 
    {
        public List<PageInfo> Pages { get; set; }
    }

    /// <summary>
    /// Service for generating random readable strings with specific constraints.
    /// </summary>
    public class TranslateFileService
    {
        static readonly TranslateService translateService = new();

        // public static void ProcessPage(PackagePart pagePart, TranslateOptions options, PageInfo pageInfo)
        // {
        //     var pageStream = pagePart.GetStream(FileMode.Open, FileAccess.ReadWrite);
        //     var xmlPage = XDocument.Load(pageStream);

        //     var xmlShapes = xmlPage.XPathSelectElements("/v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
        //     var shapes = new List<ShapeInfo>();
        //     foreach (var xmlShape in xmlShapes)
        //     {
        //         ShapeInfo shapeInfo = new();

        //         if (options.EnableTranslateShapeText)
        //             ProcessShapeText(xmlShape, shapeInfo);

        //         if (options.EnableTranslateShapeFields)
        //             TranslateShapeFields(xmlShape, shapeInfo);

        //         if (options.EnableTranslatePropertyValues)
        //             TranslatePropertyValues(xmlShape, shapeInfo);

        //         if (options.EnableTranslatePropertyLabels)
        //             TranslatePropertyLabels(xmlShape, shapeInfo);

        //         shapes.Add(shapeInfo);
        //     }

        //     pageInfo.Shapes = shapes;

        //     pageStream.SetLength(0);
        //     using (var writer = new XmlTextWriter(pageStream, new UTF8Encoding(false)))
        //     {
        //         xmlPage.Save(writer);
        //     }
        // }

        // private static void ProcessShapeText(XElement xmlShape, ShapeInfo shapeInfo, bool update)
        // {
        //     var xmlText = xmlShape.XPathSelectElements("v:Text", VisioParser.NamespaceManager).ToList();
        //     foreach (var node in xmlText.Nodes())
        //     {
        //         if (node is XText text)
        //         {
        //             if (update)
        //                 text.Value = shapeInfo.Text;
        //             else
        //                 shapeInfo.Text = text.Value;
        //         }
        //     }
        // }

        // private static void ProcessShapeFields(XElement xmlShape, ShapeInfo shapeInfo, bool update)
        // {
        //     var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Field']/v:Row", VisioParser.NamespaceManager).ToList();
        //     foreach (var xmlRow in xmlRows)
        //     {
        //         var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value' and @U='STR']", VisioParser.NamespaceManager);
        //         var attributeValue = xmlValue?.Attribute("V");
        //         if (attributeValue != null)
        //         {
        //             if (update)
        //                 attributeValue.Value = shapeInfo.FieldInfos.First(x => x.Name == attributeValue.Value).Value;
        //             else
        //                 shapeInfo.FieldInfos.Add(new FieldInfo { Name = attributeValue.Value, Value = translateService.GenerateReadableRandomString(attributeValue.Value) });
        //         }
        //             attributeValue.Value = translateService.GenerateReadableRandomString(attributeValue.Value);
        //     }
        // }

        // private static void TranslatePropertyLabels(XElement xmlShape)
        // {
        //     var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
        //     foreach (var xmlRow in xmlRows)
        //     {
        //         var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Label']", VisioParser.NamespaceManager);
        //         var attributeValue = xmlValue?.Attribute("V");
        //         if (attributeValue != null)
        //         {
        //             attributeValue.Value = translateService.GenerateReadableRandomString(attributeValue.Value);
        //         }
        //     }
        // }

        // private static void TranslatePropertyValues(XElement xmlShape)
        // {
        //     var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
        //     foreach (var xmlRow in xmlRows)
        //     {
        //         var xmlType = xmlRow.XPathSelectElement("v:Cell[@N='Type']", VisioParser.NamespaceManager);
        //         var typeValue = xmlType?.Attribute("V")?.Value ?? "0";
        //         if (!int.TryParse(typeValue, out int type))
        //             type = 0;

        //         switch (type)
        //         {
        //             case 0:  /* String */
        //                 {
        //                     var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
        //                     var attributeValue = xmlValue?.Attribute("V");
        //                     if (attributeValue != null)
        //                     {
        //                         attributeValue.Value = translateService.GenerateReadableRandomString(attributeValue.Value);
        //                     }
        //                     break;
        //                 }

        //             case 1:  /* Fixed List */
        //             case 4:  /* Variable List */
        //                 {
        //                     var xmlFormat = xmlRow.XPathSelectElement("v:Cell[@N='Format']", VisioParser.NamespaceManager);
        //                     if (xmlFormat != null)
        //                     {
        //                         var attributeFormat = xmlFormat.Attribute("V")?.Value;
        //                         if (!string.IsNullOrEmpty(attributeFormat))
        //                         {
        //                             var items = attributeFormat.Split(';');
        //                             if (items.Length > 0)
        //                             {
        //                                 var newItems = items.Select(x => translateService.GenerateReadableRandomString(x)).ToArray();
        //                                 xmlFormat.Attribute("V").Value = string.Join(";", newItems);
        //                             }
        //                         }
        //                     }
        //                     break;
        //                 }
        //         }
        //     }
        // }

        // public static void ProcessPages(Stream stream, TranslateOptions options)
        // {
        //     using (Package package = Package.Open(stream, FileMode.Open, FileAccess.ReadWrite))
        //     {
        //         var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
        //         Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
        //         var documentPart = package.GetPart(docUri);

        //         var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
        //         Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
        //         var pagesPart = package.GetPart(pagesUri);

        //         var pagesStream = pagesPart.GetStream(FileMode.Open, FileAccess.ReadWrite);
        //         var xmlPages = XDocument.Load(pagesStream);

        //         var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
        //         foreach (var pageRel in pageRels)
        //         {
        //             var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);
        //             if (options.EnableTranslatePageNames)
        //             {
        //                 var attributeName = xmlPage.Attribute("Name");
        //                 if (attributeName != null)
        //                     attributeName.Value = translateService.GenerateReadableRandomString(attributeName.Value);
        //                 var attributeNameU = xmlPage.Attribute("NameU");
        //                 if (attributeNameU != null)
        //                     attributeNameU.Value = translateService.GenerateReadableRandomString(attributeNameU.Value);
        //             }

        //             Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
        //             var pagePart = package.GetPart(pageUri);

        //             var pageId = xmlPage.Attribute("ID").Value;
        //             ProcessPage(pagePart, pageId, options);
        //         }

        //         pagesStream.SetLength(0);
        //         using (var writer = new XmlTextWriter(pagesStream, new UTF8Encoding(false)))
        //         {
        //             xmlPages.Save(writer);
        //         }
        //         package.Flush();
        //     }
        // }

        public static byte[] Process(byte[] input, TranslateOptions options)
        {
            return input;
            // using (var stream = new MemoryStream(input))
            // {
            //     ProcessPages(stream, options);
            //     stream.Flush();
            //     return stream.ToArray();
            // }
        }
    }
}