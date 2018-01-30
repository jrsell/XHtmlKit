using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace XHtmlKit
{
    public class HtmlStreamParser : HtmlParser
    {
        public override void LoadHtml(XmlDocument doc, TextReader htmlTextReader, string baseUrl = null)
        {
            XmlDomBuilder dom = new XmlDomBuilder(doc);
            HtmlStreamParserGeneric<XmlNode> parser = new HtmlStreamParserGeneric<XmlNode>();
            parser.LoadXHtml(dom, htmlTextReader, baseUrl, InsersionMode.BeforeHtml);
        }

        public override void LoadHtmlFragment(XmlNode rootNode, TextReader htmlTextReader, string baseUrl = null)
        {
            XmlDomBuilder dom = new XmlDomBuilder(rootNode);
            HtmlStreamParserGeneric<XmlNode> parser = new HtmlStreamParserGeneric<XmlNode>();
            parser.LoadXHtml(dom, htmlTextReader, baseUrl, InsersionMode.InBody);
        }
    }

    public enum InsersionMode
    {
        BeforeHtml,
        BeforeHead,
        InHead,
        InBody
    }

    public class HtmlStreamParserGeneric<DomNode>
    {

        private enum TagAttributes
        {
            None = 0,
            SelfClosing = 1,
            IsHeadTag = 2,
            TopLevel = 4,
            RCData = 8,
        }

        private static Dictionary<string, TagAttributes> _tagAttributes = new Dictionary<string, TagAttributes>()
        {
            {"area", TagAttributes.SelfClosing},
            {"base", TagAttributes.SelfClosing | TagAttributes.IsHeadTag},
            {"basefont", TagAttributes.SelfClosing | TagAttributes.IsHeadTag},
            {"bgsound", TagAttributes.SelfClosing | TagAttributes.IsHeadTag},
            {"body", TagAttributes.TopLevel},
            {"br", TagAttributes.SelfClosing},
            {"col", TagAttributes.SelfClosing},
            {"command", TagAttributes.SelfClosing},
            {"embed", TagAttributes.SelfClosing},
            {"head", TagAttributes.TopLevel},
            {"hr", TagAttributes.SelfClosing},
            {"html", TagAttributes.TopLevel},
            {"iframe", TagAttributes.RCData},
            {"img", TagAttributes.SelfClosing},
            {"input", TagAttributes.SelfClosing},
            {"keygen", TagAttributes.SelfClosing},
            {"link", TagAttributes.SelfClosing | TagAttributes.IsHeadTag},
            {"menuitem", TagAttributes.SelfClosing},
            {"meta", TagAttributes.SelfClosing | TagAttributes.IsHeadTag},
            {"noembed", TagAttributes.RCData},
            {"noframes", TagAttributes.IsHeadTag},
            {"noscript", TagAttributes.IsHeadTag | TagAttributes.RCData},
            {"param", TagAttributes.SelfClosing},
            {"script", TagAttributes.IsHeadTag | TagAttributes.RCData},
            {"source", TagAttributes.SelfClosing},
            {"style", TagAttributes.IsHeadTag | TagAttributes.RCData},
            {"template", TagAttributes.IsHeadTag},
            {"textarea", TagAttributes.RCData},
            {"title", TagAttributes.IsHeadTag | TagAttributes.RCData},
            {"track", TagAttributes.SelfClosing},
            {"wbr", TagAttributes.SelfClosing},
            {"xmp", TagAttributes.RCData}
        };

        private static bool IsHeadTag(string tag)
        {
            TagAttributes attrs = TagAttributes.None;
            _tagAttributes.TryGetValue(tag, out attrs);
            bool isHeadTag = (attrs & TagAttributes.IsHeadTag) > 0;
            return isHeadTag;
        }

        public void LoadXHtml(DomBuilder<DomNode> dom, TextReader htmlTextReader, string baseUrl = null, InsersionMode mode = InsersionMode.BeforeHtml)
        {

            // Create HtmlTextReader for html tag tokenization
            HtmlTextReader reader = new HtmlTextReader(htmlTextReader);

            // DOM Node pointers
            DomNode currNode = dom.RootNode;
            DomNode htmlNode = default(DomNode); 
            DomNode headNode = default(DomNode); 
            DomNode bodyNode = default(DomNode);

            // Set the initial insertion mode. If we are parsing into an XmlDocument, use
            // BeforeHtml, use InBody;
            InsersionMode insertionMode = mode;

            while (reader.ParseState != ParseState.Done)
            {
                ParseState state = reader.ParseState;

                // Read the next token. Ignore empty tokens
                string tok = reader.ReadNext();
                if (IsNullOrWhiteSpace(tok))
                    continue;

                // Find the insertion point for the token based on our mode 
                switch (insertionMode)
                {
                    case InsersionMode.BeforeHtml:

                        // Comment is valid at the top
                        if (state == ParseState.Comment)
                            break;

                        // <html> is valid at the top - switch to BeforeHead
                        if (state == ParseState.OpenTag && tok == "html") {
                            htmlNode = dom.AddElement(dom.RootNode, "html");
                            currNode = htmlNode;
                            insertionMode = InsersionMode.BeforeHead;
                            break;
                        }

                        // Got a tag that should be in the head. Create html/head node structure... 
                        if (state == ParseState.OpenTag && IsHeadTag(tok)) {
                            htmlNode = dom.AddElement(dom.RootNode, "html");
                            headNode = dom.AddElement(htmlNode, "head");
                            currNode = headNode;
                            insertionMode = InsersionMode.InHead;
                            break;
                        }

                        // Got anything else - put it in the body
                        else {
                            htmlNode = dom.AddElement(dom.RootNode, "html");
                            headNode = dom.AddElement(htmlNode, "head");
                            bodyNode = dom.AddElement(htmlNode, "body");
                            currNode = bodyNode;
                            insertionMode = InsersionMode.InBody;
                            break;
                        }

                    case InsersionMode.BeforeHead:

                        // Comment is valid 
                        if (state == ParseState.Comment)
                            break;

                        // <head> is valid here - switch to InHead
                        if (state == ParseState.OpenTag && tok == "head") {
                            headNode = dom.AddElement(htmlNode, "head");
                            currNode = headNode;
                            insertionMode = InsersionMode.InHead;
                            break;
                        }

                        // Got a tag that 'should' be in the head. Create head... 
                        if (state == ParseState.OpenTag && IsHeadTag(tok)) {
                            headNode = dom.AddElement(htmlNode, "head");
                            currNode = headNode;
                            insertionMode = InsersionMode.InHead;
                            break;
                        }

                        // Got anything else, including <body> - put it in the body
                        headNode = dom.AddElement(htmlNode, "head");
                        bodyNode = dom.AddElement(htmlNode, "body");
                        currNode = bodyNode;
                        insertionMode = InsersionMode.InBody;
                        break;

                    case InsersionMode.InHead:

                        // Comment is valid here
                        if (state == ParseState.Comment)
                            break;

                        // Head tags are valid here
                        if (state == ParseState.OpenTag && IsHeadTag(tok)) {
                            break;
                        }

                        // Anything else must go into the body
                        bodyNode = dom.AddElement(htmlNode, "body");
                        currNode = bodyNode;
                        insertionMode = InsersionMode.InBody;
                        break;
                }                

                // Add the token to the DOM
                switch (state)
                {
                    case ParseState.Comment:
                        // Xml Comments cannot have '--', and they cannot end in '-'
                        string commentText = tok.Replace("-", "#");
                        dom.AddComment(currNode, commentText);
                        break; 

                    case ParseState.Text:
                        // Decode the text, to convert all encoded values (eg: '&gt;' to '>')
                        string textContent = HtmlDecode(tok);
                        dom.AddText(currNode, textContent);
                        break;
                    
                    case ParseState.OpenTag:

                        // Look up the attributes of the tag
                        TagAttributes attributes = TagAttributes.None;
                        _tagAttributes.TryGetValue(tok, out attributes);

                        // For top-level html, body, all we do is add attributes to 
                        // the already create nodes. head tags get ignored
                        if ( (attributes & TagAttributes.TopLevel) > 0 ) {
                            if (tok == "html" && htmlNode != null) {
                                AddAttributes(dom, htmlNode, tok, reader);
                                break;
                            }
                            if (tok == "body" && bodyNode != null) {
                                AddAttributes(dom, bodyNode, tok, reader);
                                break;
                            }
                            break;
                        }

                        // Create the new tag, add attributes, and append to DOM
                        DomNode tag = dom.AddElement(currNode, tok);
                        AddAttributes(dom, tag, tok, reader, baseUrl);

                        // If this is a self closing tag, we are done. Don't move pointer.
                        if ((attributes & TagAttributes.SelfClosing) > 0)
                            break;

                        // If this is an RCData tag, get the text value for it and add it.
                        if ((attributes & TagAttributes.RCData) > 0) {
                            tok = reader.ReadRCData(tok);
                            dom.AddText(tag, tok);
                            break;
                        }

                        // Set current tag pointer to the newly added tag
                        currNode = tag;

                        break;

                    case ParseState.CloseTag:

                        // Look up our ancestor chain for the corresponding open tag
                        DomNode parent = dom.FindAncestor(currNode, tok);
                        currNode = parent != null ? parent : currNode;

                        // If we moved up beyond the body (for example if there were tags or text 
                        // after the </html> tag - set the pointer to the <body> tag. 
                        if (Equals(currNode, htmlNode) || Equals(currNode, dom.RootNode))
                            if (bodyNode != null)   // possible we don't have a body node
                                currNode = bodyNode;

                        break;

                }
                
            }

        }

        // Read all attributes onto the given tag...
        private static void AddAttributes(DomBuilder<DomNode> dom, DomNode tag, string tagName, HtmlTextReader reader, string originatingUrl=null)
        {
            while (reader.ParseState == ParseState.AttributeName)
            {
                // Get the attribute Name and Value
                string attrName = reader.ReadAttributeName();
                string attrValue = reader.ParseState == ParseState.AttributeValue ? reader.ReadAttributeValue() : string.Empty;

                // Make sure we have a value for the attribute name
                if (string.IsNullOrEmpty(attrName))
                    continue;

                // Make sure the attribute name is a valid XML name...
                attrName = XmlConvert.EncodeLocalName(attrName);

                // Values can have html encodings - we want them decoded 
                attrValue = HtmlDecode(attrValue);

                // Fully-qualify UrlAttributes
                if (originatingUrl != null &&
                    ((tagName == "a" && attrName == "href") || (tagName == "img" && attrName == "src")) &&
                        !attrValue.Contains("://"))
                {
                    Uri baseUri = new Uri(originatingUrl);
                    Uri compbinedUri = new Uri(baseUri, attrValue);
                    attrValue = compbinedUri.ToString();
                }

                // Add the attribute, if it does not already exist
                dom.AddAttribute(tag, attrName, attrValue);

            }
            
        }

        private static string HtmlDecode(string htmlText)
        {
#if net20 || net35
            return System.Web.HttpUtility.HtmlDecode(htmlText);
#else
            return System.Net.WebUtility.HtmlDecode(htmlText);
#endif
        }

        private static bool IsNullOrWhiteSpace(String value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }
    }
}
