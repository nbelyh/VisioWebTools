using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO.Compression;

namespace VsdxTools
{

    public class ExtractMediaService
    {
        public static byte[] ExtractMedia(Stream stream)
        {
            using (var output = new MemoryStream())
            {
                using (var zip = new ZipArchive(output, ZipArchiveMode.Create))
                {
                    XNamespace nsRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

                    using (Package package = Package.Open(stream))
                    {
                        var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
                        Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
                        var documentPart = package.GetPart(docUri);

                        var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
                        Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
                        var pagesPart = package.GetPart(pagesUri);

                        var xmlPages = VisioParser.GetXMLFromPart(pagesPart);
                        var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
                        foreach (var pageRel in pageRels)
                        {
                            Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
                            var pagePart = package.GetPart(pageUri);

                            var imageRels = pagePart.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/image").ToList();

                            var xmlPage = VisioParser.GetXMLFromPart(pagePart);

                            var xmlShapes = xmlPage.XPathSelectElements("v:PageContents//v:Shape[@Type='Foreign']", VisioParser.NamespaceManager).ToList();
                            foreach (var xmlShape in xmlShapes)
                            {
                                var pageId = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager).Attribute("ID").Value;
                                var shapeId = xmlShape.Attribute("ID").Value;

                                var imageRelId = xmlShape.XPathSelectElement("./v:ForeignData/v:Rel", VisioParser.NamespaceManager).Attribute(nsRel + "id").Value;

                                var imageRel = imageRels.First(r => r.Id == imageRelId);
                                var imagePart = package.GetPart(PackUriHelper.ResolvePartUri(pagePart.Uri, imageRel.TargetUri));
                                var uri = imagePart.Uri;

                                var fileBytes = VisioParser.ReadAllBytesFromStream(imagePart.GetStream());
                                var imageName = Path.GetFileName(uri.ToString());
                                var fileName = $"pageid_{pageId}_shapeid_{shapeId}_{imageName}";

                                // Save the image to the destination directory
                                var entry = zip.CreateEntry(fileName);
                                using (var entryStream = entry.Open())
                                {
                                    entryStream.Write(fileBytes, 0, fileBytes.Length);
                                }
                            }
                        }
                    }
                }
                output.Flush();
                return output.ToArray();
            }
        }
    }

}