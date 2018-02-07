#if !net20 

using System.Xml;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace XHtmlKit
{
    /// <summary>
    /// Adds extensions to the XmlDocument class for querying the document
    /// using XPath... 
    /// </summary>
    public class XHtmlQueryEngine
    {
        private HtmlParser _htmlParser = null;

        public XHtmlQueryEngine(HtmlParser parser=null)
        {
            _htmlParser = parser != null ? parser :  HtmlParser.DefaultParser;
        }

        public HtmlParser Parser
        {
            get { return _htmlParser; }
        }

        

        /// <summary>
        /// Extracts content from an XHtmlNode
        /// </summary>
        /// <param name="inputHtmlNode"></param>
        /// <param name="selectQueryNode"></param>
        /// <param name="resultMountNode"></param>
        private static void SelectNodes(XmlNode inputHtmlNode, XmlNode selectQueryNode, XmlNode resultMountNode)
        {
            // Don't query on something that is not an element
            if (selectQueryNode.NodeType != XmlNodeType.Element)
            {
                return;
            }

            // Get parameters
            bool emit = selectQueryNode.Attributes["emit"] != null ? (bool.Parse(selectQueryNode.Attributes["emit"].Value) == true) : true; // emit by default
            bool wrapInCData = selectQueryNode.Attributes["cdata"] != null && (bool.Parse(selectQueryNode.Attributes["cdata"].Value) == true);
            string xpath = selectQueryNode.Attributes["xpath"] != null ? selectQueryNode.Attributes["xpath"].Value : ((selectQueryNode.FirstChild != null) && (selectQueryNode.FirstChild.NodeType == XmlNodeType.Text) ? selectQueryNode.FirstChild.Value : null);

            // Run the xpath query to get the result nodes
            IEnumerable queryResultNodes = (xpath == null) ? new XmlNode[] { inputHtmlNode } : (IEnumerable)inputHtmlNode.SelectNodes(xpath);

            // Get the associated owner document
            XmlDocument ownerDocument = resultMountNode is XmlDocument ? (XmlDocument)resultMountNode : resultMountNode.OwnerDocument;

            // Add each query result
            foreach (XmlNode queryResultNode in queryResultNodes)
            {
                // Create a new result node (can be a fragment, if we are not emitting)
                XmlNode childResultNode = emit ? ownerDocument.CreateElement(selectQueryNode.Name) : (XmlNode)ownerDocument.CreateDocumentFragment();

                //  Recurse down if we have child queries to perform... 
                if (selectQueryNode.HasChildNodes && (selectQueryNode.LastChild.NodeType == XmlNodeType.Element))
                {
                    // Recurse down
                    foreach (XmlNode childSelectNode in selectQueryNode.ChildNodes)
                    {
                        SelectNodes(queryResultNode, childSelectNode, childResultNode);
                    }
                }

                // Add the content to the results, if we have no child queries to perform.
                else
                {
                    bool isText = queryResultNode.NodeType == XmlNodeType.Text || queryResultNode.NodeType == XmlNodeType.Attribute || queryResultNode.NodeType == XmlNodeType.CDATA;
                    XmlNode resultContentNode = null;

                    // Text result - wrap it in CDATA
                    if (isText && wrapInCData)
                        resultContentNode = ownerDocument.CreateCDataSection(queryResultNode.Value.Trim());

                    // Text result - output as text (decode special characters)
                    else if (isText && !wrapInCData)
                        resultContentNode = ownerDocument.CreateTextNode( queryResultNode.Value.Trim() );

                    // Element result - wrap in CDATA
                    else if (!isText && wrapInCData)
                        resultContentNode = ownerDocument.CreateCDataSection(queryResultNode.OuterXml);

                    // For the document root - we don't want to imort anything - just have it be an empty fragment
                    else if (queryResultNode.NodeType == XmlNodeType.Document)
                        resultContentNode = ownerDocument.CreateDocumentFragment();

                    // All other cases, just import the node 
                    else
                        resultContentNode = ownerDocument.ImportNode(queryResultNode, true);

                    // Add content to the results
                    childResultNode.AppendChild(resultContentNode);
                }

                // Mount the results 
                resultMountNode.AppendChild(childResultNode);

            }

        }

        public XmlElement SelectOnHtml(string html, string selectQueryXml, XmlElement output = null, string originatingUrl = null)
        {
            // Load content from html string
            XmlDocument xhtmlDoc = new XmlDocument();
            this.Parser.Parse(xhtmlDoc, new HtmlTextReader(html), new HtmlParserOptions{ BaseUrl = originatingUrl });

            // Create a result element to mount results onto if none was supplied
            XmlElement resultElem = output;
            if (resultElem == null)
            {
                XmlDocument resultDoc = new XmlDocument();
                resultElem = resultDoc.CreateElement("results");
                resultDoc.AppendChild(resultElem);
            }

            // Run the query on the XHtml document
            return SelectOnXHtml(xhtmlDoc, selectQueryXml, resultElem);
        }

        public XmlElement SelectOnXHtml(XmlDocument xhtmlDoc, string selectQueryXml, XmlElement output)
        {
            // Load query
            XmlDocument selectDoc = new XmlDocument();
            selectDoc.LoadXml(selectQueryXml);

            // Select content
            SelectNodes(xhtmlDoc, selectDoc.DocumentElement, output);
            return output;
        }

        public async Task<XmlDocument> LoadXHtmlDocAsync(string url)
        {
            XmlDocument xhtmlDoc = new XmlDocument();

            // Get the Html asynchronously and Parse it into an Xml Document            
            using (HtmlTextReader htmlReader = await HtmlClient.GetHtmlTextReaderAsync(url))
                this.Parser.Parse(xhtmlDoc, htmlReader, new HtmlParserOptions { BaseUrl = url } );

            return xhtmlDoc;
        }

        public async Task<XmlElement> RunSelectAsync(string url, string selectQueryXml, XmlElement outputElement=null)
        {
            // Create a result element to mount results onto if none was supplied
            XmlElement resultElem = outputElement;
            if (resultElem == null)
            {
                XmlDocument resultDoc = new XmlDocument();
                resultElem = resultDoc.CreateElement("results");
                resultDoc.AppendChild(resultElem);
            }

            try
            {
                // Get the Html asynchronously and Parse it into an Xml Document
                XmlDocument xhtmlDoc = await LoadXHtmlDocAsync(url);

                // Run the query on the xhtml content, mount onto results
                SelectOnXHtml(xhtmlDoc, selectQueryXml, resultElem);
            }
            catch (System.Exception ex)
            {                
                XmlNode errorNode = resultElem.AppendChild(resultElem.OwnerDocument.CreateElement("error"));
                errorNode.InnerText = ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : "");
            }

            return resultElem;
        }

        public async Task<XmlElement> RunQueryAsync(string queryXml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(queryXml);
            return await RunQueryAsync(xmlDoc);
        }

        public async Task<XmlElement> RunQueryAsync(XmlDocument queryDoc)
        {
            // Get the 'select' node, and ensure we are not going to emit a 'select' node in the results
            XmlElement selectQueryNode = (XmlElement)queryDoc.SelectSingleNode("//query/select");
            if (selectQueryNode == null)
            {
                throw new System.Exception("Invalid query. Missing 'select'.");
            }
            selectQueryNode.SetAttribute("emit", "false");

            // Get the list of 'from' nodes. There may be multiple.
            XmlNodeList fromQueryNodes = queryDoc.SelectNodes("//query/from");
            if (fromQueryNodes.Count == 0)
            {
                throw new System.Exception("Invalid query. Missing 'from'.");
            }

            // Build the results document with a 'results' root element.
            XmlDocument retval = new XmlDocument();
            XmlElement resultNode = retval.CreateElement("results");
            retval.AppendChild(resultNode);

            // Build a list of query tasks and launch them.
            List<Task> tasks = new List<Task>();
            foreach (XmlNode urlNode in fromQueryNodes)
            {
                tasks.Add(RunSelectAsync(urlNode.InnerText, selectQueryNode.OuterXml, resultNode));
            }

            // Await the results...
            await Task.WhenAll(tasks);

            return retval.DocumentElement;
        }
    }
}
#endif