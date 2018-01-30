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
            XmlDomBuilder builder = new XmlDomBuilder();
            XmlElement htmlElem = builder.AddElement(builder.RootNode, "html");
            XmlElement bodyElem = builder.AddElement(htmlElem, "body");
            XmlElement anchor = builder.AddElement(bodyElem, "a");
            builder.AddAttribute(anchor, "class", "bold");
            builder.AddAttribute(anchor, "class", "red");
            builder.AddText(anchor, "Some Anchor Text");
            XmlDocument output = builder.Document;
            Console.WriteLine(output.OuterXml);
        }

    }


    }
