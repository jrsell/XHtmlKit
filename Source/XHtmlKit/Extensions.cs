#if !net20 && !net30
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Threading.Tasks;

namespace XHtmlKit.Extensions
{
    public static class XmlDocumentExtensions
    {
        public static void LoadHtml(this XmlDocument doc, string html)
        {
            XHtmlLoader.LoadHtml(doc, new StringReader(html), new HtmlParserOptions());
        }

        public static void LoadHtmlFragment(this XmlNode node, string html)
        {
            XHtmlLoader.LoadHtmlFragment(node, new StringReader(html), new HtmlParserOptions());
        }

        public static async Task LoadWebPageAsync(this XmlDocument doc, string url)
        {
            await XHtmlLoader.LoadWebPageAsync(doc, url, new LoaderOptions());
        }
    }

    public static class XDocumentExtensions
    {
        public static void LoadHtml(this XDocument doc, string html)
        {
            XHtmlLoaderX.LoadHtml(doc, new StringReader(html), new HtmlParserOptions());
        }

        public static void LoadHtmlFragment(this XNode node, string html)
        {
            XHtmlLoaderX.LoadHtmlFragment(node, new StringReader(html), new HtmlParserOptions());
        }

        public static async Task LoadWebPageAsync(this XDocument doc, string url)
        {
            await XHtmlLoaderX.LoadWebPageAsync(doc, url, new LoaderOptions());
        }
    }
        
}
#endif