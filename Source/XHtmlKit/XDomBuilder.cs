#if !net20
using System;
using System.Xml.Linq;

namespace XHtmlKit
{
    public class XDomBuilder : DomBuilder<XNode>
    {
        private XDocument _doc;
        private XNode _rootNode;

        public XDomBuilder(XNode rootNode)
        {
            // Ensure valid root node type
            if (!(rootNode is XDocument || rootNode is XElement))
                throw new Exception("Invalid rootNode type. Must be either XDocument or XElement.");

            // Get the ownning document for the root node
            _rootNode = rootNode;
            _doc = rootNode.Document;
        }

        public override XNode RootNode { get { return _rootNode; } }

        public override XNode AddElement(XNode node, string elemName)
        {
            XContainer newElem = new XElement(elemName);
            XContainer currNode = (XContainer)node;
            currNode.Add(newElem);
            return newElem;
        }

        public override void AddText(XNode node, string text)
        {
            XContainer currNode = (XContainer)node;
            currNode.Add(new XText(text));
        }

        public override void AddComment(XNode node, string comment)
        {
            XContainer currNode = (XContainer)node;
            currNode.Add(new XComment(comment));
        }

        public override void AddAttribute(XNode node, string attrName, string attrValue)
        {
            XElement currNode = (XElement)node;

            // Don't update existing attributes
            if (currNode.Attribute(attrName) != null)
                return;

            currNode.Add(new XAttribute(attrName, attrValue));
        }

        public override string GetAttribute(XNode node, string attrName)
        {
            XElement elem = (XElement)node;
            XAttribute attr = elem.Attribute(attrName);
            return attr == null ? string.Empty : attr.Value;
        }

        public override XNode FindAncestor(XNode node, string name)
        {
            XElement parent = (XElement)node;
            bool tagMatch = false;
            while (true)
            {
                // Got to the top of the tree
                if (parent.Parent == null)
                    break;
                // Got to our root node
                if (parent == _rootNode)
                    break;
                // Found a match for our tag
                if (parent.Name == name) {
                    tagMatch = true;
                    break;
                }

                // Move up
                parent = parent.Parent;
            }

            // If we found a match - return the match's parent
            if (tagMatch)
                return parent.Parent;

            return null;
        }
    }
}
#endif