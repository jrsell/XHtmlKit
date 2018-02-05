using System.Xml;
using System.IO;

#if !net20
using System.Xml.Linq;
#endif 

namespace XHtmlKit
{
    public class XHtmlLoader
    {
        public static XmlDocument LoadXmlDocument(string html, string baseUrl = null)
        {
            XmlDocument doc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            HtmlTextReader reader = new HtmlTextReader( new StringReader(html) );
            parser.Parse(dom, reader, baseUrl);
            return doc;
        }

        public static XmlDocument LoadXmlDocument(TextReader htmlTextReader, string baseUrl = null)
        {
            XmlDocument doc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            HtmlTextReader reader = new HtmlTextReader(htmlTextReader);
            parser.Parse(dom, reader, baseUrl);
            return doc;
        }

#if !net20
        public static XDocument LoadXDocument(string html, string baseUrl = null)
        {
            XDocument doc = new XDocument();
            XDomBuilder dom = new XDomBuilder(doc);
            HtmlTextReader reader = new HtmlTextReader(html);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            parser.Parse(dom, reader, baseUrl, InsersionMode.BeforeHtml);
            return doc;
        }

        public static XDocument LoadXDocument(TextReader htmlTextReader, string baseUrl = null)
        {
            XDocument doc = new XDocument();
            XDomBuilder dom = new XDomBuilder(doc);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            HtmlTextReader reader = new HtmlTextReader(htmlTextReader);
            parser.Parse(dom, reader, baseUrl, InsersionMode.BeforeHtml);
            return doc;
        }
#endif

    }
}
