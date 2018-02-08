using System.IO;
using System.Xml;

namespace XHtmlKit
{
    public abstract class HtmlParser
    {
        public abstract void Parse(XmlDocument doc, HtmlTextReader htmlTextReader, HtmlParserOptions options);
        public abstract void ParseFragment(XmlNode node, HtmlTextReader htmlTextReader, HtmlParserOptions options);

        private static HtmlParser _parser = new HtmlParserImpl();
        public static HtmlParser DefaultParser
        {
            get { return _parser; }
            set { _parser = value; }
        }

        // Static overloads 
        public static void Parse(XmlDocument doc, string html)
        {
            DefaultParser.Parse(doc, new HtmlTextReader(html), new HtmlParserOptions());
        }

        public static void Parse(XmlDocument doc, string html, HtmlParserOptions options)
        {
            DefaultParser.Parse(doc, new HtmlTextReader(html), options);
        }

        public static void Parse(XmlDocument doc, TextReader htmlTextReader)
        {
            DefaultParser.Parse(doc, new HtmlTextReader(htmlTextReader), new HtmlParserOptions());
        }

        public static void Parse(XmlDocument doc, TextReader htmlTextReader, HtmlParserOptions options)
        {
            DefaultParser.Parse(doc, new HtmlTextReader(htmlTextReader), options);
        }

        public static void ParseFragment(XmlNode node, string html)
        {
            DefaultParser.ParseFragment(node, new HtmlTextReader(html), new HtmlParserOptions());
        }

        public static void ParseFragment(XmlNode node, TextReader reader)
        {
            DefaultParser.ParseFragment(node, new HtmlTextReader(reader), new HtmlParserOptions());
        }


    }

    internal class HtmlParserImpl : HtmlParser
    {
        public override void Parse(XmlDocument doc, HtmlTextReader reader, HtmlParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            parser.Parse(dom, reader, options);
        }

        public override void ParseFragment(XmlNode node, HtmlTextReader reader, HtmlParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(node);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();            
            parser.Parse(dom, reader, options, InsersionMode.InBody);
        }

    }


}
