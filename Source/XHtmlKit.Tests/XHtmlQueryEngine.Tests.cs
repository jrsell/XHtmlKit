using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using System.Threading.Tasks;

namespace XHtmlKit.Query.Tests
{
    [TestClass]
    public class XHtmlQueryEngine_Tests
    {
        public XHtmlQueryEngine_Tests()
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
        [TestMethod]
        public void TestHtml2Xml()
        {
            // Transfer content to XmlDoc...
            XmlDocument xhtmlDoc = new XmlDocument();
            xhtmlDoc.LoadHtml(_testHTML);
            Assert.AreEqual("html", xhtmlDoc.DocumentElement.Name);
            Console.WriteLine(xhtmlDoc.OuterXml);
        }


        [TestMethod]
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

        [TestMethod]
        public void SelectNodesTest01()
        {
            string query = "<rows><row xpath='//a'><title xpath='./text()'/><link xpath='./@href'/></row></rows>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row><title>Google &amp; Boogle</title><link>www.google.com</link></row><row><title>Yahoo</title><link>www.yahoo.com?q=123&amp;v=1</link></row></rows>", results);
        }

        [TestMethod]
        public void DecodeRequired()
        {
            string htmlWithDecodeRequired = "<html><title>Metalogix | Content Migration & Management for O365 & SharePoint</title><a class='hello&#45;world'>My Results</a><a class='hello world'>def</a> </html>";
            string query = "<foobar>//a[@class='hello-world']/text()</foobar>";
            string results = XHtmlQueryEngine.SelectOnHtml(htmlWithDecodeRequired, query).InnerXml;
            Assert.AreEqual("<foobar>My Results</foobar>", results);
        }

        [TestMethod]
        public void DecodeRequired2()
        {
            string htmlWithDecodeRequired = "<html><title>Metalogix | Content Migration & Management for O365 & SharePoint</title><a class='hello&#45;world'>My Results</a><a class='hello world'>def</a> </html>";
            string query = "<foobar>//title/text()</foobar>";
            string results = XHtmlQueryEngine.SelectOnHtml(htmlWithDecodeRequired, query).InnerXml;
            Assert.AreEqual("<foobar>Metalogix | Content Migration &amp; Management for O365 &amp; SharePoint</foobar>", results);
        }

