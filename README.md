# XHtmlKit
A lightweight Html parser for use with native XmlDocument, and XDocument. Fast, memory efficient, tolerant of malformed Html, allows parsing from a stream, and has built-in encoding detection.

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
