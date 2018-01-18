using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Net;

namespace XHtmlKit
{
    public class HtmlStreamParser: HtmlParser
    {
        private enum InsersionMode
        {
            BeforeHtml,
            BeforeHead,
            InHead,
            InBody
        }

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

        public override void LoadHtml(XmlDocument doc, string html, string originatingUrl=null)
        {
            LoadHtml(doc, new StringReader(html), originatingUrl);
        }

        public override void LoadHtml(XmlDocument doc, TextReader htmlTextReader, string originatingUrl=null)
        {
            // Create HtmlTextReader for html tag tokenization
            HtmlTextReader reader = new HtmlTextReader(htmlTextReader);

            // DOM Node pointers
            XmlNode currTag = doc;
            XmlNode htmlNode = null; 
            XmlNode headNode = null; 
            XmlNode bodyNode = null;

            // Initial insertion mode is BeforeHtml;
            InsersionMode insertionMode = InsersionMode.BeforeHtml;

            while (reader.ParseState != ParseState.Done)
            {
                ParseState state = reader.ParseState;

                // Read the next token. Ignore empty tokens
                string tok = reader.ReadNext();
                if (string.IsNullOrWhiteSpace(tok))
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
                            htmlNode = doc.AppendChild(doc.CreateElement("html"));
                            currTag = htmlNode;
                            insertionMode = InsersionMode.BeforeHead;
                            break;
                        }

                        // Got a tag that should be in the head. Create html/head node structure... 
                        if (state == ParseState.OpenTag && IsHeadTag(tok)) {
                            htmlNode = doc.AppendChild(doc.CreateElement("html"));
                            headNode = htmlNode.AppendChild(doc.CreateElement("head"));
                            currTag = headNode;
                            insertionMode = InsersionMode.InHead;
                            break;
                        }

                        // Got anything else - put it in the body
                        else {
                            htmlNode = doc.AppendChild(doc.CreateElement("html"));
                            headNode = htmlNode.AppendChild(doc.CreateElement("head"));
                            bodyNode = htmlNode.AppendChild(doc.CreateElement("body"));
                            currTag = bodyNode;
                            insertionMode = InsersionMode.InBody;
                            break;
                        }

                    case InsersionMode.BeforeHead:

                        // Comment is valid 
                        if (state == ParseState.Comment)
                            break;

                        // <head> is valid here - switch to InHead
                        if (state == ParseState.OpenTag && tok == "head") {
                            headNode = htmlNode.AppendChild(doc.CreateElement("head"));
                            currTag = headNode;
                            insertionMode = InsersionMode.InHead;
                            break;
                        }

                        // Got a tag that 'should' be in the head. Create head... 
                        if (state == ParseState.OpenTag && IsHeadTag(tok)) {
                            headNode = htmlNode.AppendChild(doc.CreateElement("head"));
                            currTag = headNode;
                            insertionMode = InsersionMode.InHead;
                            break;
                        }

                        // Got anything else, including <body> - put it in the body
                        headNode = htmlNode.AppendChild(doc.CreateElement("head"));
                        bodyNode = htmlNode.AppendChild(doc.CreateElement("body"));
                        currTag = bodyNode;
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
                        bodyNode = htmlNode.AppendChild(doc.CreateElement("body"));
                        currTag = bodyNode;
                        insertionMode = InsersionMode.InBody;
                        break;
                }                

                // Add the token to the DOM
                switch (state)
                {
                    case ParseState.Comment:
                        // Xml Comments cannot have '--', and they cannot end in '-'
                        string commentText = tok.Replace("-", "#");
                        currTag.AppendChild(doc.CreateComment(commentText));
                        break; 

                    case ParseState.Text:
                        // Decode the text, to convert all encoded values (eg: '&gt;' to '>')
                        string textContent = WebUtility.HtmlDecode(tok);
                        currTag.AppendChild(doc.CreateTextNode(textContent)); 
                        break;
                    
                    case ParseState.OpenTag:

                        // Look up the attributes of the tag
                        TagAttributes attributes = TagAttributes.None;
                        _tagAttributes.TryGetValue(tok, out attributes);

                        // For top-level html, body, all we do is add attributes to 
                        // the already create nodes. head tags get ignored
                        if ( (attributes & TagAttributes.TopLevel) > 0 ) {
                            if (tok == "html") {
                                AddAttributes(doc, htmlNode, reader);
                                break;
                            }
                            if (tok == "body") {
                                AddAttributes(doc, bodyNode, reader);
                                break;
                            }
                            break;
                        }

                        // Create the new tag, add attributes, and append to DOM
                        XmlNode tag = null;
                        tag = doc.CreateElement(tok);                            
                        AddAttributes(doc, tag, reader);
                        currTag.AppendChild(tag);

                        // If this is a self closing tag, we are done. Don't move pointer.
                        if ((attributes & TagAttributes.SelfClosing) > 0)
                            break;

                        // If this is an RCData tag, get the text value for it and add it.
                        if ((attributes & TagAttributes.RCData) > 0) {
                            tok = reader.ReadRCData(tag.Name);
                            tag.AppendChild(doc.CreateTextNode(tok));
                            break;
                        }

                        // Set current tag pointer to the newly added tag
                        currTag = tag;

                        break;

                    case ParseState.CloseTag:

                        // Look up our ancestor chain for the corresponding open tag
                        XmlNode parent = currTag;
                        while (parent != null && parent.Name != tok)
                            parent = parent.ParentNode;

                        // If we found a match - move currTag up a level from the match
                        if (parent != null)
                            currTag = parent.ParentNode;

                        // If we moved up beyond the body (for example if there were tags or text 
                        // after the </html> tag - set the pointer to the <body> tag. 
                        if (currTag == htmlNode || currTag == doc)
                            currTag = bodyNode;

                        break;

                }
                
            }

        }

        // Read all attributes onto the given tag...
        private static void AddAttributes(XmlDocument doc, XmlNode tag, HtmlTextReader reader)
        {
            string tok = null;
            ParseState state; 
            XmlAttribute currAttr = null;
            while (true)
            {
                state = reader.ParseState;

                // Add attribute name, if it does not already exist on the tag
                if (state == ParseState.AttributeName)
                {
                    tok = reader.ReadNext();
                    if (string.IsNullOrEmpty(tok))
                        continue;

                    // Make sure the attribute name is a valid XML name...
                    string attrName = XmlConvert.EncodeLocalName(tok);

                    // Make sure the attribute does not already exist
                    currAttr = null;
                    if (tag.Attributes[attrName] != null)
                        continue;

                    // Add the attribute
                    currAttr = doc.CreateAttribute(attrName);
                    tag.Attributes.Append(currAttr);                    
                    continue;
                }

                // Add attribute value
                if (state == ParseState.AttributeValue)
                {
                    tok = reader.ReadNext();
                    if (string.IsNullOrEmpty(tok))
                        continue;

                    if (currAttr == null)
                        continue;

                    // Values can have html encodings - we want them decoded 
                    string attrValue = WebUtility.HtmlDecode(tok);
                    currAttr.Value = attrValue;                    
                    continue;
                }

                // The reader is now past the tag, so we are done adding attributes
                break;
            }
        }

    }
}
