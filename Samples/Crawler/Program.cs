using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHtmlKit;
using XHtmlKit.Query;
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
        public string OutputDir = "../../Output";
        public int Depth = 1;
        public bool StayOnDomain = true;
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Get crawler settings from the command-line
            Settings crawlerSettings = new Settings();

            // Get command-line settings - use XHtmlKit parser. Why not.
            if (args.Length > 0 )
            {
                XmlDocument settingsDoc = new XmlDocument();
                settingsDoc.LoadHtmlFragment("<settings " + string.Join(" ", args) + " />");
                XmlElement settings = settingsDoc.DocumentElement;

                crawlerSettings.Url = (settings.Attributes["Url"] != null && !string.IsNullOrWhiteSpace(settings.Attributes["Url"].Value)) ? settings.Attributes["Url"].Value.Trim() : crawlerSettings.Url;
                crawlerSettings.Depth = settings.Attributes["Depth"] != null  ?  Convert.ToInt32(settings.Attributes["Depth"].Value ) : crawlerSettings.Depth;
                crawlerSettings.StayOnDomain = settings.Attributes["StayOnDomain"] != null  ? Convert.ToBoolean(settings.Attributes["StayOnDomain"].Value) : crawlerSettings.StayOnDomain;
                crawlerSettings.OutputDir = (settings.Attributes["OutputDir"] != null && !string.IsNullOrWhiteSpace(settings.Attributes["OutputDir"].Value)) ? settings.Attributes["OutputDir"].Value.Trim() : crawlerSettings.OutputDir;
            }

            // Create 'todo' and 'done' lists
            Queue<Link> urlsToCrawl = new Queue<Link>();
            HashSet<string> urlsCrawled = new HashSet<string>();

            // Add the root url to the todo list
            urlsToCrawl.Enqueue(new Link { Url = crawlerSettings.Url });

            Console.WriteLine("Crawling: " + crawlerSettings.Url + ", Depth: " + crawlerSettings.Depth + ", OutputDir: " + crawlerSettings.OutputDir + ", StayOnDomain: " + crawlerSettings.StayOnDomain);

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
                    xhtmlDoc = XHtmlQueryEngine.LoadXHtmlDocAsync(currentUrl.Url).Result;
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

                // Save the file to disk

                // Get sub-links from the XHtml
                var subLinkElems = xhtmlDoc.SelectNodes("//a");

                // We are at the MaxLevel - we won't crawl deeper
                if (currentUrl.Depth >= crawlerSettings.Depth)
                    continue;

                // Add sub-links to the 'todo' list
                int subLinks = 0;
                foreach (XmlNode subLinkElem in subLinkElems)
                {
                    // Don't add empty links 
                    if (subLinkElem.Attributes["href"] == null || string.IsNullOrWhiteSpace(subLinkElem.Attributes["href"].InnerText))
                        continue;

                    // Get the sub-link
                    string sublink = subLinkElem.Attributes["href"].InnerText;
                    Uri subUri = new Uri(sublink.ToLower());
                    Uri baseUri = new Uri(crawlerSettings.Url.ToLower());

                    // Don't add links that are off the Domain
                    if (crawlerSettings.StayOnDomain && (subUri.Host != baseUri.Host))
                        continue;

                    // Don't add links that we have already crawled...
                    if (urlsCrawled.Contains(sublink.ToLower()))
                        continue;
    
                    // Add the sub-link
                    urlsToCrawl.Enqueue(new Link { Url = sublink, LinkText = subLinkElem.InnerText.Trim(), Depth = (currentUrl.Depth + 1) } );
                    subLinks++;
                }

                currentUrl.SubLinks = subLinks;
            }
        }
    }
}
