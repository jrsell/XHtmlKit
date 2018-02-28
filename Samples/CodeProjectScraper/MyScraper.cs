using System.Collections.Generic;
using System.Xml;
using XHtmlKit;
using System.Text;
using System.Threading.Tasks;

namespace SampleScraper
{
    public static class MyScraper
    {
        /// <summary>
        /// Sample scraper
        /// </summary>
        public static async Task<Article[]> GetCodeProjectArticlesAsync(int pageNum = 1)
        {
            List<Article> results = new List<Article>();

            // Get web page as an XHtml document using XHtmlKit
            string url = "https://www.codeproject.com/script/Articles/Latest.aspx?pgnum=" + pageNum; 
            XmlDocument page = await XHtmlLoader.LoadWebPageAsync(url);

            // Select all articles using an anchor node containing a robust @class attribute
            var articles = page.SelectNodes("//table[contains(@class,'article-list')]/tr[@valign]");

            // Get each article
            foreach (XmlNode a in articles)
            {
                // Extract article data - we need to be aware that sometimes there are no results 
                // for certain fields
                var category = a.SelectSingleNode("./td[1]//a/text()");
                var title = a.SelectSingleNode(".//div[@class='title']/a/text()");
                var date = a.SelectSingleNode(".//div[contains(@class,'modified')]/text()");
                var rating = a.SelectSingleNode(".//div[contains(@class,'rating-stars')]/@title");
                var desc = a.SelectSingleNode(".//div[@class='description']/text()");
                var author = a.SelectSingleNode(".//div[contains(@class,'author')]/text()");
                XmlNodeList tagNodes = a.SelectNodes(".//div[@class='t']/a/text()");
                StringBuilder tags = new StringBuilder();
                foreach (XmlNode tagNode in tagNodes)
                    tags.Append((tags.Length > 0 ? "," : "") + tagNode.Value);

                // Create the data structure we want
                Article article = new Article
                {
                    Category = category != null ? category.Value : string.Empty,
                    Title = title != null ? title.Value : string.Empty,
                    Author = author != null ? author.Value : string.Empty,
                    Description = desc != null ? desc.Value : string.Empty,
                    Rating = rating != null ? rating.Value : string.Empty,
                    Date = date != null ? date.Value : string.Empty,
                    Tags = tags.ToString()
                };

                // Add to results
                results.Add(article);
            }
            return results.ToArray();
        }
    }
}
