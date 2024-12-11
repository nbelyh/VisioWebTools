using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using SixLabors.ImageSharp;

namespace VisioWebTools
{
    public class CipherFileService
    {
        static readonly RandomStringService randomStringService = new();

        public static void ProcessPage(PackagePart pagePart, CipherOptions options)
        {
            var pageStream = pagePart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            var xmlPage = XDocument.Load(pageStream);

            var xmlShapes = xmlPage.XPathSelectElements("/v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
            foreach (var xmlShape in xmlShapes)
            {
                if (options.EnableCipherShapeText)
                    CipherShapeText(xmlShape);

                if (options.EnableCipherShapeFields)
                    CipherShapeFields(xmlShape);

                if (options.EnableCipherPropertyValues)
                    CipherPropertyValues(xmlShape);

                if (options.EnableCipherPropertyLabels)
                    CipherPropertyLabels(xmlShape);
            }

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
                var xmlValue = xmlRow.XPathSelectElement("v:Cell[@N='Value' and @U='STR']", VisioParser.NamespaceManager);
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
                            {
                                attributeValue.Value = randomStringService.GenerateReadableRandomString(attributeValue.Value);
                            }
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

        public static void ProcessPages(Stream stream, CipherOptions options)
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

                pagesStream.SetLength(0);
                using (var writer = new XmlTextWriter(pagesStream, new UTF8Encoding(false)))
                {
                    xmlPages.Save(writer);
                }
                package.Flush();
            }
        }

        public static byte[] Process(byte[] input, CipherOptions options)
        {
            using (var stream = new MemoryStream(input))
            {
                ProcessPages(stream, options);
                stream.Flush();
                return stream.ToArray();
            }
        }
    }
}