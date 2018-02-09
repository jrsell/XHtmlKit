using System.Xml;
using System.IO;


namespace XHtmlKit
{
    public partial class XHtmlLoader
    {

        public static XmlDocument LoadXmlDocument(string html)
        {
            return LoadXmlDocument(html, new HtmlParserOptions());
        }

        public static XmlDocument LoadXmlDocument(string html, HtmlParserOptions options)
        {
            return LoadXmlDocument(new StringReader(html), options);
        }

        public static XmlDocument LoadXmlDocument(TextReader htmlTextReader)
        {
            return LoadXmlDocument(htmlTextReader, new HtmlParserOptions());
        }

        public static XmlDocument LoadXmlDocument(TextReader htmlTextReader, HtmlParserOptions options)
        {
            XmlDocument doc = new XmlDocument();
            LoadXmlDocument(doc, htmlTextReader, options);
            return doc;
        }

        private static void LoadXmlDocument(XmlDocument doc, TextReader htmlTextReader, HtmlParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            HtmlTextReader reader = new HtmlTextReader(htmlTextReader);
            parser.Parse(dom, reader, options);
        }

        public static void LoadXmlFragment(XmlNode node, string html)
        {
            LoadXmlFragment(node, new StringReader(html), new HtmlParserOptions());
        }

        public static void LoadXmlFragment(XmlNode node, TextReader reader, HtmlParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(node);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            HtmlTextReader htmlReader = new HtmlTextReader(reader);
            parser.Parse(dom, htmlReader, options, InsersionMode.InBody);
        }
    }
}