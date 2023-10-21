using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace VisioWebTools
{
    public class SplitPagesService
    {
        public static XmlNamespaceManager CreateVisioXmlNamespaceManager()
        {
            var ns = new XmlNamespaceManager(new NameTable());
            ns.AddNamespace("v", "http://schemas.microsoft.com/office/visio/2012/main");
            ns.AddNamespace("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            return ns;
        }

        public static readonly XmlNamespaceManager VisioNamespaceManager = CreateVisioXmlNamespaceManager();

        public static byte[] ReadAllBytesFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static string MakeSafeFileName(string unsafeFileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                unsafeFileName = unsafeFileName.Replace(c.ToString(), "_");
            }
            return unsafeFileName;
        }

        public static HashSet<string> GetRelatedPages(string pageId, List<PageInfo> infos)
        {
            var result = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(pageId);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                var info = infos.FirstOrDefault(i => i.PageId == id);
                if (info != null)
                {
                    result.Add(id);
                    if (!string.IsNullOrEmpty(info.BackPage))
                    {
                        queue.Enqueue(info.BackPage);
                    }
                }
            }

            return result;
        }

        public static byte[] SplitFile(Stream stream)
        {
            using (var output = new MemoryStream())
            {
                using (var zip = new ZipArchive(output, ZipArchiveMode.Create))
                {
                    var pageInfos = GetPageInfos(stream);

                    foreach (var pageInfo in pageInfos.Where(p => !p.Background))
                    {
                        using (var pageStream = new MemoryStream())
                        {
                            stream.Position = 0;
                            stream.CopyTo(pageStream);

                            var pagesToKeep = GetRelatedPages(pageInfo.PageId, pageInfos);
                            RemovePagesExcept(pageStream, pagesToKeep);

                            var fileName = MakeSafeFileName(pageInfo.PageName);
                            var entry = zip.CreateEntry($"{fileName}.vsdx");
                            using (var entryStream = entry.Open())
                            {
                                var fileBytes = ReadAllBytesFromStream(pageStream);
                                entryStream.Write(fileBytes, 0, fileBytes.Length);
                            }
                        }
                    }
                }
                output.Flush();
                return output.ToArray();
            }
        }

        public static void RemovePagesExcept(Stream stream, HashSet<string> pagesToKeep)
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

                for (var i = pageRels.Count - 1; i >= 0; --i)
                {
                    var pageRel = pageRels.ElementAt(i);
                    Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);

                    var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioNamespaceManager);
                    var pageId = xmlPage.Attribute("ID").Value;
                    if (!pagesToKeep.Contains(pageId))
                    {
                        xmlPage.Remove();
                        package.DeletePart(pageUri);
                        pagesPart.DeleteRelationship(pageRel.Id);
                    }
                }

                pagesStream.SetLength(0);
                using (var writer = new XmlTextWriter(pagesStream, new UTF8Encoding(false)))
                {
                    xmlPages.Save(writer);
                }

                package.Flush();
            }
        }

        public class PageInfo
        {
            public string PageId { get; set; }
            public string PageName { get; set; }
            public bool Background { get; set; }
            public string BackPage { get; set; }
        }

        public static List<PageInfo> GetPageInfos(Stream stream)
        {
            using (Package package = Package.Open(stream))
            {
                var documentRel = package.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/document").First();
                Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), documentRel.TargetUri);
                var documentPart = package.GetPart(docUri);

                var pagesRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/pages").First();
                Uri pagesUri = PackUriHelper.ResolvePartUri(documentPart.Uri, pagesRel.TargetUri);
                var pagesPart = package.GetPart(pagesUri);
                
                var pagesStream = pagesPart.GetStream();

                var result = new List<PageInfo>();
                var xmlPages = XDocument.Load(pagesStream).XPathSelectElements($"/v:Pages/v:Page", VisioNamespaceManager);
                foreach (var xmlPage in xmlPages)
                {
                    var pageInfo = new PageInfo
                    {
                        PageId = xmlPage.Attribute("ID").Value,
                        PageName = xmlPage.Attribute("Name").Value,
                        Background = xmlPage.Attribute("Background")?.Value == "1",
                        BackPage = xmlPage.Attribute("BackPage")?.Value
                    };
                    result.Add(pageInfo);
                }

                return result;
            }
        }
    }

}