using System;

namespace SampleScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get data
            Article[] articles = MyScraper.GetCodeProjectArticlesAsync().Result;

            // Do something with data
            foreach (Article a in articles)
            {
                Console.WriteLine(a.Date + ", " + a.Title + ", " + a.Rating);
            }
        }
    }
}
