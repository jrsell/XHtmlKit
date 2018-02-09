#if !net20

using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Threading.Tasks;

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

    public static partial class XHtmlLoader
    {
        #region XmlDocument extension methods
        public static void LoadHtml(this XmlDocument doc, string html)
        {
            LoadXmlDocument(doc, new StringReader(html), new HtmlParserOptions());
        }

        public static void LoadHtmlFragment(this XmlNode node, string html) {
            LoadXmlFragment(node, new StringReader(html), new HtmlParserOptions());
        }

        public static async void LoadWebPageAsync(this XmlDocument doc, string url)
        {
            await LoadXmlDocumentAsync(doc, url, new XHtmlLoaderOptions());
        }
        #endregion

        #region LoadXmlDocumentAsync overloads
        public static async Task<XmlDocument> LoadXmlDocumentAsync(string url)
        {
            return await LoadXmlDocumentAsync(url, new XHtmlLoaderOptions());
        }

        public static async Task<XmlDocument> LoadXmlDocumentAsync(string url, XHtmlLoaderOptions options)
        {
            XmlDocument xhtmlDoc = new XmlDocument();
            await LoadXmlDocumentAsync(xhtmlDoc, url, options);
            return xhtmlDoc;
        }

        private static async Task LoadXmlDocumentAsync(XmlDocument doc, string url, XHtmlLoaderOptions options)
        {
            XHtmlLoaderOptions optionsToUse = options == null ? new XHtmlLoaderOptions() : options;
            optionsToUse.ParserOptions.BaseUrl = string.IsNullOrEmpty(optionsToUse.ParserOptions.BaseUrl) ? url : optionsToUse.ParserOptions.BaseUrl;

            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url, optionsToUse.ClientOptions))
                parser.Parse(dom, htmlReader, optionsToUse.ParserOptions);
        }
        #endregion

        // XDocument Methods

        #region XDocument extension methods
        public static void LoadHtml(this XDocument doc, string html)
        {
            LoadXDocument(doc, new StringReader(html), new HtmlParserOptions());
        }

        public static void LoadHtmlFragment(this XNode node, string html)
        {
            LoadXFragment(node, new StringReader(html), new HtmlParserOptions());
        }

        public static async void LoadWebPageAsync(this XDocument doc, string url)
        {
            await LoadXDocumentAsync(doc, url, new XHtmlLoaderOptions());
        }
        #endregion

        #region LoadXDocument overloads
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
            LoadXDocument(doc, reader, options);
            return doc;
        }

        private static void LoadXDocument(XDocument doc, TextReader reader, HtmlParserOptions options)
        {
            XDomBuilder dom = new XDomBuilder(doc);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            HtmlTextReader htmlTextReader = new HtmlTextReader(reader);
            parser.Parse(dom, htmlTextReader, options);
        }
        #endregion

        #region LoadXDocumentAsync overloads

        public static async Task<XDocument> LoadXDocumentAsync(string url)
        {
            return await LoadXDocumentAsync(url, new XHtmlLoaderOptions());
        }

        public static async Task<XDocument> LoadXDocumentAsync(string url, XHtmlLoaderOptions options)
        {
            XDocument xhtmlDoc = new XDocument();
            await LoadXDocumentAsync(xhtmlDoc, url, options);
            return xhtmlDoc;
        }

        private static async Task LoadXDocumentAsync(XDocument doc, string url, XHtmlLoaderOptions options)
        {
            XHtmlLoaderOptions optionsToUse = options == null ? new XHtmlLoaderOptions() : options;
            optionsToUse.ParserOptions.BaseUrl = string.IsNullOrEmpty(optionsToUse.ParserOptions.BaseUrl) ? url : optionsToUse.ParserOptions.BaseUrl;

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url, optionsToUse.ClientOptions))
            {
                XDomBuilder dom = new XDomBuilder(doc);
                HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
                parser.Parse(dom, htmlReader, optionsToUse.ParserOptions);
            }
        }

        #endregion

        #region LoadXFragment overloads

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

        #endregion
    }
}
#endif 
