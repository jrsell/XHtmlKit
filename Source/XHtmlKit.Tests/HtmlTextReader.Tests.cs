using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XHtmlKit;
using System.IO;
using System.Text;
using System.Collections.Generic;


namespace XHtmlKit.Parser.Tests
{
    [TestClass]
    public class HtmlTextReader_Tests
    {
        [TestMethod]
        public void ReadText()
        {
            string input;
            HtmlTextReader reader;
            string output;

            // Text to a comment
            input = "Hello Text<!-- a comment";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadText();
            Assert.AreEqual("Hello Text", output);
            Assert.AreEqual(ParseState.Comment, reader.ParseState);

            // Text to a comment with whitespace
            input = "   Hello Text<!-- a comment";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadText();
            Assert.AreEqual("   Hello Text", output);
            Assert.AreEqual(ParseState.Comment, reader.ParseState);

            // Text to an open tag
            input = "Hello Text<A>";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadText();
            Assert.AreEqual("Hello Text", output);
            Assert.AreEqual(ParseState.OpenTag, reader.ParseState);

            // Text to an open tag (bad character for tag, so treated as text)
            input = "Hello Text<1>";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadText();
            Assert.AreEqual("Hello Text<1>", output);
            Assert.AreEqual(ParseState.Done, reader.ParseState);

            // Text to a close tag
            input = "Hello Text</A>";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadText();
            Assert.AreEqual("Hello Text", output);
            Assert.AreEqual(ParseState.CloseTag, reader.ParseState);

            // Text to a close tag (bad char)
            input = "Hello Text</_>";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadText();
            Assert.AreEqual("Hello Text", output);
            Assert.AreEqual(ParseState.Comment, reader.ParseState);

        }

        [TestMethod]
        public void ReadComment()
        {
            string input;
            HtmlTextReader reader;
            string output;

            // Full comment
            input = "--Hello comment--> asdfaf";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello comment", output);

            // Full comment
            input = "--Hello \n\tcomment--> asdfaf";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello \n\tcomment", output);

            // Full comment
            input = "--Hello -> comment--> asdfaf";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello -> comment", output);

            // Full comment
            input = "--Hello - comment--> asdfaf";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello - comment", output);

            // Full comment
            input = "--Hello -- comment--> asdfaf";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello -- comment", output);

            // Basic 
            input = "Hello comment";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello comment", output);

            // Basic 
            input = "Hello comment>";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello comment", output);

            // Basic 
            input = "Hello comment>   ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello comment", output);

            // Basic
            input = "Hello comment--";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello comment--", output);

            // Basic
            input = "Hello comment-";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("Hello comment-", output);

            // Basic
            input = "-Hello comment->";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadComment();
            Assert.AreEqual("-Hello comment-", output);
        }

        [TestMethod]
        public void ReadUnquotedAttribute()
        {
            string input;
            HtmlTextReader reader;
            string output;

            // no whitespace
            input = "=abc";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Starting whitespace
            input = "  =abc";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Starting whitespace
            input = "  \t\n\r=abc";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Ending whitespace
            input = "=abc  ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Whitespace start and end
            input = "  = abc  ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Whitespace start and end
            input = "  =\t\n\rabc \n\r";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Whitespace end tag in the middle. Ensure
            // we don't consume the char.
            input = "=  abc>def  ";
            StringReader sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);
            Assert.AreEqual('d', (char)reader.Peek());
            Assert.AreEqual(ParseState.Text, reader.ParseState);

