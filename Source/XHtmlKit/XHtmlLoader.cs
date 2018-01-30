using System.Xml;
using System.IO;

namespace XHtmlKit
{
    public class XHtmlLoader
    {
        public static XmlDocument Load(string html, string baseUrl = null)
        {
            XmlDocument doc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlParserGeneric<XmlNode> parser = new HtmlParserGeneric<XmlNode>();
            TextReader reader = new StringReader(html);
            parser.Parse(dom, reader, baseUrl);
            return doc;
        }

        public static XmlDocument Load(TextReader reader, string baseUrl = null)
        {
            XmlDocument doc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlParserGeneric<XmlNode> parser = new HtmlParserGeneric<XmlNode>();
            parser.Parse(dom, reader, baseUrl);
            return doc;
        }

    }
}
