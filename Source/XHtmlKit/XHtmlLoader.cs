using System.Xml;
using System.IO;


namespace XHtmlKit
{
    /// <summary>
    /// Static class for loading an XmlDocument with Html.
    /// </summary>
    public partial class XHtmlLoader
    {

        public static XmlDocument LoadHtml(string html)
        {
            return LoadHtml(html, new ParserOptions());
        }

        public static XmlDocument LoadHtml(string html, ParserOptions options)
        {
            return LoadHtml(new StringReader(html), options);
        }

        public static XmlDocument LoadHtml(TextReader htmlTextReader)
        {
            return LoadHtml(htmlTextReader, new ParserOptions());
        }

        public static XmlDocument LoadHtml(TextReader htmlTextReader, ParserOptions options)
        {
            XmlDocument doc = new XmlDocument();
            LoadHtml(doc, htmlTextReader, options);
            return doc;
        }

        internal static void LoadHtml(XmlDocument doc, TextReader htmlTextReader, ParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            HtmlTextReader reader = new HtmlTextReader(htmlTextReader);
            parser.Parse(dom, reader, options);
        }

        public static void LoadHtmlFragment(XmlNode node, string html)
        {
            LoadHtmlFragment(node, new StringReader(html), new ParserOptions());
        }

        public static void LoadHtmlFragment(XmlNode node, TextReader reader, ParserOptions options)
        {
            XmlDomBuilder dom = new XmlDomBuilder(node);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            HtmlTextReader htmlReader = new HtmlTextReader(reader);
            parser.Parse(dom, htmlReader, options, InsersionMode.InBody);
        }
    }
}