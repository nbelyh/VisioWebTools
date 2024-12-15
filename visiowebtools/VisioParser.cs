using System.IO.Packaging;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace VisioWebTools
{
    internal static class VisioParser
    {
        public static byte[] ReadAllBytesFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private static XmlNamespaceManager CreateVisioXmlNamespaceManager()
        {
            var ns = new XmlNamespaceManager(new NameTable());
            ns.AddNamespace("v", "http://schemas.microsoft.com/office/visio/2012/main");
            ns.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            ns.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            ns.AddNamespace("cp", "http://schemas.openxmlformats.org/package/2006/metadata/core-properties");
            return ns;
        }

        public static readonly XmlNamespaceManager NamespaceManager = CreateVisioXmlNamespaceManager();

        public static XDocument GetXMLFromPart(PackagePart packagePart)
        {
            var partStream = packagePart.GetStream();
            var partXml = XDocument.Load(partStream);
            return partXml;
        }
    }
}
