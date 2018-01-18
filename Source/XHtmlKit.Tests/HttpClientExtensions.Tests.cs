using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Xml;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace XHtmlKit.Network.Tests
{
    [TestClass]
    public class HttpClientExtensions_Tests
    {
        private static HttpClient _client;
        public static HttpClient HttpClient
        {
            get
            {
                if (_client == null)
                {
                    _client = new HttpClient();
                }
                return _client;
            }
        }

        /// <summary>
        /// Ensure that using HttpClient.GetTextReaderAsync()
        /// returns the same number of characters as using 
        /// HttpClient.GetStringAsync()... This should be the case if
        /// the two are detecting the Encoding the same way...
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestDownloadMethodConsistency()
        {
            string[] urls = new string[] {
                "http://Namazu.org",
                "http://Brockenwheels.com",
                "http://Collabtech.org",
                "http://Wda.jp",
                "http://www.Cruillaconnecta.cat",
                "http://Caninechronicleshowcalendar.com"
            };

            foreach (string url in urls)
            {
                await TestGetTextReaderAsync_ForEncoding(url);
            }
        }

        public async Task TestGetTextReaderAsync_ForEncoding(string url)
        {
            string s1;
            using (TextReader reader = await HttpClient.GetTextReaderAsync(url))
            {
                s1 = reader.ReadToEnd();
            }
            string s2 = await HttpClient.GetStringAsync(url);
            Assert.AreEqual(s1.Length, s2.Length);
            Console.WriteLine("Compared: " + url + ", len: " + s1.Length);
        }

        /// <summary>
        /// This test shows that streamed reading from the network is neither
        /// faster, nor slower than reading the entire page into memory before
        /// reading the contents. At the very least, streamed reading offers the 
        /// very real benefit that it consumes less memory.
        /// </summary>        
        public async Task TestPerfOfStreamedReading()
        {
            int iters = 2;
            string[] urls = new string[] {
                "http://Namazu.org",
                "http://Brockenwheels.com",
                "http://Collabtech.org",
                "http://Wda.jp",
                "http://www.Cruillaconnecta.cat",
                "http://Caninechronicleshowcalendar.com"
            };

            await TestPerfOfNonStreamedReading(iters, urls);
            await TestPerfOfStreamedReading(iters, urls);
            await TestPerfOfNonStreamedReading(iters, urls);
            await TestPerfOfStreamedReading(iters, urls);
            await TestPerfOfNonStreamedReading(iters, urls);
            await TestPerfOfStreamedReading(iters, urls);
            await TestPerfOfNonStreamedReading(iters, urls);
            await TestPerfOfStreamedReading(iters, urls);
            await TestPerfOfNonStreamedReading(iters, urls);
            await TestPerfOfStreamedReading(iters, urls);
            await TestPerfOfNonStreamedReading(iters, urls);
        }

        public async Task TestPerfOfStreamedReading(int iters, string[] urls)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

            // GetAsStringAsync
            List<int> charsRead1List = new List<int>();
            stopWatch.Restart();
            for (int i = 0; i < iters; i++)
            {
                foreach (string url in urls)
                {
                    int charsread = await DownloadPageUsingGetAsTextReaderAsync(url + "?nocache=" + Guid.NewGuid().ToString());
                    charsRead1List.Add(charsread);
                    //Console.WriteLine(charsread + "\t" + url);
                }
            }
            stopWatch.Stop();
            Console.WriteLine("GetAsTextReaderAsync: time: " + stopWatch.ElapsedMilliseconds + ", iters: " + iters);
        }

        public async Task TestPerfOfNonStreamedReading(int iters, string[] urls)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

            // GetAsStringAsync
            List<int> charsRead1List = new List<int>();
            stopWatch.Restart();
            for (int i = 0; i < iters; i++)
            {
                foreach (string url in urls)
                {
                    int charsread = await DownloadPageUsingGetAsStringAsync(url + "?nocache=" + Guid.NewGuid().ToString());
                    charsRead1List.Add(charsread);
                    //Console.WriteLine(charsread + "\t" + url);
                }
            }
            stopWatch.Stop();
            Console.WriteLine("GetAsStringAsync: time: " + stopWatch.ElapsedMilliseconds + ", iters: " + iters);
        }

        public async Task<int> DownloadPageUsingGetAsStringAsync(string url)
        {
            string contents = await HttpClient.GetStringAsync(url);
            using (TextReader reader = new StringReader(contents))
            {
                int c = 0;
                int charsRead = 0;
                while (true) {
                    c = reader.Read();
                    if (c < 0) break;
                    charsRead++;
                }
                return charsRead;
            }
        }

        public async Task<int> DownloadPageUsingGetAsTextReaderAsync(string url)
        {
            using (TextReader reader = await HttpClient.GetTextReaderAsync(url))
            {
                int c = 0;
                int charsRead = 0;
                while (true)
                {
                    c = reader.Read();
                    if (c < 0) break;
                    charsRead++;
                }

                return charsRead;
            }
        }
    }
}
