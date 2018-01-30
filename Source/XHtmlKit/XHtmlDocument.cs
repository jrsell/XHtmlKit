using System.Xml;
using System.IO;

namespace XHtmlKit
{
    public static class XHtmlDocument
    {

        public static XmlDocument Load(string html, string baseUrl = null, HtmlParser parser = null)
        {
            HtmlParser p = (parser == null ? HtmlParser.DefaultParser : parser);
            XmlDocument doc = new XmlDocument();
            p.LoadHtml(doc, html, baseUrl);
            return doc;
        }

        public static XmlDocument Load(TextReader htmlTextReader, string baseUrl = null, HtmlParser parser = null)
        {
            HtmlParser p = (parser == null ? HtmlParser.DefaultParser : parser);
            XmlDocument doc = new XmlDocument();
            p.LoadHtml(doc, htmlTextReader, baseUrl);
            return doc;
        }
        
    }
}
