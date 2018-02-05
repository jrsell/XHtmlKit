using System;
using NUnit.Framework;
using System.IO;
using System.Xml;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using XHtmlKit;

namespace XHtmlKit.Network.Tests
{
    [TestFixture]
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

        [Test]
        public void ParseCharset()
        {
            string[] content = new string[] {
                "text/html; charset=ISO-8859-1",
                "text/html; charset =ISO-8859-1",
                "text/html; charset= ISO-8859-1",
                "text/html; charset= ISO-8859-1 ",
                "text/html; charset= 'ISO-8859-1' ",
                "text/html; charset= ' ISO-8859-1 '  ",
                "text/html; charset= \" ISO-8859-1 '  ",
                "text/html; charset= ' ISO-8859-1 \"  ",
                 "text/html; charset= \"ISO-8859-1\" "
            };
            foreach (string input in content)
            {
                var match = System.Text.RegularExpressions.Regex.Match(input, "charset\\s*=[\\s\"']*([^\\s\"' />]*)");
                Assert.IsTrue(match.Success);
                Assert.AreEqual("ISO-8859-1", match.Groups[1].Value);

            }
        }

        //[Test]
        //public void Test_shift_jis()
        //{
            //System.Text.Encoding enc = System.Text.Encoding.GetEncoding("shift_jis");
            //System.Text.Encoding enc2 = System.Text.Encoding.GetEncoding("Windows-31J");
            //bool mybreak = true;
        //}

        [Test]
        [Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding01_iso_8859_1()
        {
            // ISO-8859-1: Example where the headers don't provide the encoding... The encoding should be found in the meta charset...
            await TestGetTextReaderAsync_ForEncoding("http://www.orange.fr/", "Orange : téléphones, forfaits, Internet, actualité, sport, video");
        }

        [Test]
        [Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding01_big5()
        {
            // ISO-8859-1: Example where the headers don't provide the encoding... The encoding should be found in the meta charset...
            await TestGetTextReaderAsync_ForEncoding("http://Momoshop.com.tw", "Orange : téléphones, forfaits, Internet, actualité, sport, video");
        }

        [Test]
        public async Task DetectEncoding02_windows_1251()
        {
            // Windows-1251   ... note this page also has 'windows-1251' charset defined in meta charset...
            await TestGetTextReaderAsync_ForEncoding("http://kinozal.tv/", "Торрент трекер Кинозал.ТВ");
        }

        [Test]
        [Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding03_shift_jis()
        {
            // Shift JIS    (shift_jis)... an example where the encoding does not come out of the headers or the BOM...
            await TestGetTextReaderAsync_ForEncoding("http://www.itmedia.co.jp/", "IT総合情報ポータル「ITmedia」Home");
        }

        [Test]
        [Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding03_shift_jis_2()
        {
            // Shift JIS    (shift_jis)... an example where the encoding does not come out of the headers or the BOM...
            await TestGetTextReaderAsync_ForEncoding("http://kakaku.com/", "Unknown...");
        }
        

        [Test]
        //[Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding04_Windows_1252()
        {
            // Windows-1252    
            await TestGetTextReaderAsync_ForEncoding("https://www.usps.com/", "Welcome | USPS");
        }

        [Test]
        public async Task DetectEncoding05_GB2312()
        {
            // GB2312   
            await TestGetTextReaderAsync_ForEncoding("http://www.qq.com/", "腾讯首页");
        }

        [Test]
        public async Task DetectEncoding06_EUC_KR()
        {
            // EUC-KR
            await TestGetTextReaderAsync_ForEncoding("http://gmarket.co.kr/", "G마켓 - 쇼핑을 다 담다.");
        }
        
        [Test]
        public async Task DetectEncoding07_EUC_JP()
        {
            // EUC-JP
            await TestGetTextReaderAsync_ForEncoding("https://www.rakuten.co.jp/", "【楽天市場】Shopping is Entertainment! ： インターネット最大級の通信販売、通販オンラインショッピングコミュニティ");
        }

        public async Task TestGetTextReaderAsync_ForEncoding(string url, string expectedTitle)
        {
            // Method 1 - use GetStringAsync() which does encoding detection...
            //string s2 = await HttpClient.GetStringAsync(url);
            //XmlDocument doc2 = new XmlDocument();
            //doc2.LoadHtml(s2);
            //string title2 = doc2.SelectSingleNode("//title/text()").InnerText;

            // Method 2 - use our GetTextReaderAsync() which also does encoding detetion...
            XHtmlQueryEngine engine = new XHtmlQueryEngine();
            XmlDocument doc1 = new XmlDocument();

            System.Text.Encoding initialEncoding=null;
            //EncodingConfidence initialConfidence = EncodingConfidence.Tentative;
            System.Text.Encoding finalEncoding = null;
            //EncodingConfidence finalConfidence = EncodingConfidence.Tentative;

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (TextReader htmlReader = await HtmlClient.GetTextReaderAsync(url)) {

                StreamReader streamReader = (StreamReader)htmlReader;
                initialEncoding = streamReader.CurrentEncoding;

                HtmlParser.DefaultParser.Parse(doc1, htmlReader, new HtmlParserOptions { BaseUrl = url } );

                finalEncoding = streamReader.CurrentEncoding;
                //finalConfidence = htmlReader.CurrentEncodingConfidence;
            }

            string title1 = doc1.SelectSingleNode("//title/text()").InnerText;

            Console.WriteLine("Crawled: " + url + ", title: " + title1 + ", inital: " + initialEncoding.WebName + ", final: " + finalEncoding.WebName  );

            // Compare the titles of the pages to see if the encoding is picking up consistently between 
            // GetStringAsync and GetTextReaderAsync
            Assert.AreEqual(expectedTitle, title1);

            // Sanity check - compare against what we get from GetStringAsync()...
            // Assert.AreEqual(title2, title1);

            //Assert.AreEqual(s1.Length, s2.Length);
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
            using (TextReader reader = await HtmlClient.GetTextReaderAsync(url))
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
