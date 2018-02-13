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
        XmlDocument doc = XHtmlLoader.LoadXmlDocumentAsync("http://wikipedia.org").Result;
        string title = doc.SelectSingleNode("//title/text()").Value;
        Console.WriteLine("Title is: " + title);
    }
}

~~~~
