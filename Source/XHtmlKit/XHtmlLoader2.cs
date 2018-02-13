#if !net20

using System.Xml;
using System.Threading.Tasks;
using XHtmlKit.Network;

namespace XHtmlKit
{

    public static partial class XHtmlLoader
    {
        public static async Task<XmlDocument> LoadWebPageAsync(string url)
        {
            return await LoadWebPageAsync(url, new LoaderOptions());
        }

        public static async Task<XmlDocument> LoadWebPageAsync(string url, LoaderOptions options)
        {
            XmlDocument xhtmlDoc = new XmlDocument();
            await LoadWebPageAsync(xhtmlDoc, url, options);
            return xhtmlDoc;
        }

        internal static async Task LoadWebPageAsync(XmlDocument doc, string url, LoaderOptions options)
        {
            LoaderOptions optionsToUse = options == null ? new LoaderOptions() : options;
            optionsToUse.ParserOptions.BaseUrl = string.IsNullOrEmpty(optionsToUse.ParserOptions.BaseUrl) ? url : optionsToUse.ParserOptions.BaseUrl;

            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParser<XmlNode> parser = new HtmlStreamParser<XmlNode>();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url, optionsToUse))
                parser.Parse(dom, htmlReader, optionsToUse.ParserOptions);
        }
    }

   
        
}
#endif 
