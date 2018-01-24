using System.Xml;
using System.IO;

namespace XHtmlKit
{
    public static class HtmlParserExtensions
    {

        public static void LoadHtml(this XmlDocument doc, string html, string baseUrl = null)
        {
            HtmlParser.DefaultParser.LoadHtml(doc, html, baseUrl);
        }

        public static void LoadHtml(this XmlDocument doc, TextReader htmlTextReader, string baseUrl = null)
        {
            HtmlParser.DefaultParser.LoadHtml(doc, htmlTextReader, baseUrl);
        }

        public static void LoadHtmlFragment(this XmlNode rootNode, string html, string baseUrl = null)
        {
            HtmlParser.DefaultParser.LoadHtmlFragment(rootNode, html, baseUrl);
        }

        public static void LoadHtmlFragment(this XmlNode rootNode, TextReader htmlTextReader, string baseUrl = null)
        {
            HtmlParser.DefaultParser.LoadHtmlFragment(rootNode, htmlTextReader, baseUrl);
        }

        
    }

}
