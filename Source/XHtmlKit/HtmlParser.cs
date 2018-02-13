using System.IO;
using System.Xml;

namespace XHtmlKit
{
    internal abstract class HtmlParser
    {
        public abstract void Parse(XmlDocument doc, HtmlTextReader htmlTextReader, ParserOptions options);
        public abstract void ParseFragment(XmlNode node, HtmlTextReader htmlTextReader, ParserOptions options);

        private static HtmlParser _parser = new HtmlParserImpl();
        public static HtmlParser DefaultParser
        {
            get { return _parser; }
            set { _parser = value; }
        }
    }

    internal class HtmlParserImpl : HtmlParser
    {
        public override void Parse(XmlDocument doc, HtmlTextReader reader, ParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            parser.Parse(dom, reader, options);
        }

        public override void ParseFragment(XmlNode node, HtmlTextReader reader, ParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(node);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();            
            parser.Parse(dom, reader, options, InsersionMode.InBody);
        }

    }


}
