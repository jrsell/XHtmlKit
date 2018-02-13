using System.Xml;
using System.Xml.Linq;
using XHtmlKit;

class Program
{
    static void Main(string[] args)
    {
        // Load Html string into an XmlDocument 
        XmlDocument doc1 = XHtmlLoader.LoadHtml("<html><head><title>Hello World!</title><body><h1>Hello World</h1><p>This is a test</body>");

        // Load web page into an XmlDocument
        XmlDocument doc2 = XHtmlLoader.LoadWebPageAsync("http://wikipedia.org").Result;

        // Load Html string into an XDocument 
        XDocument doc4 = XHtmlLoaderX.LoadHtml("<html><head><title>Hello World!</title><body><h1>Hello World</h1><p>This is a test</body>");

        // Load web page into an XDocument
        XDocument doc5 = XHtmlLoaderX.LoadWebPageAsync("http://wikipedia.org").Result;
    }
}
