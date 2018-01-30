using System.IO;
using System.Xml;

namespace XHtmlKit
{
    public abstract class HtmlParser
    {
        public abstract void Parse(XmlDocument doc, TextReader htmlTextReader, string baseUrl = null);
        public abstract void ParseFragment(XmlNode rootNode, TextReader htmlTextReader, string baseUrl = null);

        private static HtmlParser _parser = new HtmlParserImpl();
        public static HtmlParser DefaultParser
        {
            get { return _parser; }
            set { _parser = value; }
        }
    }

    public class HtmlParserImpl : HtmlParser
    {
        public override void Parse(XmlDocument doc, TextReader htmlTextReader, string baseUrl = null)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlParserGeneric<XmlNode> parser = new HtmlParserGeneric<XmlNode>();
            parser.Parse(dom, htmlTextReader, baseUrl, InsersionMode.BeforeHtml);
        }

        public override void ParseFragment(XmlNode rootNode, TextReader htmlTextReader, string baseUrl = null)
        {
            XmlDomBuilder dom = new XmlDomBuilder(rootNode);
            HtmlParserGeneric<XmlNode> parser = new HtmlParserGeneric<XmlNode>();
            parser.Parse(dom, htmlTextReader, baseUrl, InsersionMode.InBody);
        }
    }


}
