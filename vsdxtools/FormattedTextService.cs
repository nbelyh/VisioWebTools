
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VsdxTools
{
    public class TextResult
    {
        public string PlainText { get; set; }
        public string FormattedText { get; set; }
    }

    public class FormattedTextService
    {
        class ParsedItem
        {
            public bool IsTag { get; set; }
            public string Content { get; set; }
            public int IX { get; set; }
        }

        public static TextResult GetShapeText(XElement xmlText)
        {
            var plainText = new StringBuilder();
            var formattedText = new StringBuilder();
            foreach (var node in xmlText.Nodes())
            {
                if (node is XText text)
                {
                    plainText.Append(text.Value);
                    formattedText.Append(text.Value);
                }
                else if (node is XElement el)
                {
                    formattedText.Append($"{{{el.Name.LocalName}{el.Attribute("IX")?.Value}}}");
                }
            }
            return new TextResult
            {
                PlainText = plainText.ToString(),
                FormattedText = formattedText.ToString()
            };
        }

        private static List<ParsedItem> ParseShapeText(string input)
        {
            var result = new List<ParsedItem>();
            string pattern = @"\{(?<TagName>[a-zA-Z]+)(?<IX>\d+)\}|(?<Text>[^{}]+)";

            foreach (Match match in Regex.Matches(input, pattern))
            {
                if (match.Groups["TagName"].Success && match.Groups["IX"].Success)
                {
                    result.Add(new ParsedItem
                    {
                        IsTag = true,
                        Content = match.Groups["TagName"].Value,
                        IX = int.Parse(match.Groups["IX"].Value)
                    });
                }
                else if (match.Groups["Text"].Success)
                {
                    result.Add(new ParsedItem
                    {
                        IsTag = false,
                        Content = match.Groups["Text"].Value
                    });
                }
            }
            return result;
        }

        public static void BuildXElements(XElement root, string input)
        {
            var items = ParseShapeText(input);

            XNamespace ns = "http://schemas.microsoft.com/office/visio/2012/main";
            root.RemoveAll();
            foreach (var item in items)
            {
                if (item.IsTag)
                {
                    root.Add(new XElement(ns + item.Content, new XAttribute("IX", item.IX)));
                }
                else
                {
                    root.Add(new XText(item.Content));
                }
            }
        }
    }
}