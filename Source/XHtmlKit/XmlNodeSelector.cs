#if !net20 
using System.Collections;
using System.Xml;
using System.Xml.Linq;

namespace XHtmlKit.Query
{
    internal abstract class NodeSelector<DomNode>
    {
        public abstract void SelectNodes(DomNode inputHtmlNode, DomNode selectQueryNode, DomNode resultMountNode);
    }

    internal class XNodeSelector : NodeSelector<XNode>
    {
        public override void SelectNodes(XNode inputHtmlNode, XNode selectQueryNode, XNode resultMountNode)
        {
            throw new System.NotImplementedException();
        }
    }

    class XmlNodeSelector : NodeSelector<XmlNode>
    {
        public override void SelectNodes(XmlNode inputHtmlNode, XmlNode selectQueryNode, XmlNode resultMountNode)
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
                        resultContentNode = ownerDocument.CreateTextNode(queryResultNode.Value.Trim());

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
    }

}
#endif