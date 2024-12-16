using System.IO.Packaging;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace VsdxTools
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
            ns.AddNamespace("ep", "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties");
            return ns;
        }

        public static readonly XmlNamespaceManager NamespaceManager = CreateVisioXmlNamespaceManager();

        public static XDocument GetXMLFromPart(PackagePart packagePart)
        {
            var partStream = packagePart.GetStream();
            var partXml = XDocument.Load(partStream);
            return partXml;
        }

        public static bool IsTextValue(string input)
        {
            return input != null && Regex.IsMatch(input, @"\p{L}");
        }

        public static void FlushStream(XDocument doc, Stream stream)
        {
            stream.SetLength(0);
            using var writer = new XmlTextWriter(stream, new UTF8Encoding(false));
            doc.Save(writer);
        }
    }
}
