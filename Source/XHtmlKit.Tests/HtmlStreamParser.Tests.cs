using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XHtmlKit.Parser;
using System.Threading.Tasks;
using System.Xml;

namespace XHtmlKit.Parser.Tests
{
    [TestClass]
    public class HtmlStreamParser
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

        [TestMethod]
        public void Test_HelloWorld()
        {
            string html = @"
            <html><body>
            <h1>Hello World</h1>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);
            Console.WriteLine(doc.OuterXml);
        }

        [TestMethod]
        public void Test_HeadInBody()
        {
            string html = @"
            <html><body>
            <h1>Hello World</h1><p> para <head>somehead</head> end para </p>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the <head> inside the body tag is ingnored
            Assert.IsNull(doc.SelectSingleNode("//body/head"));
        }

        [TestMethod]
        public void Test_HeadWithNested()
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
            doc.LoadHtml(html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the text under meta ignored, since meta is a self-closing tag
            Assert.IsNull(doc.SelectSingleNode("//head/meta/a"));
            // The <a> tag should go under the body
            Assert.IsNotNull(doc.SelectSingleNode("//body/a"));

        }



        [TestMethod]
        public void Test_BodywithAttributes()
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
            doc.LoadHtml(html);
            Console.WriteLine(doc.OuterXml);

            Assert.AreEqual("foo", doc.SelectSingleNode("//body").Attributes["class"].Value);
        }

        [TestMethod]
        public void Test_Script()
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
            doc.LoadHtml(html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the </body> inside the script is treated as RCData...
            Assert.IsTrue(doc.SelectSingleNode("//script/text()").Value.Contains("<table>"));
        }


        [TestMethod]
        public void Test_FormattingTags()
        {
            string html = @"            
            <html><body>
            <h1>Hello World</h1>
            Some text <b><i>italics</b></i>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);
            Console.WriteLine(doc.OuterXml);

            // The <b> tag should contain the <i> tag
            Assert.AreEqual("<b><i>italics</i></b>", doc.SelectSingleNode("//b").OuterXml);
        }

        [TestMethod]
        public void Test_Attributes()
        {
            string html = @"            
            <html>
	            <body>
		            <a id=01 /class/red >class red</a>
		            <a id=02 /class/= /red >class = red</a>
		            <a id=03 class// / = /red >class = red</a>
		            <a id=04    class / = /red >class = red</a>
		            <a id=05 class   = /red >class='red'</a>
                    <a id=06 class= /red >class='red'</a>
		            <a id=07 clas:s= red >class:='red'</a>
		            <a id=07 class:= red >class:='red'</a>
                    <a id=08 class= 'red'/ >class='red'</a>
		            <a id=09 class=red/ >class='red/'</a>
		            <a id=10 class=red/>class='red'</a>
	            </body>
            </html>";
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);
            Console.WriteLine(ToFormattedString(doc));

            // The <b> tag should contain the <i> tag
            // Assert.AreEqual("<b><i>italics</i></b>", doc.SelectSingleNode("//b").OuterXml);
        }

        [TestMethod]
        public void Test_BeforeAndAfterStuff()
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
            doc.LoadHtml(html);
            Console.WriteLine(doc.OuterXml);

            // Ensure the comment shows up at the beginning, and the text at the end...
            Assert.IsTrue(doc.FirstChild.NodeType == XmlNodeType.Comment);
            Assert.AreEqual("some after text", doc.SelectSingleNode("//body").LastChild.InnerText);
        }

        [TestMethod]
        public void Test_NoHeadTag()
        {
            string html = @"
            <title>a title</title> <body>
            <h1>Hello World</h1>
            Some_&nbsp;_text > hello &gt; &copy; &#169; <b><i>italics</b></i>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);

            // Ensure the document gets constructed properly...
            Assert.IsTrue(doc.DocumentElement.Name == "html");
            Assert.IsTrue(doc.DocumentElement.FirstChild.Name == "head");
            Assert.IsTrue(doc.DocumentElement.FirstChild.FirstChild.Name == "title");

            Console.WriteLine(doc.OuterXml);
        }

        [TestMethod]
        public void Test_HtmlWithAttributes()
        {
            string html = @"
            <html lang='en'><body>
            <h1>Hello World</h1><html lang='fr' style='green'>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);

            Console.WriteLine(doc.OuterXml);

            // Ensure the nested <html> tag is used simply as a source of attributes on the 
            // main <html> tag - the 'lang' attribute should not overwrite the value, but the 'style'
            // attribute should get tacked on.
            Assert.IsTrue(doc.DocumentElement.Name == "html");
            Assert.AreEqual("en", doc.DocumentElement.Attributes["lang"].Value);
            Assert.AreEqual("green", doc.DocumentElement.Attributes["style"].Value);

        }

        [TestMethod]
        public void Test_SelfClosingTags()
        {
            string html = @"           
            <html><body>
            <h1>Hello World</h1>
            Some text <br> Some more text <img src='foobar.jpg'> more text <hr><a>foo</a>
            <p/> non self-closing </p>
            </body></html>";
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(html);

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