        /// <summary>
        /// We should be able to supply the xpath in the text node, as opposed to the xpath attribute
        /// </summary>
        [TestMethod]
        public void XpathInText()
        {
            string query = "<foobar>//body <subquery xpath='.//a/text()'/></foobar>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<foobar><subquery>Google &amp; Boogle</subquery><subquery>Yahoo</subquery></foobar>", results);
        }

        /// <summary>
        /// Test wrapping the output in a CDATA
        /// </summary>
        [TestMethod]
        public void Text_CDATA()
        {
            string query = "<rows><row xpath='//a'><title cdata='true' xpath='./text()'/><link xpath='./@href'/></row></rows>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row><title><![CDATA[Google & Boogle]]></title><link>www.google.com</link></row><row><title><![CDATA[Yahoo]]></title><link>www.yahoo.com?q=123&amp;v=1</link></row></rows>", results);
        }

        /// <summary>
        /// Result is an anchor text node, not output in CDATA. The special characters should be escaped here.
        /// </summary>
        [TestMethod]
        public void Text_not_CDATA()
        {
            string query = "<anchorText xpath='//a/text()' />";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<anchorText>Google &amp; Boogle</anchorText><anchorText>Yahoo</anchorText>", results);
        }

        /// <summary>
        /// Result is an attribute node, not output in CDATA. The special characters should be escaped here.
        /// </summary>
        [TestMethod]
        public void Attribute_no_CDATA()
        {
            string query = "<hrefAttr xpath='//a/@href' />";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<hrefAttr>www.google.com</hrefAttr><hrefAttr>www.yahoo.com?q=123&amp;v=1</hrefAttr>", results);
        }

        /// <summary>
        /// Test wrapping element output in a CDATA
        /// </summary>
        [TestMethod]
        public void Elem_CDATA()
        {
            string query = "<anchor cdata='true' xpath='//a' />";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<anchor><![CDATA[<a href=\"www.google.com\">Google &amp; Boogle</a>]]></anchor><anchor><![CDATA[<a href=\"www.yahoo.com?q=123&amp;v=1\">Yahoo</a>]]></anchor>", results);
        }

        /// <summary>
        /// Test wrapping element output not in a CDATA
        /// </summary>
        [TestMethod]
        public void Elem_not_CDATA()
        {
            string query = "<anchor cdata='false' xpath='//a' />";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<anchor><a href=\"www.google.com\">Google &amp; Boogle</a></anchor><anchor><a href=\"www.yahoo.com?q=123&amp;v=1\">Yahoo</a></anchor>", results);
        }

        /// <summary>
        /// Test the emit attribute. Setting to 'False' should prevent the wrapper node from being emitted. Allows
        /// use cases where you may want multiple xpaths, but not multiple nodes...
        /// </summary>
        [TestMethod]
        public void No_emit_leafnode()
        {
            string query = "<foobar><title emit='False' xpath='//h1/text()' /></foobar>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<foobar>This is a title</foobar>", results);
        }

        /// <summary>
        /// Test the emit='False' flag for wrapper elements
        /// </summary>
        [TestMethod]
        public void No_emit_elem()
        {
            string query = "<foobar emit='False'><title xpath='//h1/text()' /></foobar>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<title>This is a title</title>", results);
        }

        /// <summary>
        /// Test making sure that with no results we don't return the entire document!
        /// </summary>
        [TestMethod]
        public void NoXPathInQuery()
        {
            string query = "<foobar />";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<foobar />", results);
        }

        /// <summary>
        /// Test that we can return the root node.
        /// </summary>
        [TestMethod]
        public void SelectRoot()
        {
            string query = "<foobar xpath='./html'/>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<foobar><html><head><title>This is a title</title></head><body><div class=\"This &amp; is a test\" id=\"ThisIsAnID123\" /><h1>This is a title</h1><br /><p> The quick brown <a href=\"www.google.com\">Google &amp; Boogle</a> fox jumped</p><p> The quick brown <a href=\"www.yahoo.com?q=123&amp;v=1\">Yahoo</a> fox jumped</p></body></html></foobar>" , results);
        }

        /// <summary>
        /// Test XML characters in the attribute...
        /// </summary>
        [TestMethod]
        public void XML_Characters_InResults()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadHtml(_testHTML);
            Console.WriteLine(ToFormattedString(doc));

            string query = "<row xpath='//body/div/@class'></row>";
            string results = Query.XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<row>This &amp; is a test</row>", results);
        }

        /// <summary>
        /// Test an OR xpath on multiple attributes...
        /// </summary>
        [TestMethod]
        public void Xpath_OR_test()
        {
            string query = "<rows><row xpath='//body/div/@class | //body/div/@id'></row></rows>";
            string results = Query.XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row>This &amp; is a test</row><row>ThisIsAnID123</row></rows>", results);
        }

        /// <summary>
        /// Test putting the xpath in the text of the node rather than in the xpath attribute
        /// </summary>
        [TestMethod]
        public void XpathInTextNode()
        {
            string query = "<rows><row>//body/div/@class</row></rows>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<rows><row>This &amp; is a test</row></rows>", results);
        }

        /// <summary>
        /// Test aggregating multiple results with a common parent
        /// </summary>
        [TestMethod]
        public void AggregateResults()
        {
            XmlDocument resultsDoc = new XmlDocument();
            XmlElement resultsElem = resultsDoc.CreateElement("testResults");
            resultsDoc.AppendChild(resultsElem);

            XHtmlQueryEngine.SelectOnHtml(_testHTML, "<r1>//body/div/@id</r1>", resultsElem);
            XHtmlQueryEngine.SelectOnHtml(_testHTML, "<r2>//h1/text()</r2>", resultsElem);
            Assert.AreEqual("<testResults><r1>ThisIsAnID123</r1><r2>This is a title</r2></testResults>", resultsDoc.OuterXml);
        }

        /// <summary>
        /// We should be able to return results that have no parent
        /// </summary>
        [TestMethod]
        public void FragementResults()
        {
            string query = "<para xpath='//a/@href' />";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual("<para>www.google.com</para><para>www.yahoo.com?q=123&amp;v=1</para>", results);
        }


        /// <summary>
        /// Test that XML comes out ok when there are XML characters in the selected results...
        /// </summary>
        [TestMethod]
        public void XMLCharsInOutput()
        {
            string query = "<rows><row xpath='//p'></row></rows>";
            string results = XHtmlQueryEngine.SelectOnHtml(_testHTML, query).InnerXml;
            Assert.AreEqual(@"<rows><row><p> The quick brown <a href=""www.google.com"">Google &amp; Boogle</a> fox jumped</p></row><row><p> The quick brown <a href=""www.yahoo.com?q=123&amp;v=1"">Yahoo</a> fox jumped</p></row></rows>", results);
        }

        [TestMethod]
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
            XmlElement resultsDoc = await XHtmlQueryEngine.RunSelectAsync("https://www.thriftyfoods.com/shop-now/browse", select);

            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));

            Assert.AreEqual(20, resultsDoc.SelectNodes("//category").Count);
        }

        [TestMethod]
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
            XmlElement resultsDoc = await XHtmlQueryEngine.RunQueryAsync(select);
            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));
            Assert.AreEqual(20, resultsDoc.SelectNodes("//category").Count);
        }

        [TestMethod]
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
            XmlElement resultsDoc = await XHtmlQueryEngine.RunSelectAsync("https://www.thriftyfoods.com/shop-now/bulk-foods?pageSize=5", select);

            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));

            Assert.AreEqual(5, resultsDoc.SelectNodes("//product").Count);
        }

        [TestMethod]
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
            XmlElement resultsDoc = await XHtmlQueryEngine.RunQueryAsync(select);
            System.Diagnostics.Debug.Write(ToFormattedString(resultsDoc.OwnerDocument));
            Assert.AreEqual(2, resultsDoc.SelectNodes("//error").Count);
        }

    }
}
