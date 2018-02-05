using System;
using System.IO;
using NUnit.Framework;
using System.Xml;
using System.Threading.Tasks;
using XHtmlKit;

namespace XHtmlKit.Tests
{
    [TestFixture]
    public class QueryEngine_Tests
    {
        private static XHtmlQueryEngine _queryEngine;
        public static XHtmlQueryEngine QueryEngine
        {
            get
            {
                if (_queryEngine == null)
                {
                    _queryEngine = new XHtmlQueryEngine();
                }
                return _queryEngine;
            }
        }

        public QueryEngine_Tests()
        {
            // Configure default parser to use HtmlAgilityPack
            // HtmlParser.DefaultParser = new Parser.HtmlAgility.HtmlAgilityParser();
        }

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

        public static string _testHTML = @"
                <!-- This is a comment -->
                <HTML>
                    <head>
                        <title>This is a title</title>
                    </head>
                    <body>
                        <div class='This & is a test' id='ThisIsAnID123'></div>
                        <h1>This is a title</h1><br>
                        <p> The quick brown <a href='www.google.com'>Google & Boogle</a> fox jumped</p>
                        <p> The quick brown <a href='www.yahoo.com?q=123&v=1'>Yahoo</a> fox jumped</p>
                        </body>
                </HTML>
                ";



        /// <summary>
        /// See if we can use the HtmlDocument for parsing, and transfer it over to
        /// an XmlDocument for querying...
        /// </summary>
        [Test]
        public void TestHtml2Xml()
        {
            // Transfer content to XmlDoc...
            XmlDocument xhtmlDoc = new XmlDocument();
            HtmlParser.DefaultParser.Parse(xhtmlDoc, new HtmlTextReader(_testHTML));
            Assert.AreEqual("html", xhtmlDoc.DocumentElement.Name);
            Console.WriteLine(xhtmlDoc.OuterXml);
        }


        [Test]
        public void AttributeEncode()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("root");
            doc.AppendChild(root);
            root.SetAttribute("class", "hello&#45;world");
            XmlAttribute attr = root.Attributes["class"];

            string val = System.Net.WebUtility.HtmlDecode(attr.Value);
            attr.Value = val;

            System.Diagnostics.Debug.WriteLine(doc.OuterXml);

            Assert.AreEqual("<root class=\"hello-world\" />", doc.OuterXml);
        }

