using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Xml;

namespace XHtmlKit
{
    public class HtmlAgilityParser: HtmlParser
    {        
        public override void LoadHtml(XmlDocument resultDoc, string html, string originatingUrl = null)
        {
            HtmlDocument htmlDoc = null;
            htmlDoc = new HtmlDocument();
            htmlDoc.OptionOutputAsXml = true;

            // Parse HTML
            htmlDoc.LoadHtml(html);

            // Transfer content to XmlDoc...
            ConvertHtml2Xml(htmlDoc, resultDoc, originatingUrl);
        }

        public override void LoadHtml(XmlDocument resultDoc, TextReader htmlReader, string originatingUrl = null)
        {
            HtmlDocument htmlDoc = null;

            htmlDoc = new HtmlDocument();
            htmlDoc.OptionOutputAsXml = true;

            // Parse HTML using HtmlAgilityPack
            htmlDoc.Load(htmlReader);

            // Transfer content to an XmlDoc for consistent XPath'ing...
            ConvertHtml2Xml(htmlDoc, resultDoc, originatingUrl);
        }       

        private static void ConvertHtml2Xml(HtmlDocument htmlDoc, XmlDocument doc, string originatingUrl = null)
        {
            StringWriter stringWriter = new StringWriter();
            htmlDoc.DocumentNode.Element("html").WriteTo(stringWriter);
            doc.InnerXml = stringWriter.ToString();

            // TODO - make this an option...
            FullyQualifyRelativeUrls(doc, originatingUrl);
            DecodeAttributeValues(doc);
        }

        private static void DecodeAttributeValues(XmlDocument doc)
        {
            // Decode any xml attributes that have not been decoded...
            var attributes = doc.SelectNodes("//attribute::*[contains(., '&#')]");
            foreach (XmlAttribute attr in attributes)
            {
                attr.Value = WebUtility.HtmlDecode(attr.Value);
            }

        }

        private static void FullyQualifyRelativeUrls(XmlDocument doc, string originatingUrl)
        {
            // Full-Qualify relative URLs 
            if (originatingUrl == null)
            {
                return;
            }

            // Get base Url
            System.Uri uri = new System.Uri(originatingUrl);
            string baseUrl = uri.Scheme + System.Uri.SchemeDelimiter + uri.Host + ":" + uri.Port;

            // Update relative img/@src and a/@href tags
            var relativeTags = doc.SelectNodes("//img/@src | //a/@href");
            foreach (XmlAttribute relativeTag in relativeTags)
            {
                // TODO: Better Regex for this?
                if (relativeTag.Value.StartsWith("/") && !relativeTag.Value.StartsWith("//"))
                {
                    relativeTag.Value = baseUrl + relativeTag.Value;
                }
            }

        }

        
    }
}