using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace XHtmlKit.Tests
{

    [TestClass]
    public class GenerateTagAttributes
    {
        private enum TagAttributes
        {
            None = 0,
            SelfClosing = 1,
            IsHeadTag = 2,
            TopLevel = 4,
            RCData = 8,
        }

        private static string[] _rcDataTags = new string[] {
            "title", "textarea", "style", "xmp", "iframe", "noembed", "script", "noscript" };

        private static string[] _selfClosing = new string[] {
            "area", "base", "basefont", "bgsound", "br", "col", "command", "embed", "hr", "img",
            "input", "keygen", "link", "menuitem", "meta", "param", "source", "track", "wbr" };

        private static string[] _headTag = new string[] {
            "base", "basefont", "bgsound", "link", "meta", "title", "noframes", "noscript", "script", "style", "template" };

        private static string[] _topLevelTag = new string[] {
            "html", "head", "body" };

        [TestMethod]
        public void Generate()
        {
            SortedList<string, TagAttributes> tags = new SortedList<string, TagAttributes>();
            AddTags(tags, _rcDataTags, TagAttributes.RCData);
            AddTags(tags, _selfClosing, TagAttributes.SelfClosing);
            AddTags(tags, _headTag, TagAttributes.IsHeadTag);
            AddTags(tags, _topLevelTag, TagAttributes.TopLevel);

            foreach (string tag in tags.Keys)
            {
                Console.Write("{\"" + tag + "\", ");
                TagAttributes attrMask = tags[tag];
                string attrMaskString = "";
                foreach (TagAttributes attrValue in Enum.GetValues(typeof(TagAttributes)))
                {
                    if ((attrMask & attrValue) > 0)
                        attrMaskString += ((attrMaskString.Length > 0) ? " | " : "") + "TagAttributes." + attrValue.ToString();
                }
                Console.WriteLine(attrMaskString + "}," );
            }
        }

        private void AddTags(SortedList<string, TagAttributes> tagCollection, string[] tags, TagAttributes attr)
        {
            foreach (string tag in tags) {
                if (!tagCollection.ContainsKey(tag))
                    tagCollection.Add(tag, TagAttributes.None);
                tagCollection[tag] = tagCollection[tag] | attr;
            }
        }

        

        
    }
}
