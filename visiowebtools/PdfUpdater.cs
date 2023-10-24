using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using System.IO.Packaging;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml.XPath;
using System;
using System.Globalization;
using PdfSharpCore.Pdf.Annotations;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace VisioWebTools
{
    public class PdfUpdater
    {
        public static byte[] Process(byte[] pdf, byte[] vsdx, PdfOptions options)
        {
            using (var vsdxStream = new MemoryStream(vsdx))
            using (Package package = Package.Open(vsdxStream))
            {
                var documentPart = VisioParser.GetPackageParts(package, "http://schemas.microsoft.com/visio/2010/relationships/document").First();

                var pagesPart = VisioParser.GetPackageParts(package, documentPart, "http://schemas.microsoft.com/visio/2010/relationships/pages").First();

                var pageParts = VisioParser.GetPackageParts(package, pagesPart, "http://schemas.microsoft.com/visio/2010/relationships/page");

                var visioPages = pageParts.Select(pagePart => VisioParser.GetXMLFromPart(pagePart)).ToList();

                return AddCommentsToPdf(visioPages, pdf, options);
            }
        }

        private static byte[] AddCommentsToPdf(List<XDocument> visioPages, byte[] pdf, PdfOptions options)
        {
            using (var pdfDocStream = new MemoryStream(pdf))
            using (var pdfDoc = PdfReader.Open(pdfDocStream))
            {
                for (var i = 0; i < pdfDoc.PageCount; ++i)
                {
                    var pdfPage = pdfDoc.Pages[i];
                    var visioPage = visioPages[i];

                    var shapes = visioPage.XPathSelectElements("/v:PageContents/v:Shapes/v:Shape", VisioParser.NamespaceManager).ToList();

                    foreach (var shape in shapes)
                    {
                        string GetCellValue(string name)
                        {
                            var cell = shape.XPathSelectElement($"v:Cell[@N='{name}']", VisioParser.NamespaceManager);
                            return cell?.Attribute("V")?.Value;
                        }

                        double getCellDoubleValue(string name)
                        {
                            return Convert.ToDouble(GetCellValue(name), CultureInfo.InvariantCulture);
                        }

                        // if comment exists
                        var comment = GetCellValue("Comment");
                        if (!string.IsNullOrEmpty(comment))
                        {
                            // add it as annotation
                            var pinX = getCellDoubleValue("PinX");
                            var pinY = getCellDoubleValue("PinY");
                            var width = getCellDoubleValue("Width");
                            var height = getCellDoubleValue("Height");

                            var x = pinX - (1 - options.HorizontalLocation) * width / 2;
                            var y = pinY - (options.VerticalLocation - 1) * height / 2;

                            PdfTextAnnotationIcon icon = PdfTextAnnotationIcon.Note;
                            Enum.TryParse(options.Icon, out icon);

                            var annotation = new PdfTextAnnotation
                            {
                                Title = comment,
                                Contents = comment,
                                Icon = icon,
                                Color = XColor.FromArgb(options.Color.ToArgb())
                            };

                            // # inches to points
                            var point = new XPoint(x * 72, y * 72);
                            var size = new XSize(0, 0);
                            var rect = new XRect(point, size);
                            annotation.Rectangle = new PdfRectangle(rect);

                            pdfPage.Annotations.Add(annotation);
                        }
                    }
                }

                using (var outputStream = new MemoryStream())
                {
                    pdfDoc.Save(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }
}
