using System.IO;
using System.Xml;

namespace XHtmlKit
{
    public abstract class HtmlParser
    {
        public abstract void LoadHtml(XmlDocument doc, string html, string baseUrl = null);
        public abstract void LoadHtml(XmlDocument doc, TextReader htmlTextReader, string baseUrl = null);
        public abstract void LoadHtmlFragment(XmlNode rootNode, string html, string baseUrl = null);
        public abstract void LoadHtmlFragment(XmlNode rootNode, TextReader htmlTextReader, string baseUrl = null);

        private static HtmlParser _parser = new HtmlStreamParser();
        public static HtmlParser DefaultParser
        {
            get { return _parser; }
            set { _parser = value; }
        }
    }

    
}
