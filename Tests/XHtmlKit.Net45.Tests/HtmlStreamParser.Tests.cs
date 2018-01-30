using System;
using NUnit.Framework;
using XHtmlKit;
using System.Threading.Tasks;
using System.Xml;

namespace XHtmlKit.Parser.Tests
{
    [TestFixture]
    public class HtmlStreamParser_Tests
    {
        public static string ToFormattedString(XmlDocument doc)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            XmlWriterSettings writerSettings = new XmlWriterSettings() { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(sb, writerSettings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Basic html document.
        /// </summary>
        [Test]
        public void HelloWorldBasicTest()
        {
            string html = @"
            <html><body>
            <h1>Hello World</h1>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);
        }

        /// <summary>
        /// 'head' tags found inside the 'body' should be ignored.
        /// </summary>
        [Test]
        public void HeadTagInsideBodyTag()
        {
            string html = @"
            <html><body>
            <h1>Hello World</h1><p> para <head>somehead</head> end para </p>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the <head> inside the body tag is ingnored
            Assert.IsNull(doc.SelectSingleNode("//body/head"));
        }

        /// <summary>
        /// Since 'meta' tags are self-closing, and 'a' tags do not belong in the 
        /// 'head', the 'a' should get inserted in the body.
        /// </summary>
        [Test]
        public void HeadTagWithNestedTags()
        {
            string html = @"
            <html>
                <head>
                    <meta> <a> foobar </a> blah </meta>
                </head>
                <body>
            <h1>Hello World</h1>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the text under meta ignored, since meta is a self-closing tag
            Assert.IsNull(doc.SelectSingleNode("//head/meta/a"));
            // The <a> tag should go under the body
            Assert.IsNotNull(doc.SelectSingleNode("//body/a"));

        }

        [Test]
        public void LoadHtmlFragment()
        {
            string html = @"
                <html>
                <div>
                    <h1>Hello World</h1>
                    <body foo='bar'>
                </div></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtmlFragment(doc, html);

            Console.WriteLine(doc.OuterXml);

            // Ensure we are not inserting html, head or body nodes...
            Assert.AreEqual("div", doc.DocumentElement.Name);
        }

        [Test]
        public void LoadHtmlFragmentInSubElem()
        {
            string html = @"
                <div>
                    <h1>Hello World</h1>
                    <body foo='bar'>
                </div>
                <p>hello</p>
                </foo>abc";
            XmlDocument doc = new XmlDocument();
            XmlElement parent = doc.CreateElement("foo");
            doc.AppendChild(parent);
            HtmlParser.DefaultParser.LoadHtmlFragment(parent, html);

            Console.WriteLine( ToFormattedString(doc));

            // Ensure we are not inserting html, head or body nodes...
            // And ensure we can load the fragment
            // And ensure that the </foo> tag does not match our root node...
            Assert.AreEqual("foo", parent.Name);
            Assert.AreEqual(3, parent.ChildNodes.Count);
        }

        /// <summary>
        /// Attributes should be possible on the body tag
        /// </summary>
        [Test]
        public void BodyTagwithAttributes()
        {
            string html = @"
            <html>
                <head> 
                    <script src='/js/mobileredirect.js'></script>
                </head>
                <body class='foo'>
                    <h1>Hello World</h1>
                </body>
            </html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);

            Assert.AreEqual("foo", doc.SelectSingleNode("//body").Attributes["class"].Value);
        }

        [Test]
        public void ScriptRCDataParsing()
        {
            string html = @"            
            <html><body>
            <h1>Hello World</h1><script>
                ga('create', 'UA-40765809-1', {
                  'allowLinker': true,
                  'cookiePath': '/finance'
                });
                ga('send', 'pageview<table>');
            </script>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the </body> inside the script is treated as RCData...
            Assert.IsTrue(doc.SelectSingleNode("//script/text()").Value.Contains("<table>"));
        }

        [Test]
        public void TitleRCDataParsingCaps()
        {
            string html = @"            
            <html><head>
                <title>This is a Title!</TITLE>
                </head>
            <body>
            <h1>Hello World</h1>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the </TITLE> match is case insenstive...
            Assert.AreEqual("This is a Title!", doc.SelectSingleNode("//title/text()").Value);
        }

        /// <summary>
        /// When you supply a 'baseUrl' parameter, the parser should fully-qualify
        /// Urls... This test supplies a number of different Url formats.
        /// </summary>
        [Test]
        public void FullyQualifyUrls()
        {
            string html = @"            
            <html><body>
            <a id='1' href='//foobar.com'>hello</a>
            <a id='2' href='/helloworld.html'>hello</a>
            <a id='3' href='helloworld.html'>hello</a>
            <a id='4' href='http://blah.com/helloworld.html'>hello</a>
            <a id='5' href='../helloworld.html'>hello</a>
            <a id='6' href='/wiki/Wikipedia:Introduction'>hello</a>

            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html, "http://www.foobar.com/products/cat1/someprod.html");
            Console.WriteLine(doc.OuterXml);

            // Ensure the urls are fully qualified...
            Assert.AreEqual("http://foobar.com/", doc.SelectSingleNode("//a[@id='1']/@href").Value);
            Assert.AreEqual("http://www.foobar.com/helloworld.html", doc.SelectSingleNode("//a[@id='2']/@href").Value);
            Assert.AreEqual("http://www.foobar.com/products/cat1/helloworld.html", doc.SelectSingleNode("//a[@id='3']/@href").Value);
            Assert.AreEqual("http://blah.com/helloworld.html", doc.SelectSingleNode("//a[@id='4']/@href").Value);
            Assert.AreEqual("http://www.foobar.com/products/helloworld.html", doc.SelectSingleNode("//a[@id='5']/@href").Value);
            Assert.AreEqual("http://www.foobar.com/wiki/Wikipedia:Introduction", doc.SelectSingleNode("//a[@id='6']/@href").Value);

        }

