using System.Xml;
using System.IO;

#if !net20
using System.Xml.Linq;
using XHtmlKit.Network;
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
            HtmlTextReader reader = new HtmlTextReader(html);
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
            HtmlTextReader reader = new HtmlTextReader(htmlTextReader);
            parser.Parse(dom, reader, options);
            return doc;
        }

#if !net20
        public static async System.Threading.Tasks.Task<XmlDocument> LoadXmlDocumentAsync(string url)
        {
            return await LoadXmlDocumentAsync(url, new HtmlParserOptions());
        }

        public static async System.Threading.Tasks.Task<XmlDocument> LoadXmlDocumentAsync(string url, HtmlParserOptions options)
        {
            HtmlParserOptions optionsToUse = options == null ? new HtmlParserOptions() : options;
            optionsToUse.BaseUrl = string.IsNullOrEmpty(optionsToUse.BaseUrl) ? url : optionsToUse.BaseUrl;

            XmlDocument xhtmlDoc = new XmlDocument();
            XmlDomBuilder dom = new XmlDomBuilder(xhtmlDoc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url))
                parser.Parse(dom, htmlReader, optionsToUse);

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
            HtmlTextReader reader = new HtmlTextReader(html);
            HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
            parser.Parse(dom, reader, options);
            return doc;
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
            return await LoadXDocumentAsync(url, new HtmlParserOptions());
        }

        public static async System.Threading.Tasks.Task<XDocument> LoadXDocumentAsync(string url, HtmlParserOptions options)
        {
            HtmlParserOptions optionsToUse = options == null ? new HtmlParserOptions() : options;
            optionsToUse.BaseUrl = string.IsNullOrEmpty(optionsToUse.BaseUrl) ? url : optionsToUse.BaseUrl;

            XDocument xhtmlDoc = new XDocument();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url))
            {
                XDomBuilder dom = new XDomBuilder(xhtmlDoc);
                HtmlStreamParser<XNode> parser = new HtmlStreamParser<XNode>();
                parser.Parse(dom, htmlReader, optionsToUse);
            }
            return xhtmlDoc;
        }

#endif

    }
}
