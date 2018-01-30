using System.Xml.Linq;
using System.IO;

namespace XHtmlKit.Linq
{
    public class XHtmlLoader
    {
        public static XDocument Load(string html, string baseUrl = null)
        {
            XDocument doc = new XDocument();
            XDomBuilder dom = new XDomBuilder(doc);
            TextReader reader = new StringReader(html);
            HtmlParserGeneric<XNode> parser = new HtmlParserGeneric<XNode>();
            parser.Parse(dom, reader, baseUrl, InsersionMode.BeforeHtml);
            return doc;
        }

        public static XDocument Load(TextReader htmlTextReader, string baseUrl = null)
        {
            XDocument doc = new XDocument();
            XDomBuilder dom = new XDomBuilder(doc);
            HtmlParserGeneric<XNode> parser = new HtmlParserGeneric<XNode>();
            parser.Parse(dom, htmlTextReader, baseUrl, InsersionMode.BeforeHtml);
            return doc;
        }

    }
}
