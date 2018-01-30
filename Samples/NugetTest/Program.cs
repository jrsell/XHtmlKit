using System;
using System.Xml;
using XHtmlKit;

namespace NugetTest
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, "<html>hello world</html>");
            Console.WriteLine(doc.OuterXml);
        }
    }
}
