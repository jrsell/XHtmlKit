﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

namespace XHtmlKit
{
    public class HtmlParserOptions
    {
        public string BaseUrl = null;
    }

    public enum InsersionMode
    {
        BeforeHtml,
        BeforeHead,
        InHead,
        InBody
    }
    
    public class HtmlStreamParser<DomNode>
    {
        private enum TagProperties
        {
            None = 0,
            SelfClosing = 1,
            IsHeadTag = 2,
            TopLevel = 4,
            RCData = 8,
        }

        private static Dictionary<string, TagProperties> _tagProperties = new Dictionary<string, TagProperties>()
        {
            {"area", TagProperties.SelfClosing},
            {"base", TagProperties.SelfClosing | TagProperties.IsHeadTag},
            {"basefont", TagProperties.SelfClosing | TagProperties.IsHeadTag},
            {"bgsound", TagProperties.SelfClosing | TagProperties.IsHeadTag},
            {"body", TagProperties.TopLevel},
            {"br", TagProperties.SelfClosing},
            {"col", TagProperties.SelfClosing},
            {"command", TagProperties.SelfClosing},
            {"embed", TagProperties.SelfClosing},
            {"head", TagProperties.TopLevel},
            {"hr", TagProperties.SelfClosing},
            {"html", TagProperties.TopLevel},
            {"iframe", TagProperties.RCData},
            {"img", TagProperties.SelfClosing},
            {"input", TagProperties.SelfClosing},
            {"keygen", TagProperties.SelfClosing},
            {"link", TagProperties.SelfClosing | TagProperties.IsHeadTag},
            {"menuitem", TagProperties.SelfClosing},
            {"meta", TagProperties.SelfClosing | TagProperties.IsHeadTag},
            {"noembed", TagProperties.RCData},
            {"noframes", TagProperties.IsHeadTag},
            {"noscript", TagProperties.IsHeadTag | TagProperties.RCData},
            {"param", TagProperties.SelfClosing},
            {"script", TagProperties.IsHeadTag | TagProperties.RCData},
            {"source", TagProperties.SelfClosing},
            {"style", TagProperties.IsHeadTag | TagProperties.RCData},
            {"template", TagProperties.IsHeadTag},
            {"textarea", TagProperties.RCData},
            {"title", TagProperties.IsHeadTag | TagProperties.RCData},
            {"track", TagProperties.SelfClosing},
            {"wbr", TagProperties.SelfClosing},
            {"xmp", TagProperties.RCData}
        };

        

        public void Parse(DomBuilder<DomNode> dom, HtmlTextReader reader, HtmlParserOptions options, InsersionMode mode = InsersionMode.BeforeHtml)
        {
            HtmlParserOptions parserOptions = options == null ? new HtmlParserOptions() : options;

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

                        // Look up the properties of the tag
                        TagProperties properties = TagProperties.None;
                        _tagProperties.TryGetValue(tok, out properties);

                        // For top-level <html>, & <body> tags, all we do is add attributes to 
                        // the already create nodes. <head> tags get ignored
                        if ( (properties & TagProperties.TopLevel) > 0 ) {
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
                        string tagName = EncodeLocalName(tok);
                        DomNode tag = dom.AddElement(currNode, tagName);
                        AddAttributes(dom, tag, tagName, reader, parserOptions.BaseUrl);
                        
                        // If this is a meta tag, and our underlying stream
                        // lets us ReWind, and we are tentative about the encoding, then check for a new charset
                        if (((properties & TagProperties.IsHeadTag) > 0) && (reader.CurrentEncodingConfidence == EncodingConfidence.Tentative) && reader.CanRewind && (tok == "meta"))
                        {
                            Encoding encoding = CheckForNewEncoding(tag, dom);
                            
                            // If we found a new encoding encoding, we will need to start over!
                            if (encoding != null /* TODO: and if it is different from current one ? */)
                            {
                                // Start over!
                                reader.Rewind(encoding); // Rewind underlying stream, set new encoding
                                dom.RemoveAll(dom.RootNode); // Clear DOM so far
                                Parse(dom, reader, options, mode); // Re-parse from scratch
                                return;
                            }
                        }                        

                        // If this is a self closing tag, we are done. Don't move pointer.
                        if ((properties & TagProperties.SelfClosing) > 0)
                            break;

                        // If this is an RCData tag, get the text value for it and add it.
                        if ((properties & TagProperties.RCData) > 0) {
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

        private static Encoding CheckForNewEncoding(DomNode tag, DomBuilder<DomNode> dom)
        {
            // Parse out the 'charset' from the <meta> tag
            string charset = dom.GetAttribute(tag, "charset");
            string httpEquiv = dom.GetAttribute(tag, "http-equiv");
            string content = dom.GetAttribute(tag, "content");
            charset = EncodingUtils.GetCharset(charset, httpEquiv, content);

            // Look up the encoding
            return EncodingUtils.GetEncoding(charset);
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
                attrName = EncodeLocalName(attrName);

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

        private static string EncodeLocalName(string name)
        {
            return ValidateXmlName(name) ? name : XmlConvert.EncodeLocalName(name);
        }

        private static bool ValidateXmlName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i == 0 && !XmlConvert.IsStartNCNameChar(c))
                    return false;

                if (!XmlConvert.IsNCNameChar(c))
                    return false;
            }

            return true;
        }

        private static string HtmlDecode(string htmlText)
        {
#if net20 || net35
            return System.Web.HttpUtility.HtmlDecode(htmlText);
#else
            return System.Net.WebUtility.HtmlDecode(htmlText);
#endif
        }

        private static bool IsHeadTag(string tag)
        {
            TagProperties attrs = TagProperties.None;
            _tagProperties.TryGetValue(tag, out attrs);
            bool isHeadTag = (attrs & TagProperties.IsHeadTag) > 0;
            return isHeadTag;
        }

        private static bool IsNullOrWhiteSpace(String value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++) {
                if (!Char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }
    }
}