        [Test]
        public void SelectNodesTest01()
        {
            string query = "<rows><row xpath='//a'><title xpath='./text()'/><link xpath='./@href'/></row></rows>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row><title>Google &amp; Boogle</title><link>www.google.com</link></row><row><title>Yahoo</title><link>www.yahoo.com?q=123&amp;v=1</link></row></rows>", results);
        }

        [Test]
        public void DecodeRequired()
        {
            string htmlWithDecodeRequired = "<html><title>Metalogix | Content Migration & Management for O365 & SharePoint</title><a class='hello&#45;world'>My Results</a><a class='hello world'>def</a> </html>";
            string query = "<foobar>//a[@class='hello-world']/text()</foobar>";
            string results = QueryEngine.SelectOnHtml(htmlWithDecodeRequired, query).InnerXml;
            Assert.AreEqual("<foobar>My Results</foobar>", results);
        }

        [Test]
        public void DecodeRequired2()
        {
            string htmlWithDecodeRequired = "<html><title>Metalogix | Content Migration & Management for O365 & SharePoint</title><a class='hello&#45;world'>My Results</a><a class='hello world'>def</a> </html>";
            string query = "<foobar>//title/text()</foobar>";
            string results = QueryEngine.SelectOnHtml(htmlWithDecodeRequired, query).InnerXml;
            Assert.AreEqual("<foobar>Metalogix | Content Migration &amp; Management for O365 &amp; SharePoint</foobar>", results);
        }

        /// <summary>
        /// We should be able to supply the xpath in the text node, as opposed to the xpath attribute
        /// </summary>
        [Test]
        public void XpathInText()
        {
            string query = "<foobar>//body <subquery xpath='.//a/text()'/></foobar>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<foobar><subquery>Google &amp; Boogle</subquery><subquery>Yahoo</subquery></foobar>", results);
        }

        /// <summary>
        /// Test wrapping the output in a CDATA
        /// </summary>
        [Test]
        public void Text_CDATA()
        {
            string query = "<rows><row xpath='//a'><title cdata='true' xpath='./text()'/><link xpath='./@href'/></row></rows>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row><title><![CDATA[Google & Boogle]]></title><link>www.google.com</link></row><row><title><![CDATA[Yahoo]]></title><link>www.yahoo.com?q=123&amp;v=1</link></row></rows>", results);
        }

        /// <summary>
        /// Result is an anchor text node, not output in CDATA. The special characters should be escaped here.
        /// </summary>
        [Test]
        public void Text_not_CDATA()
        {
            string query = "<anchorText xpath='//a/text()' />";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<anchorText>Google &amp; Boogle</anchorText><anchorText>Yahoo</anchorText>", results);
        }

        /// <summary>
        /// Result is an attribute node, not output in CDATA. The special characters should be escaped here.
        /// </summary>
        [Test]
        public void Attribute_no_CDATA()
        {
            string query = "<hrefAttr xpath='//a/@href' />";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<hrefAttr>www.google.com</hrefAttr><hrefAttr>www.yahoo.com?q=123&amp;v=1</hrefAttr>", results);
        }

        /// <summary>
        /// Test wrapping element output in a CDATA
        /// </summary>
        [Test]
        public void Elem_CDATA()
        {
            string query = "<anchor cdata='true' xpath='//a' />";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<anchor><![CDATA[<a href=\"www.google.com\">Google &amp; Boogle</a>]]></anchor><anchor><![CDATA[<a href=\"www.yahoo.com?q=123&amp;v=1\">Yahoo</a>]]></anchor>", results);
        }

        /// <summary>
        /// Test wrapping element output not in a CDATA
        /// </summary>
        [Test]
        public void Elem_not_CDATA()
        {
            string query = "<anchor cdata='false' xpath='//a' />";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<anchor><a href=\"www.google.com\">Google &amp; Boogle</a></anchor><anchor><a href=\"www.yahoo.com?q=123&amp;v=1\">Yahoo</a></anchor>", results);
        }

        /// <summary>
        /// Test the emit attribute. Setting to 'False' should prevent the wrapper node from being emitted. Allows
        /// use cases where you may want multiple xpaths, but not multiple nodes...
        /// </summary>
        [Test]
        public void No_emit_leafnode()
        {
            string query = "<foobar><title emit='False' xpath='//h1/text()' /></foobar>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<foobar>This is a title</foobar>", results);
        }

        /// <summary>
        /// Test the emit='False' flag for wrapper elements
        /// </summary>
        [Test]
        public void No_emit_elem()
        {
            string query = "<foobar emit='False'><title xpath='//h1/text()' /></foobar>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<title>This is a title</title>", results);
        }

        /// <summary>
        /// Test making sure that with no results we don't return the entire document!
        /// </summary>
        [Test]
        public void NoXPathInQuery()
        {
            string query = "<foobar />";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<foobar />", results);
        }

        /// <summary>
        /// Test that we can return the root node.
        /// </summary>
        [Test]
        public void SelectRoot()
        {
            string query = "<foobar xpath='./html'/>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;

            Console.WriteLine(results);
            Assert.AreEqual("<foobar><html><head><title>This is a title</title></head><body><div class=\"This &amp; is a test\" id=\"ThisIsAnID123\" /><h1>This is a title</h1><br /><p> The quick brown <a href=\"www.google.com\">Google &amp; Boogle</a> fox jumped</p><p> The quick brown <a href=\"www.yahoo.com?q=123&amp;v=1\">Yahoo</a> fox jumped</p></body></html></foobar>" , results);
        }

        /// <summary>
        /// Test XML characters in the attribute...
        /// </summary>
        [Test]
        public void XML_Characters_InResults()
        {
            XmlDocument doc = XHtmlLoader.LoadXmlDocument(_testHTML);
            Console.WriteLine(ToFormattedString(doc));

            string query = "<row xpath='//body/div/@class'></row>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<row>This &amp; is a test</row>", results);
        }

        /// <summary>
        /// Test an OR xpath on multiple attributes...
        /// </summary>
        [Test]
        public void Xpath_OR_test()
        {
            string query = "<rows><row xpath='//body/div/@class | //body/div/@id'></row></rows>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row>This &amp; is a test</row><row>ThisIsAnID123</row></rows>", results);
        }

        /// <summary>
        /// Test putting the xpath in the text of the node rather than in the xpath attribute.
        /// For example: <row xpath='//div'/> and <row>//div</row> should be the same. The 
        /// The advantage of putting the xpath in the text, is twofold: 1) for cases where
        /// you want a filter, you require an extra set of quotes eg: xpath="//div[@class='abc']"
        /// putting the xpath in the text removes this need, and avoids the requirement for escaping
        /// quotes in the outer xml. The second rational is that it makes expressing the query in JSON
        /// much more readable.
        /// </summary>
        [Test]
        public void XpathInTextNode()
        {
            string query = "<rows><row>//body/div/@class</row></rows>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row>This &amp; is a test</row></rows>", results);
        }

        /// <summary>
        /// Test aggregating multiple results with a common parent. This is required
        /// when we wish to run multiple queries, and have the results aggregated.
        /// </summary>
        [Test]
        public void AggregateResults()
        {
            XmlDocument resultsDoc = new XmlDocument();
            XmlElement resultsElem = resultsDoc.CreateElement("testResults");
            resultsDoc.AppendChild(resultsElem);

            QueryEngine.SelectOnHtml(_testHTML, "<r1>//body/div/@id</r1>", resultsElem);
            QueryEngine.SelectOnHtml(_testHTML, "<r2>//h1/text()</r2>", resultsElem);
            Assert.AreEqual("<testResults><r1>ThisIsAnID123</r1><r2>This is a title</r2></testResults>", resultsDoc.OuterXml);
        }

        /// <summary>
        /// We should be able to return results that have no parent
        /// </summary>
        [Test]
        public void FragementResults()
        {
            string query = "<para xpath='//a/@href' />";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<para>www.google.com</para><para>www.yahoo.com?q=123&amp;v=1</para>", results);
        }


        /// <summary>
        /// Test that XML comes out ok when there are XML characters in the selected results...
        /// </summary>
        [Test]
        public void XMLCharsInOutput()
        {
            string query = "<rows><row xpath='//p'></row></rows>";
            string results = QueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual(@"<rows><row><p> The quick brown <a href=""www.google.com"">Google &amp; Boogle</a> fox jumped</p></row><row><p> The quick brown <a href=""www.yahoo.com?q=123&amp;v=1"">Yahoo</a> fox jumped</p></row></rows>", results);
        }

        /// <summary>
        /// A real world example of querying and parsing a web page to extract meaningful
        /// structured data.
        /// </summary>
        [Test]
        public async Task ParseThriftyCategoriesTest()
        {
            // Get query
            string select = @"
                <categories>
                    <category>//a[@class='js-ga-category']
                        <name>./span/text()</name>
                        <count>./text()</count>
                        <link>./@href</link>
                    </category>
                </categories>";

            // Run test
            XmlElement resultsDoc = await QueryEngine.RunSelectAsync("https://www.thriftyfoods.com/shop-now/browse", select);

            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));

            Assert.AreEqual(20, resultsDoc.SelectNodes("//category").Count);
        }

        /// <summary>
        /// The same real-world example, but using the full query
        /// syntax.
        /// </summary>
        [Test]
        public async Task QueryThriftyCategoriesTest()
        {
            // Get query
            string select = @"
            <query>
                <select>
                    <categories>
                        <category>//a[@class='js-ga-category']
                            <name>./span/text()</name>
                            <count>./text()</count>
                            <link>./@href</link>
                        </category>
                    </categories>
                </select>
                <from>https://www.thriftyfoods.com/shop-now/browse</from>
            </query>";

            // Run test
            XmlElement resultsDoc = await QueryEngine.RunQueryAsync(select);
            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));
            Assert.AreEqual(20, resultsDoc.SelectNodes("//category").Count);
        }

        [Test]
        public async Task ParseThriftyProductsTest()
        {
            // Get query
            string select = @"
                <products>
                    <product xpath=""//div[@class='item-product__content push--top']"">
                        <name xpath="".//a[@class='js-ga-productname']/text()"" />
                        <link xpath="".//a[@class='js-ga-productname']/@href"" />
                        <price xpath="".//span[@class='price text--strong']/text()"" />                   
                        <productInfo1 xpath="".//div[@class='item-product__info'][1]/text()"" />
                        <productInfo2 xpath="".//div[@class='item-product__info'][2]/text()"" />
                        <productInfo3 xpath="".//div[@class='item-product__info'][3]/text()"" />
                    </product>
                </products>";

            // Run test
            XmlElement resultsDoc = await QueryEngine.RunSelectAsync("https://www.thriftyfoods.com/shop-now/bulk-foods?pageSize=5", select);

            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));

            Assert.AreEqual(5, resultsDoc.SelectNodes("//product").Count);
        }

        /// <summary>
        /// A real world query example where one of the url's supplied is 
        /// invalid. Here we should get two errors and one valid result. 
        /// The errors should be returned in the XML results, rather than
        /// be thrown by the call.
        /// </summary>
        [Test]
        public async Task InvalidUrlTest()
        {
            // Get query
            string select = @"
            <query>
                <select>
                    <mytitle>//title</mytitle>
                </select>
                <from>https://accounts.google.com:443/intl/en/policies/terms/</from>
                <from>http://www.wikipedia.org</from>
                <from>https://www.invalidUrlThisDoesNotExist.com/shop-now/browse</from>
            </query>";

            // Run test
            XmlElement resultsDoc = await QueryEngine.RunQueryAsync(select);
            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));
            Assert.AreEqual(2, resultsDoc.SelectNodes("//error").Count);
        }

    }
}