        /// <summary>
        /// Look at mis-ordered formatting tags
        /// </summary>
        [Test]
        public void FormattingTags()
        {
            string html = @"            
            <html><body>
            <h1>Hello World</h1>
            Some text <b><i>italics</b></i>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);

            // The <b> tag should contain the <i> tag
            Assert.AreEqual("<b><i>italics</i></b>", doc.SelectSingleNode("//b").OuterXml);
        }

        /// <summary>
        /// A number of cases for weird or malformed html attributes.
        /// </summary>
        [Test]
        public void AttributeParsing()
        {
            string html = @"            
            <html>
	            <body>
		            <a id=01 /class/red >class red</a>
		            <a id=02 /class/= /red >class = red</a>
		            <a id=03 class// / = /red >class = red</a>
		            <a id=04    class / = /red >class = red</a>
		            <a id=05 class   = /red >class='/red'</a>
		            <a id=06 class= /red >class='/red'</a>
		            <a id=07 clas:s= red >clas:s='red'</a>
		            <a id=08 class:= red >class:='red'</a>
		            <a id=09 class= 'red'/ >class='red'</a>
		            <a id=10 class=red/ >class='red/'</a>
		            <a id=11 class=red/>class='red/'</a>
	            </body>
            </html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(ToFormattedString(doc));
        }

        /// <summary>
        /// Test putting fragments before and after the html tags. 
        /// </summary>
        [Test]
        public void BeforeAndAfterFragments()
        {
            string html = @"
            <!----- some comment ----->
            <html>
            <body>
                <h1>Hello World</h1>
                <p>Some_&nbsp;_text > hello &gt; &copy; &#169; <b><i>italics</b></i> 
                qrs
                </p>
            </body>
            </html>some after text";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the comment shows up at the beginning, and the text at the end...
            Assert.IsTrue(doc.FirstChild.NodeType == XmlNodeType.Comment);
            Assert.AreEqual("some after text", doc.SelectSingleNode("//body").LastChild.InnerText);
        }

        /// <summary>
        /// An html fragment should still parse into a proper html document.
        /// Ensure that 'head', and 'body' are properly constructed.
        /// </summary>
        [Test]
        public void NoOuterHtmlTag()
        {
            string html = @"
            <title>a title</title> <body>
            <h1>Hello World</h1>
            Some_&nbsp;_text > hello &gt; &copy; &#169; <b><i>italics</b></i>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);

            Console.WriteLine(ToFormattedString(doc));

            // Ensure the document gets constructed properly...
            Assert.IsTrue(doc.DocumentElement.Name == "html");
            Assert.IsTrue(doc.DocumentElement.FirstChild.Name == "head");
            Assert.IsTrue(doc.DocumentElement.FirstChild.FirstChild.Name == "title");

        }

        /// <summary>
        /// Ensure that the 'html' tag can have attributes...
        /// </summary>
        [Test]
        public void HtmlTagWithAttributes()
        {
            string html = @"
            <html lang='en'><body>
            <h1>Hello World</h1><html lang='fr' style='green'>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);

            Console.WriteLine(ToFormattedString(doc));

            // Ensure the nested <html> tag is used simply as a source of attributes on the 
            // main <html> tag - the 'lang' attribute should not overwrite the value, but the 'style'
            // attribute should get tacked on.
            Assert.IsTrue(doc.DocumentElement.Name == "html");
            Assert.AreEqual("en", doc.DocumentElement.Attributes["lang"].Value);
            Assert.AreEqual("green", doc.DocumentElement.Attributes["style"].Value);

        }

        /// <summary>
        /// Ensure that self-closing tags do not nest children. And
        /// ensure that a non self-closing tag, such as '<p/>' does not
        /// actually close.
        /// </summary>
        [Test]
        public void SelfClosingTags()
        {
            string html = @"           
            <html><body>
            <h1>Hello World</h1>
            Some text <br> Some more text <img src='foobar.jpg'> more text <hr><a>foo</a>
            <p/> non self-closing </p>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            HtmlParser.DefaultParser.LoadHtml(doc, html);

            Console.WriteLine(doc.OuterXml);

            // Ensure img node has attributes, and no children
            XmlNode imgNode = doc.SelectSingleNode("//img");
            Assert.AreEqual("foobar.jpg", imgNode.Attributes["src"].Value);
            Assert.IsNull(imgNode.FirstChild);

            // Ensure br node has no attributes, and no children
            XmlNode brNode = doc.SelectSingleNode("//br");
            Assert.AreEqual(0, brNode.Attributes.Count);
            Assert.IsNull(brNode.FirstChild);

            // Ensure hr node has no attributes, and no children
            XmlNode hrNode = doc.SelectSingleNode("//hr");
            Assert.AreEqual(0, hrNode.Attributes.Count);
            Assert.IsNull(hrNode.FirstChild);

            // Ensure p node has children, because it is non self-closing
            XmlNode pNode = doc.SelectSingleNode("//p");
            Assert.AreEqual(" non self-closing ", pNode.FirstChild.InnerText);

        }
    }


}
