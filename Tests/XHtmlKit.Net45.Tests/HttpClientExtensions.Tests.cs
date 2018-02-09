using System;
using NUnit.Framework;
using System.IO;
using System.Xml;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using XHtmlKit.Network;
using XHtmlKit.Query;

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

        [Test]
        public void Test_HtmlStream1()
        {
            int bufsize = 10;
            MemoryStream baseStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"));        
            HtmlStream htmlstream = new HtmlStream(baseStream, bufsize);
            byte[] buf = new byte[bufsize];

            // Test reading up to and past the cache size...
            // Initially, we cannot seek...
            Assert.AreEqual(false, htmlstream.CanSeek);
            htmlstream.Read(buf, 0, 5); // read first 5 bytes
            Assert.AreEqual(true, htmlstream.CanSeek);
            htmlstream.Read(buf, 5, 5); // read next 5 bytes
            // Here, we have now read enough data into the cache that we should be able to seek back to the start...
            Assert.AreEqual(true, htmlstream.CanSeek);
            htmlstream.Read(buf, 0, 10); // read next 10 bytes
            Assert.AreEqual(false, htmlstream.CanSeek);
        }

        [Test]
        public void Test_HtmlStream2()
        {
            int bufsize = 10;
            MemoryStream baseStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"));
            HtmlStream htmlstream = new HtmlStream(baseStream, bufsize);
            byte[] buf = new byte[bufsize];

            // Test reading up to cache size...
            Assert.AreEqual(false, htmlstream.CanRewind);
            htmlstream.Read(buf, 0, 10);
            Assert.AreEqual(true, htmlstream.CanRewind);

            // Seek to the beginning - after which we should no longer be able to seek
            htmlstream.Rewind();
            Assert.AreEqual(true, htmlstream.CanRewind);

            // Ensure the base stream is still in position (i.e. didn't get touched)
            Assert.AreEqual(10, baseStream.Position);

            // We should now reading from the cache
            byte[] buf2 = new byte[bufsize];
            htmlstream.Read(buf2, 0, 5);
            Assert.AreEqual("abcde", System.Text.Encoding.ASCII.GetString(buf2, 0, 5));

            // Base stream is still in position
            Assert.AreEqual(10, baseStream.Position);

            // If we are reading from cache, we should be able to seek to beginning again 
            Assert.AreEqual(true, htmlstream.CanRewind);
            htmlstream.Read(buf2, 5, 5);
            Assert.AreEqual("abcdefghij", System.Text.Encoding.ASCII.GetString(buf2));
            Assert.AreEqual(true, htmlstream.CanRewind);

            // Now move just past the cache size... We should no longer be able to seek to origin 
            htmlstream.Read(buf2, 0, 1);
            Assert.AreEqual(false, htmlstream.CanRewind);
        }

        /// <summary>
        /// Make sure if we are reading from the cache, and we try
        /// to read more than what exists in the cache, that we then
        /// start dipping into the real stream...
        /// </summary>
        [Test]
        public void Test_HtmlStream3()
        {
            int bufsize = 10;
            MemoryStream baseStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"));
            HtmlStream htmlstream = new HtmlStream(baseStream, bufsize);
            byte[] buf = new byte[bufsize];

            // Test reading up to cache size...
            Assert.AreEqual(false, htmlstream.CanRewind);
            htmlstream.Read(buf, 0, 10);
            Assert.AreEqual(true, htmlstream.CanRewind);

            // Seek to the beginning 
            htmlstream.Rewind();
            Assert.AreEqual(true, htmlstream.CanRewind);

            // Ensure the base stream is still in position (i.e. didn't get touched)
            Assert.AreEqual(10, baseStream.Position);

            // We should now reading from the cache, and in this case
            // a little bit of the real stream...
            byte[] buf2 = new byte[15];
            htmlstream.Read(buf2, 0, 15);
            Assert.AreEqual("abcdefghijklmno", System.Text.Encoding.ASCII.GetString(buf2));
            Assert.IsFalse(htmlstream.CanRewind, "CanRewind should be false, since we should be past the cache");

        }

        [Test]
        public void Test_HtmlStream4()
        {
            int bufsize = 10;
            MemoryStream baseStream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"));
            HtmlStream htmlstream = new HtmlStream(baseStream, bufsize);
            byte[] buf = new byte[15];

            // Test reading up to and beyond the cache size (we should still cache it all)...
            Assert.AreEqual(false, htmlstream.CanRewind);
            htmlstream.Read(buf, 0, buf.Length);
            Assert.AreEqual(true, htmlstream.CanRewind);

            // Seek to the beginning 
            htmlstream.Rewind();
            Assert.AreEqual(true, htmlstream.CanRewind);

            // Ensure the base stream is still in position (i.e. didn't get touched)
            Assert.AreEqual(15, baseStream.Position);

            // We should now reading fully from the cache
            byte[] buf2 = new byte[15];
            htmlstream.Read(buf2, 0, 15);
            Assert.AreEqual("abcdefghijklmno", System.Text.Encoding.ASCII.GetString(buf2));
            Assert.IsTrue(htmlstream.CanRewind, "CanRewind should be true, since we should have expanded our cache");
        }

        //[Test]
        //public void Test_shift_jis()
        //{
        //System.Text.Encoding enc = System.Text.Encoding.GetEncoding("shift_jis");
        //System.Text.Encoding enc2 = System.Text.Encoding.GetEncoding("Windows-31J");
        //bool mybreak = true;
        //}

        [Test]
        //[Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding01_iso_8859_1()
        {
            // ISO-8859-1: Example where the headers don't provide the encoding... The encoding should be found in the meta charset...
            await TestGetTextReaderAsync_ForEncoding("http://www.orange.fr/", "Orange : téléphones, forfaits, Internet, actualité, sport, video");
        }

        [Test]
        //[Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding01_big5()
        {
            // ISO-8859-1: Example where the headers don't provide the encoding... The encoding should be found in the meta charset...
            await TestGetTextReaderAsync_ForEncoding("http://Momoshop.com.tw", "momo購物網");
        }

        [Test]
        //[Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding_utf_8_withWrongDefault()
        {
            // Google supplies utf-8 in headers
            await TestGetTextReaderAsync_ForEncoding("http://google.com", "Google", new HtmlClientOptions {DefaultEncoding = System.Text.Encoding.ASCII } );

            // These guys don't - but they have it in the <meta>
            await TestGetTextReaderAsync_ForEncoding("http://Familydoctor.com.cn", "家庭医生在线_做中国专业的健康门户网站", new HtmlClientOptions { DefaultEncoding = System.Text.Encoding.ASCII });
        }

        [Test]
        public async Task DetectEncoding02_windows_1251()
        {
            // Windows-1251   ... note this page also has 'windows-1251' charset defined in meta charset...
            await TestGetTextReaderAsync_ForEncoding("http://kinozal.tv/", "Торрент трекер Кинозал.ТВ");
        }

        [Test]
        //[Ignore("Ignore until we can detect encodings from the <meta> tags of the document itself")]
        public async Task DetectEncoding03_shift_jis()
        {
            // Shift JIS    (shift_jis)... an example where the encoding does not come out of the headers or the BOM...
            await TestGetTextReaderAsync_ForEncoding("http://www.itmedia.co.jp/", "IT総合情報ポータル「ITmedia」Home");
            await TestGetTextReaderAsync_ForEncoding("http://kakaku.com/", "価格.com - 「買ってよかった」をすべてのひとに。");
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

        public async Task TestGetTextReaderAsync_ForEncoding(string url, string expectedTitle, HtmlClientOptions options = null)
        {
            HtmlClientOptions optionsToUse = options == null ? HtmlClient.Options : options;
            XmlDocument doc1 = new XmlDocument();

            System.Text.Encoding initialEncoding=null;
            EncodingConfidence initialConfidence = EncodingConfidence.Tentative;
            System.Text.Encoding finalEncoding = null;
            EncodingConfidence finalConfidence = EncodingConfidence.Tentative;

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader textReader = await HtmlClient.GetHtmlTextReaderAsync(url, optionsToUse)) {
                initialEncoding = textReader.CurrentEncoding;
                initialConfidence = textReader.CurrentEncodingConfidence;

                HtmlParser.DefaultParser.Parse(doc1, textReader, new HtmlParserOptions { BaseUrl = url } );

                finalEncoding = textReader.CurrentEncoding;
                finalConfidence = textReader.CurrentEncodingConfidence;
            }

            string title1 = doc1.SelectSingleNode("//title/text()").InnerText;

            Console.WriteLine("Crawled: " + url + ", title: " + title1 + ", default: " + optionsToUse.DefaultEncoding.WebName + " (detect=" + optionsToUse.DetectEncoding + "), inital: " + initialEncoding.WebName + " (" + initialConfidence + "), final: " + finalEncoding.WebName + " (" + finalConfidence + ")" );

            // Compare the titles of the pages to see if the encoding is picking up consistently between 
            Assert.AreEqual(expectedTitle, title1);
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
            using (HtmlTextReader reader = await HtmlClient.GetHtmlTextReaderAsync(url))
            {
                int c = 0;
                int charsRead = 0;
                while (true)
                {
                    c = reader.BaseReader.Read();
                    if (c < 0) break;
                    charsRead++;
                }

                return charsRead;
            }
        }

        static string _sampleMultiByteHtml = @"
            <html>
                <head>
                    <title>MultiByte</title>
                </head>
                <body>
                    <h1>This is a sample html file containing multi-byte characters, requiring encoding</h1>
                    <p>【楽天市場】</p>   
                    <p>Shopping is Entertainment! ： インターネット最大級の通信販売、通販オンラインショッピングコミュニティ</p>
                </body>
            </html>";

        [Test]
        public void CheckEncodingEquality()
        {
            System.Text.Encoding en1 = System.Text.Encoding.UTF8;
            System.Text.Encoding en2 = new System.Text.UTF8Encoding();
            System.Text.Encoding en3 = new System.Text.UTF8Encoding(true);
        }

        [Test]
        public void DecodeUtf8()
        {
            TestReadingEncodedFile("utf8.xml", new System.Text.UTF8Encoding(false), new System.Text.UTF8Encoding());
        }

        [Test]
        public void DecodeUtf8_BOM()
        {
            TestReadingEncodedFile("utf8_BOM.xml", new System.Text.UTF8Encoding(true), new System.Text.UTF8Encoding());
        }

        [Test]
        public void Decode_BigEndianUnicode()
        {
            TestReadingEncodedFile("BigEndianUnicode.xml", new System.Text.UnicodeEncoding(true, true), new System.Text.UTF8Encoding());
        }

        [Test]
        public void Decode_LittleEndianUnicode()
        {
            TestReadingEncodedFile("LittleEndianUnicode.xml", new System.Text.UnicodeEncoding(false, true), new System.Text.UTF8Encoding());
        }

        [Test]
        public void Decode_BigEndianUTF32()
        {
            TestReadingEncodedFile("BigEndianUTF32.xml", new System.Text.UTF32Encoding(true, true), new System.Text.UTF8Encoding());
        }

        [Test]
        public void Decode_LitteEndianUTF32()
        {
            TestReadingEncodedFile("LitteEndianUTF32.xml", new System.Text.UTF32Encoding(false, true), new System.Text.UTF8Encoding());
        }

        public void TestReadingEncodedFile(string fileName, System.Text.Encoding encoding, System.Text.Encoding defaultEncoding)
        {
            // Set some options, so that we can know if things are working...
            XHtmlLoaderOptions loaderOptions = new XHtmlLoaderOptions();
            loaderOptions.ClientOptions.DetectEncoding = true;
            loaderOptions.ClientOptions.DefaultEncoding = defaultEncoding;
            loaderOptions.ParserOptions.IncludeMetaData = true;

            // Load multi-byte html file into memory
            XmlDocument doc = XHtmlLoader.LoadXmlDocument(_sampleMultiByteHtml);

            // Ensure Sample directory exists
            string sampleDir = (new DirectoryInfo(AssemblyDirectory)).Parent.Parent.Parent.FullName + "\\SampleData\\";
            if (!Directory.Exists(sampleDir)) {
                Directory.CreateDirectory(sampleDir);
            }

            // Create Encoded file
            string fullName = sampleDir + fileName;
            using (TextWriter sw = new StreamWriter(fullName, false, encoding) ) 
            {
                doc.Save(sw);
            }

            // Re-load into memory
            XmlDocument doc2 = XHtmlLoader.LoadXmlDocumentAsync("file://" + fullName, loaderOptions).Result;
            Console.WriteLine("Reading file: " + fileName);
            Console.WriteLine(doc2.OuterXml);
            Assert.AreEqual(doc.SelectSingleNode("//body").OuterXml, doc2.SelectSingleNode("//body").OuterXml);
        }


        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

    }
}
