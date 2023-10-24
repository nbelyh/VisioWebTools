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

        public static byte[] SplitPages(Stream stream)
        {
            using (var output = new MemoryStream())
            {
                using (var zip = new ZipArchive(output, ZipArchiveMode.Create))
                {
                    var info = GetPageInfos(stream);

                    foreach (var pageInfo in info.PageInfos.Where(p => !p.Background))
                    {
                        using (var pageStream = new MemoryStream())
                        {
                            stream.Position = 0;
                            stream.CopyTo(pageStream);

                            var pagesToKeep = GetRelatedPages(pageInfo.PageId, info.PageInfos);
                            RemovePagesExcept(pageStream, pagesToKeep, info);

                            var fileName = MakeSafeFileName(pageInfo.PageName);
                            var entry = zip.CreateEntry($"{fileName}.vsdx");
                            using (var entryStream = entry.Open())
                            {
                                pageStream.Position = 0;
                                pageStream.WriteTo(entryStream);
                            }
                        }
                    }
                }
                output.Flush();
                return output.ToArray();
            }
        }

        public static void RemovePagesExcept(Stream stream, HashSet<string> pagesToKeep, DocumentInfo info)
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

                var mediaToRemoveUrls = new HashSet<Uri>();
                for (var i = pageRels.Count - 1; i >= 0; --i)
                {
                    var pageRel = pageRels.ElementAt(i);
                    Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);

                    var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);
                    var pageId = xmlPage.Attribute("ID").Value;
                    if (!pagesToKeep.Contains(pageId))
                    {
                        xmlPage.Remove();
                        package.DeletePart(pageUri);
                        pagesPart.DeleteRelationship(pageRel.Id);
                        var pageInfo = info.PageInfos.Find(x => x.PageId == pageId);
                        mediaToRemoveUrls.UnionWith(pageInfo.UsedMedia);
                    }
                }

                var mediaToKeepUrls = new HashSet<Uri>();
                mediaToKeepUrls.UnionWith(info.UsedMedia);
                foreach (var pageIdToKeep in pagesToKeep)
                {
                    var pageInfo = info.PageInfos.Find(x => x.PageId == pageIdToKeep);
                    mediaToKeepUrls.UnionWith(pageInfo.UsedMedia);
                }

                foreach (var mediaToRemoveUrl in mediaToRemoveUrls)
                {
                    if (!mediaToKeepUrls.Contains(mediaToRemoveUrl))
                        package.DeletePart(mediaToRemoveUrl);
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

            public HashSet<Uri> UsedMedia { get; set; }
        }

        public class DocumentInfo
        {
            public HashSet<Uri> UsedMedia { get; set; }
            public List<PageInfo> PageInfos { get; set; }
        }

        public static DocumentInfo GetPageInfos(Stream stream)
        {
            var result = new DocumentInfo
            {
              UsedMedia = new HashSet<Uri>(),
              PageInfos = new List<PageInfo>()
            };

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
                    var xmlPage = xmlPages.XPathSelectElement($"/v:Pages/v:Page[v:Rel/@r:id='{pageRel.Id}']", VisioParser.NamespaceManager);
                    var pageInfo = new PageInfo
                    {
                        PageId = xmlPage.Attribute("ID").Value,
                        PageName = xmlPage.Attribute("Name").Value,
                        Background = xmlPage.Attribute("Background")?.Value == "1",
                        BackPage = xmlPage.Attribute("BackPage")?.Value,
                        UsedMedia = new HashSet<Uri>()
                    };

                    Uri pageUri = PackUriHelper.ResolvePartUri(pagesPart.Uri, pageRel.TargetUri);
                    var pagePart = package.GetPart(pageUri);

                    var imageRels = pagePart.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/image").ToList();
                    foreach (var imageRel in imageRels)
                    {
                        var mediaUri = PackUriHelper.ResolvePartUri(pagePart.Uri, imageRel.TargetUri);
                        pageInfo.UsedMedia.Add(mediaUri);
                    }

                    var oleObjectRels = pagePart.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/oleObject").ToList();
                    foreach (var oleObjectRel in oleObjectRels)
                    {
                        var mediaUri = PackUriHelper.ResolvePartUri(pagePart.Uri, oleObjectRel.TargetUri);
                        pageInfo.UsedMedia.Add(mediaUri);
                    }

                    result.PageInfos.Add(pageInfo);
                }

                result.UsedMedia = GetMastersUsedMedia(package, documentPart);

                return result;
            }
        }

        private static HashSet<Uri> GetMastersUsedMedia(Package package, PackagePart documentPart)
        {
            var usedMedia = new HashSet<Uri>();
            var mastersRel = documentPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/masters").FirstOrDefault();
            if (mastersRel != null)
            {
                Uri mastersUri = PackUriHelper.ResolvePartUri(documentPart.Uri, mastersRel.TargetUri);
                var mastersPart = package.GetPart(mastersUri);
                var xmlMasters = VisioParser.GetXMLFromPart(mastersPart);

                var masterRels = mastersPart.GetRelationshipsByType("http://schemas.microsoft.com/visio/2010/relationships/master").ToList();
                foreach (var masterRel in masterRels)
                {
                    var xmlMaster = xmlMasters.XPathSelectElement($"/v:Masters/v:Master[v:Rel/@r:id='{masterRel.Id}']", VisioParser.NamespaceManager);
                    Uri masterUri = PackUriHelper.ResolvePartUri(mastersPart.Uri, masterRel.TargetUri);
                    var masterPart = package.GetPart(masterUri);

                    var imageRels = masterPart.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/image").ToList();
                    foreach (var imageRel in imageRels)
                    {
                        var mediaUri = PackUriHelper.ResolvePartUri(masterPart.Uri, imageRel.TargetUri);
                        usedMedia.Add(mediaUri);
                    }
                }
            }
            return usedMedia;
        }
    }

}