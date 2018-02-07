using System;
using System.Xml;

namespace XHtmlKit
{
    public abstract class DomBuilder<DomNode>
    {
        public abstract DomNode RootNode { get; }
        public abstract DomNode AddElement(DomNode node, string elemName);
        public abstract void AddComment(DomNode node, string comment);
        public abstract void AddText(DomNode node, string text);
        public abstract void AddAttribute(DomNode node, string attrName, string attrValue);
        public abstract string GetAttribute(DomNode node, string attrName);
        public abstract DomNode FindAncestor(DomNode node, string name);
        public abstract void RemoveAll(DomNode node);
    }

    public class XmlDomBuilder : DomBuilder<XmlNode>
    {
        private XmlDocument _doc;
        private XmlNode _rootNode;

        public XmlDomBuilder(XmlNode rootNode)
        {
            // Ensure valid root node type
            if (!(rootNode is XmlDocument || rootNode is XmlElement))
                throw new Exception("Invalid rootNode type. Must be either XmlDocument or XmlElement.");

            // Get the ownning document for the root node
            _rootNode = rootNode;
            _doc = rootNode is XmlDocument ? (XmlDocument)rootNode : rootNode.OwnerDocument;
        }

        public override XmlNode RootNode { get { return _rootNode; } }

        public override XmlNode AddElement(XmlNode node, string elemName)
        {
            XmlElement newElem = _doc.CreateElement(elemName);
            node.AppendChild(newElem);
            return newElem;
        }

        public override void AddText(XmlNode node, string text)
        {
            node.AppendChild(_doc.CreateTextNode(text));
        }

        public override void AddComment(XmlNode node, string comment)
        {
            node.AppendChild(_doc.CreateComment(comment));
        }

        public override void AddAttribute(XmlNode node, string attrName, string attrValue)
        {            
            XmlAttributeCollection attributes = node.Attributes;
            if (attributes[attrName] == null)
            {
                XmlAttribute attr = _doc.CreateAttribute(attrName);
                attr.Value = attrValue;
                attributes.SetNamedItem(attr);
            }            
        }

        public override string GetAttribute(XmlNode node, string attrName)
        {
            XmlAttribute attr = node.Attributes[attrName];
            return attr == null ? string.Empty : attr.Value;
        }

        public override void RemoveAll(XmlNode node)
        {
            while (node.LastChild != null)
                node.RemoveChild(node.LastChild);
        }

        public override XmlNode FindAncestor(XmlNode node, string name)
        {
            XmlNode parent = node;
            bool tagMatch = false;
            while (true)
            {
                // Got to the top of the tree
                if (parent.ParentNode == null)
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
                parent = parent.ParentNode;
            }

            // If we found a match - return the match's parent
            if (tagMatch)
                return parent.ParentNode;

            return null;
        }
    }
}
