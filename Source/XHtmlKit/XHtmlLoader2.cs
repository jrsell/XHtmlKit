#if !net20

using System.Xml;
using System.IO;
using System.Xml.Linq;
using XHtmlKit.Network;

namespace XHtmlKit
{
    public class XHtmlLoaderOptions
    {
        private HtmlClientOptions _clientOptions = new HtmlClientOptions();
        private HtmlParserOptions _parserOptions = new HtmlParserOptions();
        public HtmlClientOptions ClientOptions { get { return _clientOptions; } }
        public HtmlParserOptions ParserOptions { get { return _parserOptions; } }
    }

    public partial class XHtmlLoader
    {
        public static async System.Threading.Tasks.Task<XmlDocument> LoadXmlDocumentAsync(string url)
        {
            return await LoadXmlDocumentAsync(url, new XHtmlLoaderOptions());
        }

        public static async System.Threading.Tasks.Task<XmlDocument> LoadXmlDocumentAsync(string url, XHtmlLoaderOptions options)
        {
            XHtmlLoaderOptions optionsToUse = options == null ? new XHtmlLoaderOptions() : options;
            optionsToUse.ParserOptions.BaseUrl = string.IsNullOrEmpty(optionsToUse.ParserOptions.BaseUrl) ? url : optionsToUse.ParserOptions.BaseUrl;

            XmlDocument xhtmlDoc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(xhtmlDoc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url, optionsToUse.ClientOptions))
                parser.Parse(dom, htmlReader, optionsToUse.ParserOptions);

            return xhtmlDoc;
        }

        public static XDocument LoadXDocument(string html)
        {
            return LoadXDocument(html, new HtmlParserOptions());
        }

        public static XDocument LoadXDocument(string html, HtmlParserOptions options)
        {
            return LoadXDocument(new StringReader(html), options);
        }

        public static XDocument LoadXDocument(TextReader htmlTextReader)
        {
            return LoadXDocument(htmlTextReader, new HtmlParserOptions());
        }

        public static XDocument LoadXDocument(TextReader reader, HtmlParserOptions options)
        {
            XDocument doc = new XDocument();
            XDomBuilder dom = new XDomBuilder(doc);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            HtmlTextReader htmlTextReader = new HtmlTextReader(reader);
            parser.Parse(dom, htmlTextReader, options);
            return doc;
        }

        public static async System.Threading.Tasks.Task<XDocument> LoadXDocumentAsync(string url)
        {
            return await LoadXDocumentAsync(url, new XHtmlLoaderOptions());
        }

        public static async System.Threading.Tasks.Task<XDocument> LoadXDocumentAsync(string url, XHtmlLoaderOptions options)
        {
            XHtmlLoaderOptions optionsToUse = options == null ? new XHtmlLoaderOptions() : options;
            optionsToUse.ParserOptions.BaseUrl = string.IsNullOrEmpty(optionsToUse.ParserOptions.BaseUrl) ? url : optionsToUse.ParserOptions.BaseUrl;

            XDocument xhtmlDoc = new XDocument();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url, optionsToUse.ClientOptions))
            {
                XDomBuilder dom = new XDomBuilder(xhtmlDoc);
                HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
                parser.Parse(dom, htmlReader, optionsToUse.ParserOptions);
            }
            return xhtmlDoc;
        }

        public static void LoadXFragment(XNode node, string html)
        {
            LoadXFragment(node, new StringReader(html), new HtmlParserOptions());
        }

        public static void LoadXFragment(XNode node, TextReader reader, HtmlParserOptions options)
        {
            XDomBuilder dom = new XDomBuilder(node);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            HtmlTextReader htmlTextReader = new HtmlTextReader(reader);
            parser.Parse(dom, htmlTextReader, options, InsersionMode.InBody);
        }
    }
}
#endif 
