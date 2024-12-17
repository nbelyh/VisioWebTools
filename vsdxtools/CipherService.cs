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

namespace VsdxTools
{
    public class CipherService
    {
        static readonly RandomStringService randomStringService = new();

        private static void ProcessShapes(List<XElement> xmlShapes, CipherOptions options)
        {
            foreach (var xmlShape in xmlShapes)
            {
                if (options.EnableCipherShapeText)
                    CipherShapeText(xmlShape);

                if (options.EnableCipherShapeFields)
                    CipherShapeFields(xmlShape);

                if (options.EnableCipherUserRows)
                    CipherUserRows(xmlShape);

                if (options.EnableCipherPropertyValues)
                    CipherPropertyValues(xmlShape);

                if (options.EnableCipherPropertyLabels)
                    CipherPropertyLabels(xmlShape);
            }
        }

        private static void ProcessPage(PackagePart pagePart, CipherOptions options)
        {
            var pageStream = pagePart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            var xmlPage = XDocument.Load(pageStream);

            var xmlShapes = xmlPage.XPathSelectElements("v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
            ProcessShapes(xmlShapes, options);

            pageStream.SetLength(0);
            using (var writer = new XmlTextWriter(pageStream, new UTF8Encoding(false)))
            {
                xmlPage.Save(writer);
            }
        }

        private static void CipherShapeText(XElement xmlShape)
        {
            var xmlText = xmlShape.XPathSelectElements("v:Text", VisioParser.NamespaceManager).ToList();
            foreach (var node in xmlText.Nodes())
            {
                if (node is XText text)
                    text.Value = randomStringService.GenerateReadableRandomString(text.Value);
            }
        }

        private static void CipherShapeFields(XElement xmlShape)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Field']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (attributeValue != null)
                    attributeValue.Value = randomStringService.GenerateReadableRandomString(attributeValue.Value);
            }
        }

