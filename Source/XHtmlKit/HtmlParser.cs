using System.IO;
using System.Xml;

namespace XHtmlKit
{
    public abstract class HtmlParser
    {
        public abstract void Parse(XmlDocument doc, TextReader htmlTextReader, HtmlParserOptions options);

        private static HtmlParser _parser = new HtmlParserImpl();
        public static HtmlParser DefaultParser
        {
            get { return _parser; }
            set { _parser = value; }
        }
    }

    public class HtmlParserImpl : HtmlParser
    {
        public void Parse(XmlDocument doc, string html)
        {
            Parse(doc, html, new HtmlParserOptions());
        }

        public void Parse(XmlDocument doc, string html, HtmlParserOptions options)
        {
            Parse(doc, new StringReader(html), options);
        }

        public void Parse(XmlDocument doc, TextReader htmlTextReader)
        {
            Parse(doc, htmlTextReader, new HtmlParserOptions());
        }

        public override void Parse(XmlDocument doc, TextReader htmlTextReader, HtmlParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            parser.Parse(dom, htmlTextReader, options);
        }

        public void ParseFragment(XmlNode node, string html)
        {
            ParseFragment(node, html, new HtmlParserOptions());
        }

        public void ParseFragment(XmlNode node, string html, HtmlParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(node);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            parser.Parse(dom, new StringReader(html), options, InsersionMode.InBody);
        }

    }


}
