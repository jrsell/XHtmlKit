using System;
using System.Xml;
using XHtmlKit;

namespace NugetTest
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument doc = XHtmlLoader.LoadXmlDocument("<html>hello world</html>");
            Console.WriteLine(doc.OuterXml);
        }
    }
}
