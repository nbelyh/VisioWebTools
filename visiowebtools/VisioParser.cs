using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.IO;
using System.Linq;
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
            return ns;
        }

        public static readonly XmlNamespaceManager NamespaceManager = CreateVisioXmlNamespaceManager();

        public static IEnumerable<PackagePart> GetPackageParts(Package filePackage,
            string relationship)
        {
            var packageRels = filePackage.GetRelationshipsByType(relationship);
            foreach (var packageRel in packageRels)
            {
                Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), packageRel.TargetUri);
                yield return filePackage.GetPart(docUri);
            }
        }

        public static IEnumerable<PackagePart> GetPackageParts(Package filePackage,
            PackagePart sourcePart, string relationship)
        {
            var packageRels = sourcePart.GetRelationshipsByType(relationship);
            foreach (var packageRel in packageRels)
            {
                Uri partUri = PackUriHelper.ResolvePartUri(sourcePart.Uri, packageRel.TargetUri);
                yield return filePackage.GetPart(partUri);
            }
        }

        public static XDocument GetXMLFromPart(PackagePart packagePart)
        {
            var partStream = packagePart.GetStream();
            var partXml = XDocument.Load(partStream);
            return partXml;
        }

        //public static IEnumerable<XElement> GetXElementsByName(XDocument packagePart, string elementType)
        //{
        //    // Construct a LINQ query that selects elements by their element type.
        //    var elements =
        //        from element in packagePart.Descendants()
        //        where element.Name.LocalName == elementType
        //        select element;
        //    // Return the selected elements to the calling code.
        //    return elements.DefaultIfEmpty(null);
        //}
    }
}