            // Ensure we don't consume the last space.
            input = "  =abcdef ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abcdef", output);
            Assert.AreEqual(' ', (char)reader.Peek());
        }

        public static Tuple<string, ParseState>[] ReadAll(HtmlTextReader reader)
        {
            List<Tuple<string, ParseState>> toks = new List<Tuple<string, ParseState>>();
            while (reader.ParseState != ParseState.Done)
            {
                toks.Add(new Tuple<string, ParseState>(reader.ReadNext(), reader.ParseState));
            }
            return toks.ToArray();
        }

        [TestMethod]
        public void ReadScript()
        {
            string input;
            HtmlTextReader reader;
            StringReader sr;

            // Basic open close tag with text
            input = "<Script type='javascript'> console.log('<a>some tag</a>') </script>";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual(ParseState.Text, reader.ParseState); Assert.AreEqual("", reader.ReadNext());
            Assert.AreEqual(ParseState.OpenTag, reader.ParseState); Assert.AreEqual("script", reader.ReadNext());
            Assert.AreEqual(ParseState.AttributeName, reader.ParseState); Assert.AreEqual("type", reader.ReadNext());
            Assert.AreEqual(ParseState.AttributeValue, reader.ParseState); Assert.AreEqual("javascript", reader.ReadNext());
            Assert.AreEqual(ParseState.AttributeName, reader.ParseState); Assert.AreEqual("", reader.ReadNext());
            Assert.AreEqual(ParseState.Text, reader.ParseState); Assert.AreEqual(" console.log('<a>some tag</a>') ", reader.ReadRCData("script"));
            Assert.AreEqual(ParseState.Text, reader.ParseState); Assert.AreEqual("", reader.ReadNext());
            Assert.AreEqual(ParseState.Done, reader.ParseState);
        }

        [TestMethod]
        public void ReadNext()
        {
            string input;
            HtmlTextReader reader;
            StringReader sr;

            // Basic open close tag with text
            input = " <a href='www.foobar.com'>Click me</a> ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);

            // Starts in text
            Assert.AreEqual(ParseState.Text, reader.ParseState);

            // There was no text, so should be empty
            Assert.AreEqual(" ", reader.ReadNext());

            // Open tag
            Assert.AreEqual(ParseState.OpenTag, reader.ParseState);
            Assert.AreEqual("a", reader.ReadNext());

            // href
            Assert.AreEqual(ParseState.AttributeName, reader.ParseState);
            Assert.AreEqual("href", reader.ReadNext());

            // www.foobar.com
            Assert.AreEqual(ParseState.AttributeValue, reader.ParseState);
            Assert.AreEqual("www.foobar.com", reader.ReadNext());

            // Need to keep reading attributes until we get an empty one
            Assert.AreEqual(ParseState.AttributeName, reader.ParseState);
            Assert.AreEqual(String.Empty, reader.ReadNext());

            // Click me
            Assert.AreEqual(ParseState.Text, reader.ParseState);
            Assert.AreEqual("Click me", reader.ReadNext());

            // Close tag
            Assert.AreEqual(ParseState.CloseTag, reader.ParseState);
            Assert.AreEqual("a", reader.ReadNext());

            Assert.AreEqual(ParseState.Text, reader.ParseState);

        }

        [TestMethod]
        public void ReadToMatch()
        {
            string input;
            HtmlTextReader reader;
            StringReader sr;

            // Found
            input = "abcabc-->  ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("abcabc", reader.ReadTo("-->"));
            Assert.AreEqual(' ', sr.Read());

            // Not found
            input = "abcabc--  ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("abcabc--  ", reader.ReadTo("-->"));
            Assert.AreEqual(-1, sr.Read());

            // Found with multiple matches
            input = "abcabc---->";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("abcabc--", reader.ReadTo("-->"));
            Assert.AreEqual(-1, sr.Read());
        }


        

        [TestMethod]
        public void ReadQuotedAttribute()
        {
            string input;
            HtmlTextReader reader;
            string output;

            // Ensure we parse to end of single quotes...
            input = " ='abc'  ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Ensure we parse to end of double quotes...
            input = " =\"abc\"  ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc", output);

            // Ignore double quotes in the middle
            input = " ='abc\"def'  ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc\"def", output);

            // Ignore end tag in the middle of quotes
            input = " ='abc>def'  ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc>def", output);

            // Parse until end of string with open quote
            input = "= 'abcdef ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abcdef ", output);

            // Parse until end of string with open quote
            input = "= \"abcdef ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abcdef ", output);

            // Not checking whitespace inside quotes
            input = " =\"abcdef ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abcdef ", output);

            // Ignore single quotes in the middle of double quotes
            input = " =\"abc'def\" ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc'def", output);

            // Ignore end tag in the middle of quotes
            input = " =\"abc>def\" ";
            reader = new HtmlTextReader(new StringReader(input));
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abc>def", output);

            // Ensure we don't consume the >.
            input = "  ='abcdef'>";
            StringReader sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeValue();
            Assert.AreEqual("abcdef", output);
            Assert.AreEqual('>', (char)reader.Peek());

        }

        [TestMethod]
        public void ReadAttrName()
        {
            string input;
            StringReader sr;
            HtmlTextReader reader;
            string output;

            // no whitespace
            input = "class='red' ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeName();
            Assert.AreEqual("class", output);
            Assert.AreEqual('=', (char)reader.Peek());
            Assert.AreEqual(ParseState.AttributeValue, reader.ParseState);

            // whitespace
            input = "   class='red' ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeName();
            Assert.AreEqual("class", output);
            Assert.AreEqual('=', (char)reader.Peek());

            // whitespace
            input = "   class   = 'red' ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeName();
            Assert.AreEqual("class", output);
            Assert.AreEqual(' ', (char)reader.Peek());

            // whitespace
            input = "   class   = \"red\" ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeName();
            Assert.AreEqual("class", output);
            Assert.AreEqual(' ', (char)reader.Peek());

            // missing attr. the rest is val
            input = "   =red foo=bar ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            output = reader.ReadAttributeName();
            Assert.AreEqual("=red", output);
            Assert.AreEqual(' ', (char)reader.Peek());

        }

        [TestMethod]
        public void ReadAttrNameAndVal()
        {
            string input;
            StringReader sr;
            HtmlTextReader reader;

            input = "class='red' ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual("red", reader.ReadAttributeValue());

            input = "class = \"red\" ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual("red", reader.ReadAttributeValue());

            input = "   class =red ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual("red", reader.ReadAttributeValue());

            input = "   class =  red ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual("red", reader.ReadAttributeValue());

            input = "   class =  'red' ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual("red", reader.ReadAttributeValue());

            // Make sure forward slashes are skipped in the right places...
            input = " /class = /red ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual("/red", reader.ReadAttributeValue());

            // Make sure forward slashes are skipped in the right places...
            input = " class/ = red ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual("", reader.ReadAttributeValue());
            Assert.AreEqual("=", reader.ReadAttributeName());
            Assert.AreEqual("", reader.ReadAttributeValue());
            Assert.AreEqual("red", reader.ReadAttributeName());
            Assert.AreEqual("", reader.ReadAttributeValue());

            // Make sure forward slashes are skipped in the right places...
            input = " /class /= /red ";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            Assert.AreEqual("class", reader.ReadAttributeName());
            Assert.AreEqual(String.Empty, reader.ReadAttributeValue());
            Assert.AreEqual("=", reader.ReadAttributeName());
            Assert.AreEqual(String.Empty, reader.ReadAttributeValue());
            Assert.AreEqual("red", reader.ReadAttributeName());
            Assert.AreEqual(String.Empty, reader.ReadAttributeValue());

        }

        [TestMethod]
        public void ReadAllAttributes()
        {
            string input;
            string output;

            input = " class='red'   id='123' foo=bar ";
            output = ReadAllAttributes(input);
            Assert.AreEqual("class='red' id='123' foo='bar'", output);

            input = " class='red'   id='123' foo=bar ";
            output = ReadAllAttributes(input);
            Assert.AreEqual("class='red' id='123' foo='bar'", output);

            input = " =red   id='123' foo=bar ";
            output = ReadAllAttributes(input);
            Assert.AreEqual("=red='' id='123' foo='bar'", output);

            input = " a=b id='123 foo=bar ";
            output = ReadAllAttributes(input);
            Assert.AreEqual("a='b' id='123 foo=bar '", output);

            input = " rel='canonical' href='http://www.helloworld.com' />";
            output = ReadAllAttributes(input);
            Assert.AreEqual("rel='canonical' href='http://www.helloworld.com'", output);

        }

        private static string ReadAllAttributes(string input)
        {
            StringBuilder output = new StringBuilder();
            StringReader sr = new StringReader(input);
            HtmlTextReader reader = new HtmlTextReader(sr);
            while (sr.Peek() >= 0)
            {
                string name = reader.ReadAttributeName();
                string value = reader.ReadAttributeValue();
                if (!string.IsNullOrEmpty(name)) {
                    output.Append(output.Length > 0 ? " " : "");
                    output.Append(name + "='" + value + "'");
                }
            }
            return output.ToString();
        }

        /*
        [TestMethod]
        public void Test4_ReadTag()
        {
            string input;
            StringReader sr;
            HtmlTextReader reader;

            input = "class='red' id='123'";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            XmlElement tag = reader.ReadTag();
            Assert.AreEqual("<a class=\"red\" id=\"123\" />", tag.OuterXml);

            // Name must get encoded
            input = "@class='red' id='123'";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            tag = reader.ReadTag();
            Assert.AreEqual("<a _x0040_class=\"red\" id=\"123\" />", tag.OuterXml);

            // special char in Value 
            input = "class='red' id='12>3'";
            sr = new StringReader(input);
            reader = new HtmlTextReader(sr);
            tag = reader.ReadTag();
            Assert.AreEqual("<a class=\"red\" id=\"12&gt;3\" />", tag.OuterXml);

        }

    */
    }
}
