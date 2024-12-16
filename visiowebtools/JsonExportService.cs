using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text.Json;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;

namespace VisioWebTools
{
    /// <summary>
    /// Service for generating random readable strings with specific constraints.
    /// </summary>
    public static class JsonExportService
    {
        public static void ProcessMasters(Package package, PackagePart documentPart, Func<Dictionary<string, MasterInfo>> getMasterInfos)
        {
            var mastersRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/masters").FirstOrDefault();
            if (mastersRel == null)
                return;

            Uri mastersUri = PackUriHelper.ResolvePartUri(documentPart.Uri, mastersRel.TargetUri);
            var mastersPart = package.GetPart(mastersUri);

            var mastersStream = mastersPart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            var xmlMasters = XDocument.Load(mastersStream);

            var masterRels = mastersPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/master").ToList();
            foreach (var masterRel in masterRels)
            {
                var xmlMaster = xmlMasters.XPathSelectElement($"/v:Masters/v:Master[v:Rel/@r:id='{masterRel.Id}']", VisioParser.NamespaceManager);
                var masterInfo = DiagramInfoService.EnsureCollection(xmlMaster, getMasterInfos);

                var attributeName = xmlMaster.Attribute("Name");
                masterInfo.Name = attributeName?.Value;

                var attributeNameU = xmlMaster.Attribute("NameU");
                masterInfo.NameU = attributeNameU?.Value;

                var xmlType = xmlMaster.XPathSelectElement("v:MasterType", VisioParser.NamespaceManager);
                masterInfo.MasterType = xmlType?.Value;
            }
        }

        public static void ProcessPage(PackagePart pagePart, JsonExportOptions options, Func<Dictionary<string, ShapeInfo>> getShapeInfo)
        {
            var pageStream = pagePart.GetStream(FileMode.Open);
            var xmlPage = XDocument.Load(pageStream);

            var xmlShapes = xmlPage.XPathSelectElements("/v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
            foreach (var xmlShape in xmlShapes)
            {
                var shapeInfo = DiagramInfoService.EnsureCollection(xmlShape, getShapeInfo);

                if (options.IncludeShapeText)
                    ProcessShapeText(xmlShape, shapeInfo);

                if (options.IncludeShapeFields)
                    GetShapeFields(xmlShape, () => shapeInfo.FieldRows ??= []);

                if (options.IncludePropertyRows)
                    GetPropertyRows(xmlShape, () => shapeInfo.PropRows ??= []);

                if (options.IncludeUserRows)
                    GetUserRows(xmlShape, () => shapeInfo.UserRows ??= []);
            }
        }

        private static void ProcessShapeText(XElement xmlShape, ShapeInfo shapeInfo)
        {
            var xmlText = xmlShape.XPathSelectElement("v:Text", VisioParser.NamespaceManager);
            if (xmlText == null)
                return;

            var text = FormattedTextService.GetShapeText(xmlText);
            shapeInfo.Text = text.PlainText;
        }

        private static void GetShapeFields(XElement xmlShape, Func<Dictionary<string, FieldInfo>> getShapeInfos)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Field']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var fieldInfo = DiagramInfoService.EnsureCollection(xmlRow, getShapeInfos);

                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value' and @U='STR']", VisioParser.NamespaceManager);
                fieldInfo.Value = xmlValue?.Attribute("V")?.Value;
            }
        }

