using System;
using System.Xml;

namespace XHtmlKit
{
    public abstract class DomBuilder<DomDocument, DomElement, DomNode>
    {
        public abstract DomDocument Document { get; }
        public abstract DomNode RootNode { get; }
        public abstract DomElement AddElement(DomNode node, string elemName);
        public abstract void AddComment(DomNode node, string comment);
        public abstract void AddText(DomElement node, string text);
        public abstract void AddAttribute(DomElement node, string attrName, string attrValue);
    }

    public class XmlDomBuilder : DomBuilder<XmlDocument, XmlElement, XmlNode>
    {
        private XmlDocument _doc;
        private XmlNode _rootNode;
        public XmlDomBuilder()
        {
            _doc = new XmlDocument();
            _rootNode = _doc;
        }

        public XmlDomBuilder(XmlNode rootNode)
        {
            // Ensure valid root node type
            if (!(rootNode is XmlDocument || rootNode is XmlElement))
                throw new Exception("Invalid rootNode type. Must be either XmlDocument or XmlElement.");

            // Get the ownning document for the root node
            _rootNode = rootNode;
            _doc = rootNode is XmlDocument ? (XmlDocument)rootNode : rootNode.OwnerDocument;
        }

        public override XmlDocument Document { get { return _doc; } }
        public override XmlNode RootNode { get { return _rootNode; } }

        public override XmlElement AddElement(XmlNode node, string elemName)
        {
            XmlElement newElem = _doc.CreateElement(elemName);
            node.AppendChild(newElem);
            return newElem;
        }

        public override void AddText(XmlElement node, string text)
        {
            node.AppendChild(_doc.CreateTextNode(text));
        }

        public override void AddComment(XmlNode node, string comment)
        {
            node.AppendChild(_doc.CreateComment(comment));
        }

        public override void AddAttribute(XmlElement node, string attrName, string attrValue)
        {
            // Don't update existing attributes
            if (node.Attributes[attrName] != null)
                return;

            XmlAttribute attr = _doc.CreateAttribute(attrName);
            attr.Value = attrValue;
            node.Attributes.Append(attr);
        }
    }
}
