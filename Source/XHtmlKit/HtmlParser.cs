using System.IO;
using System.Xml;

namespace XHtmlKit
{
    public abstract class HtmlParser
    {
        public abstract void LoadHtml(XmlDocument doc, string html, string originatingUrl = null);
        public abstract void LoadHtml(XmlDocument doc, TextReader htmlTextReader, string originatingUrl = null);

        private static HtmlParser _parser = new HtmlStreamParser();
        public static HtmlParser DefaultParser
        {
            get { return _parser; }
            set { _parser = value; }
        }
    }
}
