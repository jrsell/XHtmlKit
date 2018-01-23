using System;
using System.Xml;
using System.IO;
using HtmlAgilityPack;
using XHtmlKit;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace ComparePerf
{
    class Program
    {
        static void Main(string[] args)
        {
            int iterations = 10;

            // Load the contents of all the sample files entirely into memory first, so that we are
            // testing only parsing speed, and no disk caching discrepencies come into play.
            string[] sampleFiles = Directory.EnumerateFiles("../../SampleHtml", "*.html").ToArray();
            string[] sampleFileContents = new string[sampleFiles.Length];
            for (int i = 0; i < sampleFiles.Length; i++)
                sampleFileContents[i] = File.ReadAllText(sampleFiles[i]);
    
            // Compare just raw parsing speed
            CompareParserPerformance(sampleFileContents, iterations);

            // Compare parsing, then an xpath search
            CompareParserPerformance(sampleFileContents, iterations, "//a");

        }

        static void CompareParserPerformance(string[] samples, int iterations, string xpath = null  )
        {
            Stopwatch sw = new Stopwatch();

            Console.WriteLine("Comparing parsers XHtmlKit and HtmlAgility " + (xpath == null ? " (parsing file in memory only)" : " (parsing file in memory and running xpath query: '" + xpath + "')") + " (" + iterations + " iterations)");

            // Compare just html parsing
            foreach (string sampleFileContents in samples)
            {
                string[] searchResults = new string[] { } ;
                int numChars = sampleFileContents.Length;

                Console.Write("File size: " + numChars);

                // Parser1: XHtmlKit
                sw.Start();                
                for (int i = 0; i < iterations; i++)
                    searchResults = XHtmlKit_ParseAndSearch(sampleFileContents, xpath);
                sw.Stop();
                Console.Write("\tXHtmlKit (ms):\t" + sw.ElapsedMilliseconds + (searchResults.Length > 0 ? "\t found: " + searchResults.Length : "\t"));

                // Parser2: tHtmlAgility
                sw.Start();
                for (int i = 0; i < iterations; i++)
                    searchResults = HtmlAgility_ParseAndSearch(sampleFileContents, xpath);
                sw.Stop();
                Console.Write("\tHtmlAgility (ms):\t" + sw.ElapsedMilliseconds + (searchResults.Length > 0 ? "\t found: " + searchResults.Length : "\t"));

                Console.Write("\n");
            }

            Console.Write("\n");
        }


        static string[] XHtmlKit_ParseAndSearch(string html, string xpath=null)
        {
            List<string> searchResults = new List<string>();
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);
            if (xpath != null)
            {
                var results = doc.DocumentElement.SelectNodes(xpath);
                foreach (XmlNode node in results)
                {
                    string result = node.InnerText;
                    searchResults.Add(result);
                }
            }
            return searchResults.ToArray();
        }


        static string[] HtmlAgility_ParseAndSearch(string html, string xpath = null)
        {
            List<string> searchResults = new List<string>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            if (xpath != null)
            {
                var results = doc.DocumentNode.SelectNodes(xpath);
                foreach (HtmlNode node in results)
                {
                    string result = node.InnerText;
                    searchResults.Add(result);
                }
            }
            return searchResults.ToArray();
        }

    }
}
