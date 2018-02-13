# XHtmlKit
A lightweight Html parser for use with native XmlDocument, and XDocument. 

XHtmlKit was inspired by the HtmlAgilityPack project, however, I wanted to manipulate results in a native XmlDocument without requiring conversion. Additionally, I didn't want to have to load the entire contents of an Html file into memory before parsing. 

XHtmlKit is a true Stream parser - extremely fast, and memory efficient. Initial tests show performance approximately 3 times faster than HtmlAgilityPack for all file sizes. It works equally well with both XmlDocument and XDocument. 

Here's how you use it: 

~~~~
using System;
using System.Xml;
using XHtmlKit;

class Program
{
    static void Main(string[] args)
    {
        // Load Html string into an XmlDocument 
        XmlDocument doc1 = XHtmlLoader.LoadHtml("<html><head><title>Hello World!</title><body><h1>Hello World</h1><p>This is a test</body>");
        Console.WriteLine("OuterXml is: " + doc1.OuterXml);

        // Load web page into an XmlDocument 
        XmlDocument doc2 = XHtmlLoader.LoadWebPageAsync("http://wikipedia.org").Result;
        string title = doc2.SelectSingleNode("//title").InnerText;
        Console.WriteLine("Title is: " + title);
    }
}

~~~~

XHtmlKit is a [nuget package](https://www.nuget.org/packages/XHtmlKit/) that can be downloaded from the Nuget site.


