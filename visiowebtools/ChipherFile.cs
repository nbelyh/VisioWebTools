using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace VisioWebTools
{
    public class ChipherOptions
    {
        public bool EnableChipherShapeText { get; set; }
        public bool EnableChipherPageNames { get; set; }
        public bool EnableChipherShapeData { get; set; }
    }

    public class ChipherFileService
    {
        static readonly RandomStringService randomStringService = new();

        public static void ProcessPage(PackagePart pagePart, ChipherOptions options)
        {
            var pageStream = pagePart.GetStream(FileMode.Open, FileAccess.ReadWrite);
            var xmlPage = XDocument.Load(pageStream);

            var xmlShapes = xmlPage.XPathSelectElements("/v:PageContents//v:Shape", VisioParser.NamespaceManager).ToList();
            foreach (var xmlShape in xmlShapes)
            {
                if (options.EnableChipherShapeText)
                {
                    var xmlText = xmlShape.XPathSelectElements("v:Text", VisioParser.NamespaceManager).ToList();
                    foreach (var node in xmlText.Nodes())
                    {
                        if (node is XText text)
                            text.Value = randomStringService.GenerateReadableRandomString(text.Value);
                    }
                }
            }
            pageStream.SetLength(0);
            using (var writer = new XmlTextWriter(pageStream, new UTF8Encoding(false)))
            {
                xmlPage.Save(writer);
            }
        }

        public static void ProcessPages(Stream stream, ChipherOptions options)
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
                    if (options.EnableChipherPageNames)
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

        public static byte[] Process(byte[] input, ChipherOptions options)
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