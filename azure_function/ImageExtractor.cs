using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO.Compression;

namespace VisioMediaExtractor
{

  class ImageExtractor
  {
    public static byte[] ReadAllBytesFromStream(Stream stream)
    {
      using (var ms = new MemoryStream())
      {
        stream.CopyTo(ms);
        return ms.ToArray();
      }
    }

    public static XDocument GetXMLFromPart(PackagePart packagePart)
    {
      var partStream = packagePart.GetStream();
      var partXml = XDocument.Load(partStream);
      return partXml;
    }

    public static byte[] ExtractMediaFromVisio(Stream stream)
    {
      using (var output = new MemoryStream())
      {
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create))
        {
          XNamespace nsRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

          var ns = new XmlNamespaceManager(new NameTable());
          ns.AddNamespace("v", "http://schemas.microsoft.com/office/visio/2012/main");
          ns.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

          using (Package visioPackage = Package.Open(stream))
          {
            var documentRel = visioPackage.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
            Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
            var documentPart = visioPackage.GetPart(docUri);

            var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
            Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
            var pagesPart = visioPackage.GetPart(pagesUri);

            var xmlPages = GetXMLFromPart(pagesPart);
            var pageRels = pagesPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/page").ToList();
            foreach (var pageRel in pageRels)
            {
              Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
              var pagePart = visioPackage.GetPart(pageUri);

              var imageRels = pagePart.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/image").ToList();

              var xmlPage = GetXMLFromPart(pagePart);

              var xmlShapes = xmlPage.XPathSelectElements("/v:PageContents//v:Shape[@Type='Foreign']", ns).ToList();
              foreach (var xmlShape in xmlShapes)
              {
                var pageId = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", ns).Attribute("ID").Value;
                var shapeId = xmlShape.Attribute("ID").Value;

                var imageRelId = xmlShape.XPathSelectElement("./v:ForeignData/v:Rel", ns).Attribute(nsRel + "id").Value;

                var imageRel = imageRels.First(r => r.Id == imageRelId);
                var imagePart = visioPackage.GetPart(PackUriHelper.ResolvePartUri(pagePart.Uri, imageRel.TargetUri));
                var uri = imagePart.Uri;

                var fileBytes = ReadAllBytesFromStream(imagePart.GetStream());
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