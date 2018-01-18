using System.Xml;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using XHtmlKit.Network;

namespace XHtmlKit.Parser
{
    public static class HtmlParserExtensions
    {

        public static void LoadHtml(this XmlDocument doc, string html, string originatingUrl = null)
        {
            HtmlParser.DefaultParser.LoadHtml(doc, html, originatingUrl);
        }

        public static void LoadHtml(this XmlDocument doc, TextReader htmlTextReader, string originatingUrl = null)
        {
            HtmlParser.DefaultParser.LoadHtml(doc, htmlTextReader, originatingUrl);
        }

        public static async Task LoadWebPageAsync(this XmlDocument doc, string url)
        {
            using (HttpClient httpClient = new HttpClient())
            using (TextReader htmlReader = await httpClient.GetTextReaderAsync(url))
            {
                HtmlParser.DefaultParser.LoadHtml(doc, htmlReader, url);
            }
        }
    }
    
}
