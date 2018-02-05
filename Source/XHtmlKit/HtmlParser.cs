using System.IO;
using System.Xml;

namespace XHtmlKit
{
    public abstract class HtmlParser
    {
        public abstract void Parse(XmlDocument doc, HtmlTextReader htmlTextReader, string baseUrl = null);

        private static HtmlParser _parser = new HtmlParserImpl();
        public static HtmlParser DefaultParser
        {
            get { return _parser; }
            set { _parser = value; }
        }
    }

    public class HtmlParserImpl : HtmlParser
    {
        public override void Parse(XmlDocument doc, HtmlTextReader htmlTextReader, string baseUrl = null)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            parser.Parse(dom, htmlTextReader, baseUrl, InsersionMode.BeforeHtml);
        }

        public void ParseFragment(XmlNode rootNode, HtmlTextReader htmlTextReader, string baseUrl = null)
        {
            XmlDomBuilder dom = new XmlDomBuilder(rootNode);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            parser.Parse(dom, htmlTextReader, baseUrl, InsersionMode.InBody);
        }
    }


}
