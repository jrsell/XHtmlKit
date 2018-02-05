using System;
using System.Collections.Generic;
using XHtmlKit;
using System.Xml;

namespace Crawler
{
    class Link
    {
        public string Url = "";
        public int Depth = 0;
        public string LinkText = "";
        public string PageTitle = "";
        public int SubLinks = 0;
    }

    class Settings
    {
        public string Url = "http://en.wikipedia.org";
        public string OutputDir = "Output";
        public string Encoding = null; 
        public int Depth = 0;
        public string UrlFilter = "";
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Crawl(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void Crawl(string[] args)
        { 
            // Get crawler settings from the command-line
            Settings crawlerSettings = new Settings();

            // Get command-line settings - use XHtmlKit parser. Why not.
            if (args.Length > 0 )
            {
                string settingsHtml = "<settings " + string.Join(" ", args) + " />";
                XmlDocument settingsDoc = new XmlDocument();
                HtmlParserImpl parser = new HtmlParserImpl();
                parser.ParseFragment(settingsDoc, settingsHtml);
                XmlElement settings = settingsDoc.DocumentElement;

                crawlerSettings.Url = (settings.Attributes["Url"] != null && !string.IsNullOrWhiteSpace(settings.Attributes["Url"].Value)) ? settings.Attributes["Url"].Value.Trim() : crawlerSettings.Url;
                crawlerSettings.Depth = settings.Attributes["Depth"] != null  ?  Convert.ToInt32(settings.Attributes["Depth"].Value ) : crawlerSettings.Depth;
                crawlerSettings.UrlFilter = (settings.Attributes["UrlFilter"] != null && !string.IsNullOrWhiteSpace(settings.Attributes["UrlFilter"].Value)) ? settings.Attributes["UrlFilter"].Value.Trim() : crawlerSettings.UrlFilter;
                crawlerSettings.OutputDir = (settings.Attributes["OutputDir"] != null && !string.IsNullOrWhiteSpace(settings.Attributes["OutputDir"].Value)) ? settings.Attributes["OutputDir"].Value.Trim() : crawlerSettings.OutputDir;
                crawlerSettings.Encoding = (settings.Attributes["Encoding"] != null && !string.IsNullOrWhiteSpace(settings.Attributes["Encoding"].Value)) ? settings.Attributes["Encoding"].Value.Trim() : crawlerSettings.Encoding;

                // See if we wish to override encoding settings...
                HtmlClient.Options.DetectEncoding = crawlerSettings.Encoding == null;
                HtmlClient.Options.DefaultEncoding = crawlerSettings.Encoding != null ? System.Text.Encoding.GetEncoding(crawlerSettings.Encoding) : HtmlClient.Options.DefaultEncoding;
            }

            // Create 'todo' and 'done' lists
            Queue<Link> urlsToCrawl = new Queue<Link>();
            HashSet<string> urlsCrawled = new HashSet<string>();
            Uri baseUri = new Uri(crawlerSettings.Url.ToLower());


            // Add the root url to the todo list
            urlsToCrawl.Enqueue(new Link { Url = crawlerSettings.Url });

            Console.WriteLine("Crawling Url = " + crawlerSettings.Url + " Depth = " + crawlerSettings.Depth + " OutputDir = '" + crawlerSettings.OutputDir + "' UrlFilter = '" + crawlerSettings.UrlFilter + "'");

            // Crawl all urls on the 'todo' list
            while (urlsToCrawl.Count > 0)
            {
                Link currentUrl = urlsToCrawl.Dequeue();

                Console.Write(currentUrl.Url);

                urlsCrawled.Add(currentUrl.Url.ToLower());

                // Crawl the Url using XHtmlKit
                XmlDocument xhtmlDoc;
                try
                {
                    xhtmlDoc = XHtmlLoader.LoadXmlDocumentAsync(currentUrl.Url).Result;
                    Console.WriteLine(", [OK]");                        
                }
                catch (Exception ex)
                {
                    Console.WriteLine(", [Error], " + (ex.InnerException != null ? ex.InnerException.Message : "") );
                    continue;
                }

                // Get title from the XHtml document
                var title = xhtmlDoc.SelectSingleNode("//title");
                if (title != null) {
                    currentUrl.PageTitle = title.InnerText.Trim();
                }

                // Save the XHtml file to disk
                try
                {
                    if (!string.IsNullOrWhiteSpace(crawlerSettings.OutputDir))
                    {
                        Uri currentUri = new Uri(currentUrl.Url.ToLower());
                        string fileName = currentUri.PathAndQuery.Trim();

                        // Replace invalid characters
                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c.ToString(), ".");
                        }
                        fileName = fileName.Trim(new char[] { '.' });

                        // Set default file name
                        if (string.IsNullOrWhiteSpace(fileName))
                            fileName = "default";

                        // Add xml extension
                        fileName = fileName + ".xml";

                        // Ensure output directory exists                        
                        string outputDir = crawlerSettings.OutputDir + "\\" + currentUri.Host;
                        if (!System.IO.Directory.Exists(outputDir))
                        {
                            System.IO.Directory.CreateDirectory(outputDir);
                        }

                        // Save file
                        xhtmlDoc.Save(outputDir + "\\" + fileName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving document: " + ex.Message);
                }

                // Get sub-links from the XHtml
                var subLinkElems = xhtmlDoc.SelectNodes("//a");

                // If we are at the Max Depth, we won't crawl deeper
                if (currentUrl.Depth >= crawlerSettings.Depth)
                    continue;

                // Add sub-links to the 'todo' list
                int numSubLinks = 0;
                foreach (XmlNode subLinkElem in subLinkElems)
                {
                    // Don't add empty links 
                    if (subLinkElem.Attributes["href"] == null || string.IsNullOrWhiteSpace(subLinkElem.Attributes["href"].InnerText))
                        continue;

                    // Get the sub-link
                    string sublink = subLinkElem.Attributes["href"].InnerText;
                    Uri subUri = new Uri(sublink.ToLower());

                    // Don't add links that don't match the UrlFilter
                    if (!string.IsNullOrWhiteSpace(crawlerSettings.UrlFilter) && (!sublink.Contains(crawlerSettings.UrlFilter)) )
                        continue;

                    // Don't add links that we have already crawled...
                    if (urlsCrawled.Contains(sublink.ToLower()))
                        continue;
    
                    // Add the sub-link
                    urlsToCrawl.Enqueue(new Link { Url = sublink, LinkText = subLinkElem.InnerText.Trim(), Depth = (currentUrl.Depth + 1) } );
                    numSubLinks++;
                }

                currentUrl.SubLinks = numSubLinks;

                // Todo - put the currentUrl metadata somewhere...
            }
        }
    }
}
