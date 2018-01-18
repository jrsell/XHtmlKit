using System.Net;
using System.Xml;
using System.Collections;
using System.Threading.Tasks;
using XHtmlKit;

namespace XHtmlKit.Query
{
    /// <summary>
    /// Adds extensions to the XmlDocument class for querying the document
    /// using XPath... 
    /// </summary>
    public static class XHtmlQueryEngine
    {

        /// <summary>
        /// Extracts content from an XHtmlNode
        /// </summary>
        /// <param name="inputHtmlNode"></param>
        /// <param name="selectQueryNode"></param>
        /// <param name="resultMountNode"></param>
        public static void SelectNodes(this XmlNode inputHtmlNode, XmlNode selectQueryNode, XmlNode resultMountNode)
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
                        queryResultNode.SelectNodes(childSelectNode, childResultNode);
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
                        resultContentNode = ownerDocument.CreateTextNode(WebUtility.HtmlDecode( queryResultNode.Value ).Trim() );

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


        public static async Task<XmlElement> RunQueryOnWebPageAsync(string url, string selectXml, XmlElement output = null, string originatingUrl = null)
        {
            // Load content from url
            XmlDocument xhtmlDoc = new XmlDocument();
            await xhtmlDoc.LoadWebPageAsync(url);
            return RunQueryOnXHtml(xhtmlDoc, selectXml, output);
        }

        public static XmlElement RunQueryOnHtml(string html, string selectXml, XmlElement output = null, string originatingUrl = null)
        {
            // Load content from html string
            XmlDocument xhtmlDoc = new XmlDocument();
            xhtmlDoc.LoadHtml(html, originatingUrl);
            return RunQueryOnXHtml(xhtmlDoc, selectXml, output);
        }

        public static XmlElement RunQueryOnXHtml(XmlDocument xhtmlDoc, string selectXml, XmlElement output = null)
        {
            // Load query
            XmlDocument selectDoc = new XmlDocument();
            selectDoc.LoadXml(selectXml);

            // Create a result element to mount results onto to if necessary
            XmlElement resultElem = output;
            if (resultElem == null)
            {
                XmlDocument resultDoc = new XmlDocument();
                resultElem = resultDoc.CreateElement("results");
                resultDoc.AppendChild(resultElem);
            }

            // Select content
            xhtmlDoc.SelectNodes(selectDoc.DocumentElement, resultElem);
            return resultElem;
        }
    }
}