        private static void CipherUserRows(XElement xmlShape)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='User']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (attributeValue != null)
                    attributeValue.Value = randomStringService.GenerateReadableRandomString(attributeValue.Value);
            }
        }

        private static void CipherPropertyLabels(XElement xmlShape)
        {
            var xmlRows = xmlShape.XPathSelectElements("v:Section[@N='Property']/v:Row", VisioParser.NamespaceManager).ToList();
            foreach (var xmlRow in xmlRows)
            {
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Label']", VisioParser.NamespaceManager);
                var attributeValue = xmlValue?.Attribute("V");
                if (attributeValue != null)
                {
                    attributeValue.Value = randomStringService.GenerateReadableRandomString(attributeValue.Value);
                }
            }
        }

        private static void CipherPropertyValues(XElement xmlShape)
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
                            if (attributeValue != null)
                                attributeValue.Value = randomStringService.GenerateReadableRandomString(attributeValue.Value);
                            break;
                        }

                    case 1:  /* Fixed List */
                    case 4:  /* Variable List */
                        {
                            var xmlFormat = xmlRow.XPathSelectElement("v:Cell[@N='Format']", VisioParser.NamespaceManager);
                            if (xmlFormat != null)
                            {
                                var attributeFormat = xmlFormat.Attribute("V")?.Value;
                                if (!string.IsNullOrEmpty(attributeFormat))
                                {
                                    var items = attributeFormat.Split(';');
                                    if (items.Length > 0)
                                    {
                                        var newItems = items.Select(x => randomStringService.GenerateReadableRandomString(x)).ToArray();
                                        xmlFormat.Attribute("V").Value = string.Join(";", newItems);
                                    }
                                }
                            }
                            break;
                        }
                }
            }
        }

        private static void ProcessPages(Package package, PackagePart documentPart, CipherOptions options)
        {
            var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").FirstOrDefault();
            if (pagesRel == null)
                return;

            Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
            var pagesPart = package.GetPart(pagesUri);

            var pagesStream = pagesPart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            var xmlPages = XDocument.Load(pagesStream);

            var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
            foreach (var pageRel in pageRels)
            {
                if (options.EnableCipherPageNames)
                {
                    var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);

                    var attributeName = xmlPage.Attribute("Name");
                    if (attributeName != null)
                        attributeName.Value = randomStringService.GenerateReadableRandomString(attributeName.Value);

                    var attributeNameU = xmlPage.Attribute("NameU");
                    if (attributeNameU != null)
                        attributeNameU.Value = randomStringService.GenerateReadableRandomString(attributeNameU.Value);
                }

                Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
                var pagePart = package.GetPart(pageUri);

                ProcessPage(pagePart, options);
            }

            VisioParser.FlushStream(xmlPages, pagesStream);
            package.Flush();
        }

        private static void ProcessMaster(PackagePart masterPart, CipherOptions options)
        {
            var masterStream = masterPart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            var xmlMaster = XDocument.Load(masterStream);

            var xmlShapes = xmlMaster.XPathSelectElements("/v:MasterContents//v:Shape", VisioParser.NamespaceManager).ToList();
            ProcessShapes(xmlShapes, options);

            VisioParser.FlushStream(xmlMaster, masterStream);
        }

        private static void ProcessMasters(Package package, PackagePart documentPart, CipherOptions options)
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
                Uri masterUri = PackUriHelper.ResolvePartUri(mastersPart.Uri, masterRel.TargetUri);
                var masterPart = package.GetPart(masterUri);
                ProcessMaster(masterPart, options);
            }

            VisioParser.FlushStream(xmlMasters, mastersStream);
            package.Flush();
        }

        private static void CipherNode(XDocument xmlDoc, string path)
        {
            var xmlNode = xmlDoc.XPathSelectElements(path, VisioParser.NamespaceManager).FirstOrDefault();
            if (xmlNode != null)
                xmlNode.Value = randomStringService.GenerateReadableRandomString(xmlNode.Value);
        }

        private static void ProcessDocProps(Package package, string ns, string[] items)
        {
            var corePropsRel = package.GetRelationshipsByType(ns).FirstOrDefault();
            if (corePropsRel == null)
                return;

            Uri corePropsUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), corePropsRel.TargetUri);
            var corePropsPart = package.GetPart(corePropsUri);

            var docPropsStream = corePropsPart.GetStream(FileMode.Open);
            var xmlCoreProps = XDocument.Load(docPropsStream);

            for (int i = 0; i < items.Length; i++)
                CipherNode(xmlCoreProps, items[i]);

            VisioParser.FlushStream(xmlCoreProps, docPropsStream);
            package.Flush();
        }

        private static void ProcessDocument(Stream stream, CipherOptions options)
        {
            using (Package package = Package.Open(stream, FileMode.Open, FileAccess.ReadWrite))
            {
                var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").FirstOrDefault();
                if (documentRel == null)
                    return;

                Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
                var documentPart = package.GetPart(docUri);

                ProcessPages(package, documentPart, options);

                if (options.EnableCipherMasters)
                    ProcessMasters(package, documentPart, options);

                if (options.EnableCipherDocumentProperties)
                {
                    ProcessDocProps(package, "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties",
                    [
                        "/cp:coreProperties/dc:creator",
                        "/cp:coreProperties/dc:title",
                        "/cp:coreProperties/dc:subject",
                        "/cp:coreProperties/dc:category",
                        "/cp:coreProperties/dc:keywords",
                    ]);

                    ProcessDocProps(package, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties",
                    [
                        "/ep:Properties/ep:Manager",
                        "/ep:Properties/ep:Company",
                    ]);
                }
            }
        }

        public static byte[] Process(byte[] input, CipherOptions options)
        {
            using (var stream = new MemoryStream(input))
            {
                ProcessDocument(stream, options);
                stream.Flush();
                return stream.ToArray();
            }
        }
    }
}