        private static void GetUserRows(XElement xmlShape, Func<Dictionary<string, UserRowInfo>> getUserRows)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='User']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var userRowInfo = DiagramInfoService.EnsureCollection(xmlRow, getUserRows);

                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
                userRowInfo.Value = xmlValue?.Attribute("V")?.Value;

                var xmlPrompt = xmlRow.XPathSelectElement("v:Cell[@N='Prompt']", VisioParser.NamespaceManager);
                userRowInfo.Prompt = xmlPrompt?.Attribute("V")?.Value;
            }
        }

        private static void GetPropertyRows(XElement xmlShape, Func<Dictionary<string, PropertyInfo>> getShapeInfo)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var propertyInfo = DiagramInfoService.EnsureCollection(xmlRow, getShapeInfo);

                var xmlType = xmlRow.XPathSelectElement("v:Cell[@N='Type']", VisioParser.NamespaceManager);
                propertyInfo.Type = xmlType?.Attribute("V")?.Value;

                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
                propertyInfo.Value = xmlValue?.Attribute("V")?.Value;

                var xmlFormat = xmlRow.XPathSelectElement("v:Cell[@N='Format']", VisioParser.NamespaceManager);
                propertyInfo.Format = xmlFormat?.Attribute("V")?.Value;

                var xmlPrompt = xmlRow.XPathSelectElement("v:Cell[@N='Prompt']", VisioParser.NamespaceManager);
                propertyInfo.Prompt = xmlPrompt?.Attribute("V")?.Value;

                var xmlLabel = xmlRow.XPathSelectElement("v:Cell[@N='Label']", VisioParser.NamespaceManager);
                propertyInfo.Label = xmlLabel?.Attribute("V")?.Value;
            }
        }

        class DocPropItem
        {
            public string Name { get; set; }
            public Action<string> Assign { get; set; }
        }

        private static void GetDocPropItems(Package package, string ns, DocPropItem[] items)
        {
            var corePropsRel = package.GetRelationshipsByType(ns).First();
            Uri corePropsUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), corePropsRel.TargetUri);
            var corePropsPart = package.GetPart(corePropsUri);

            var pagesStream = corePropsPart.GetStream(FileMode.Open);
            var xmlCoreProps = XDocument.Load(pagesStream);

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var xmlItem = xmlCoreProps.XPathSelectElements(item.Name, VisioParser.NamespaceManager).FirstOrDefault();
                item.Assign(xmlItem?.Value);
            }
        }

        public static void GetDocProps(Package package, DocumentInfo documentInfo)
        {
            GetDocPropItems(package, "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties",
            [
                new DocPropItem { Name = "/cp:coreProperties/dc:creator", Assign = (string v) => documentInfo.Creator = v },
                new DocPropItem { Name = "/cp:coreProperties/dc:title", Assign = (string v) => documentInfo.Title = v },
                new DocPropItem { Name = "/cp:coreProperties/dc:subject", Assign = (string v) => documentInfo.Subject = v },
                new DocPropItem { Name = "/cp:coreProperties/dc:category", Assign = (string v) => documentInfo.Category = v },
                new DocPropItem { Name = "/cp:coreProperties/dc:keywords", Assign = (string v) => documentInfo.Keywords = v },
            ]);

            GetDocPropItems(package, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties",
            [
                new DocPropItem { Name = "/ep:Properties/ep:Manager", Assign = (string v) => documentInfo.Manager = v },
                new DocPropItem { Name = "/ep:Properties/ep:Company", Assign = (string v) => documentInfo.Company = v },
            ]);
        }

        public static void ProcessPages(Package package, PackagePart documentPart, JsonExportOptions options, Func<Dictionary<string, PageInfo>> getPages)
        {
            var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
            Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
            var pagesPart = package.GetPart(pagesUri);

            var pagesStream = pagesPart.GetStream(FileMode.Open);
            var xmlPages = XDocument.Load(pagesStream);

            var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
            foreach (var pageRel in pageRels)
            {
                var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);

                var pageInfo = DiagramInfoService.EnsureCollection(xmlPage, getPages);

                if (options.IncludeUserRows)
                {
                    var pageSheet = xmlPage.XPathSelectElement("/v:PageSheet", VisioParser.NamespaceManager);
                    GetUserRows(xmlPage, () => pageInfo.UserRows ??= []);
                }

                var attributeName = xmlPage.Attribute("Name");
                pageInfo.Name = attributeName?.Value;

                var attributeNameU = xmlPage.Attribute("NameU");
                pageInfo.NameU = attributeNameU?.Value;

                Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
                var pagePart = package.GetPart(pageUri);
                ProcessPage(pagePart, options, () => pageInfo.Shapes ??= []);
            }
        }

        public static DocumentInfo ProcessDocument(Stream stream, JsonExportOptions options)
        {
            using (Package package = Package.Open(stream, FileMode.Open))
            {
                var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").FirstOrDefault();
                if (documentRel == null)
                    return null;

                Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
                var documentPart = package.GetPart(docUri);

                var documentInfo = new DocumentInfo();

                if (options.IncludeDocumentProperties)
                    GetDocProps(package, documentInfo);

                if (options.IncludeMasters)
                    ProcessMasters(package, documentPart, () => documentInfo.Masters ??= []);

                ProcessPages(package, documentPart, options, () => documentInfo.Pages ??= []);
                return documentInfo;
            }
        }

        public static string Process(byte[] input, JsonExportOptions options)
        {
            using (var stream = new MemoryStream(input))
            {
                DocumentInfo documentInfo = ProcessDocument(stream, options);
                var json = JsonSerializer.Serialize(documentInfo, DocumentInfoJsonContext.Context.DocumentInfo);
                return json;
            }
        }
    }
}