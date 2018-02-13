#if !net20

using System.Xml.Linq;
using System.IO;
using System.Threading.Tasks;
using XHtmlKit.Network;

namespace XHtmlKit
{
    /// <summary>
    /// Static class for loading an XDocument with Html
    /// </summary>
    public static class XHtmlLoaderX
    {

        #region LoadHtml overloads
        public static XDocument LoadHtml(string html)
        {
            return LoadHtml(new StringReader(html), new HtmlParserOptions());
        }

        public static XDocument LoadHtml(string html, HtmlParserOptions options)
        {
            return LoadHtml(new StringReader(html), options);
        }

        public static XDocument LoadHtml(TextReader htmlTextReader)
        {
            return LoadHtml(htmlTextReader, new HtmlParserOptions());
        }

        public static XDocument LoadHtml(TextReader reader, HtmlParserOptions options)
        {
            XDocument doc = new XDocument();
            LoadHtml(doc, reader, options);
            return doc;
        }

        internal static void LoadHtml(XDocument doc, TextReader reader, HtmlParserOptions options)
        {
            XDomBuilder dom = new XDomBuilder(doc);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            HtmlTextReader htmlTextReader = new HtmlTextReader(reader);
            parser.Parse(dom, htmlTextReader, options);
        }
        #endregion

        #region LoadHtmlFragment overloads

        public static void LoadHtmlFragment(XNode node, string html)
        {
            LoadHtmlFragment(node, new StringReader(html), new HtmlParserOptions());
        }

        public static void LoadHtmlFragment(XNode node, TextReader reader, HtmlParserOptions options)
        {
            XDomBuilder dom = new XDomBuilder(node);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            HtmlTextReader htmlTextReader = new HtmlTextReader(reader);
            parser.Parse(dom, htmlTextReader, options, InsersionMode.InBody);
        }

        #endregion

        #region LoadWebPageAsync overloads

        public static async Task<XDocument> LoadWebPageAsync(string url)
        {
            return await LoadWebPageAsync(url, new LoaderOptions());
        }

        public static async Task<XDocument> LoadWebPageAsync(string url, LoaderOptions options)
        {
            XDocument xhtmlDoc = new XDocument();
            await LoadWebPageAsync(xhtmlDoc, url, options);
            return xhtmlDoc;
        }

        internal static async Task LoadWebPageAsync(XDocument doc, string url, LoaderOptions options)
        {
            LoaderOptions optionsToUse = options == null ? new LoaderOptions() : options;
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

    }

    
}
#endif 
