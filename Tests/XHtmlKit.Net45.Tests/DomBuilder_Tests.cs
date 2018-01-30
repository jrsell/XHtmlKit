using System;
using NUnit.Framework;
using System.IO;
using XHtmlKit;
using System.Threading.Tasks;
using System.Xml;

namespace XHtmlKit.DomBuilder.Tests
{
    [TestFixture]
    public class DomBuilder_Tests
    {


        /// <summary>
        /// Basic html document.
        /// </summary>
        [Test]
        public void HelloWorldBasicTest()
        {
            XmlDocument doc = new XmlDocument();
            XmlDomBuilder builder = new XmlDomBuilder(doc);
            XmlNode htmlElem = builder.AddElement(builder.RootNode, "html");
            XmlNode bodyElem = builder.AddElement(htmlElem, "body");
            XmlNode anchor = builder.AddElement(bodyElem, "a");
            builder.AddAttribute(anchor, "class", "bold");
            builder.AddAttribute(anchor, "class", "red");
            builder.AddText(anchor, "Some Anchor Text");
            Console.WriteLine(doc.OuterXml);

            // Ensure we don't repeat attribute values
            Assert.AreEqual("bold", doc.SelectSingleNode("//a/@class").Value);

            // Just check full output...
            Assert.AreEqual("<html><body><a class=\"bold\">Some Anchor Text</a></body></html>", doc.OuterXml);
        }

    }


    }
