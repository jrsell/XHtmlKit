using System.Xml;
using System.IO;

#if !net20
using System.Xml.Linq;
#endif 

namespace XHtmlKit
{
    public class XHtmlLoader
    {
        public static XmlDocument LoadXmlDocument(string html)
        {
            return LoadXmlDocument(html, new HtmlParserOptions());
        }

        public static XmlDocument LoadXmlDocument(string html, HtmlParserOptions options)
        {
            XmlDocument doc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            TextReader reader = new StringReader(html);
            parser.Parse(dom, reader, options);
            return doc;
        }

        public static XmlDocument LoadXmlDocument(TextReader htmlTextReader)
        {
            return LoadXmlDocument(htmlTextReader, new HtmlParserOptions() );
        }

        public static XmlDocument LoadXmlDocument(TextReader htmlTextReader, HtmlParserOptions options)
        {
            XmlDocument doc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();
            parser.Parse(dom, htmlTextReader, options);
            return doc;
        }

#if !net20

        public static async System.Threading.Tasks.Task<XmlDocument> LoadXmlDocumentAsync(string url)
        {
            XmlDocument xhtmlDoc = new XmlDocument();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (TextReader htmlReader = await HtmlClient.GetTextReaderAsync(url))
                HtmlParser.DefaultParser.Parse(xhtmlDoc, htmlReader, new HtmlParserOptions { BaseUrl = url });

            return xhtmlDoc;
        }

        public static XDocument LoadXDocument(string html)
        {
            return LoadXDocument(html, new HtmlParserOptions());
        }

        public static XDocument LoadXDocument(string html, HtmlParserOptions options)
        {
            XDocument doc = new XDocument();
            XDomBuilder dom = new XDomBuilder(doc);
            TextReader reader = new StringReader(html);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            parser.Parse(dom, reader, options);
            return doc;
        }

        public static XDocument LoadXDocument(TextReader htmlTextReader)
        {
            return LoadXDocument(htmlTextReader, new HtmlParserOptions());
        }

        public static XDocument LoadXDocument(TextReader htmlTextReader, HtmlParserOptions options)
        {
            XDocument doc = new XDocument();
            XDomBuilder dom = new XDomBuilder(doc);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            parser.Parse(dom, htmlTextReader, options);
            return doc;
        }

        public static async System.Threading.Tasks.Task<XDocument> LoadXDocumentAsync(string url)
        {
            XDocument xhtmlDoc = new XDocument();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (TextReader htmlReader = await HtmlClient.GetTextReaderAsync(url))
            {
                XDomBuilder dom = new XDomBuilder(xhtmlDoc);
                HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
                parser.Parse(dom, htmlReader, new HtmlParserOptions());
            }
            return xhtmlDoc;
        }

#endif

    }
}
