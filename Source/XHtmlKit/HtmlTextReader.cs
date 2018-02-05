using System;
using System.Text;
using System.IO;

namespace XHtmlKit
{
    public enum EncodingConfidence
    {
        Tentative, 
        Certain,
        Irrelevant
    }

    public enum ParseState
    {
        Comment,
        OpenTag,
        AttributeName,
        AttributeValue,
        CloseTag,
        RCData,
        Text,
        Done
    }

    public static class EncodingUtils
    {
        private static System.Collections.Generic.Dictionary<string, string> _encodingAliases = new System.Collections.Generic.Dictionary<string, string>() {
            {"euc-kr", "windows-949"},
            {"euc-jp", "cp51932"},
            {"gb2312", "gbk"},
            {"gb_2312-80", "gbk"},
            {"iso-8859-1", "windows-1252"},
            {"iso-8859-9", "windows-1254"},
            {"iso-8859-11", "windows-874"},
            {"ks_c_5601-1987", "windows-949"},
            {"shift_jis", "Windows-31J"},
            {"tis-620", "windows-874"},
            {"us-ascii", "windows-1252"} };

        public static string GetCharset(string charset, string httpEquiv, string content)
        {
            // HTML5: <meta charset="UTF-8">
            // We passed in a value for the charset - so look it up
            if (!string.IsNullOrEmpty(charset))
                return charset;

            // HTML 4.0.1: <meta http-equiv="content-type" content="text/html; charset=UTF-8">
            if (httpEquiv.ToLower().Trim() == "content-type" && !string.IsNullOrEmpty(content)) {
                var match = System.Text.RegularExpressions.Regex.Match(content, "charset\\s*=[\\s\"']*([^\\s\"' />]*)");
                if (match.Success) {
                    return match.Groups[1].Value;
                }
            }

            // No charset found
            return string.Empty;        
        }

        /// <summary>
        /// Look up the coding. Returns null if it wasn't found.
        /// </summary>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(string charset)
        {
#if netstandard
            // Ug... not all encodings are available with NetStandard by default 
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            string charsetToFind = (charset == null) ? String.Empty : charset.ToLower().Trim();

            // Apply charset alias as per spec...
            if (_encodingAliases.ContainsKey(charsetToFind)) {
                charsetToFind = _encodingAliases[charsetToFind];
            }

            // Look up the encoding in the list of Registered Providers
            Encoding retval = null;
            try {
                retval = Encoding.GetEncoding(charsetToFind);
            }
            catch {
                retval = null;
            }

            return retval;
        }

    }

    /// <summary>
    /// A fast & memory-efficient, HTML Tag Tokenizer.
    /// </summary>
    public class HtmlTextReader: IDisposable
    {
        private EncodingConfidence _encodingConfidence;
        private Encoding _initialEncoding;
        private StreamReader _streamReader;
        private TextReader _reader;
        private ParseState _parseState;
        private StringBuilder _currTok;
        private int _peekChar = 0;

        public HtmlTextReader(string html)
        {
            _initialEncoding = null;
            _encodingConfidence = EncodingConfidence.Irrelevant;
            _streamReader = null;
            _reader = new StringReader(html);
            _currTok = new StringBuilder();
            _parseState = ParseState.Text;
            _peekChar = _reader.Read();
        }

        public HtmlTextReader(TextReader reader)
        {
            _initialEncoding = null;
            _encodingConfidence = EncodingConfidence.Irrelevant;
            _streamReader = null;
            _reader = reader;
            _currTok = new StringBuilder();
            _parseState = ParseState.Text;
            _peekChar = _reader.Read(); 
        }
        
        public HtmlTextReader(Stream stream, Encoding encoding = null )
        {
            _initialEncoding = encoding == null ? new UTF8Encoding() : encoding ;
            _encodingConfidence = encoding == null ? EncodingConfidence.Tentative : EncodingConfidence.Certain;
            _streamReader = new StreamReader(stream, _initialEncoding, (encoding == null) );
            _reader = _streamReader;
            _currTok = new StringBuilder();
            _parseState = ParseState.Text;
            _peekChar = _reader.Read();
        }

        public TextReader BaseReader
        {
            get { return _reader; }
        }

        public Encoding CurrentEncoding
        {
            get
            {
                if (_streamReader == null)
                    return null;

                return _streamReader.CurrentEncoding;
            }
            set
            {
                if (_streamReader == null)
                    throw new Exception("Internal Error. Cannot change the encoding.");

                // Switch out the underlying StreamReader with the new encoding
                // and set the confidence to Certain... 
                _streamReader = new StreamReader(_streamReader.BaseStream, value);
                _encodingConfidence = EncodingConfidence.Certain;
            }
        }

        public EncodingConfidence CurrentEncodingConfidence
        {
            get
            {
                // If the encoding confidence was initially Tentative, check to see if the 
                // StreamReader was able to detect a Byte order Mark. If so, we are now confident.
                if (_encodingConfidence == EncodingConfidence.Tentative) {
                    if (!object.Equals(CurrentEncoding, _initialEncoding)) {
                        _encodingConfidence = EncodingConfidence.Certain;
                    }                
                }

                return _encodingConfidence;
            }
        }


        public ParseState ParseState
        {
            get { return _parseState; }
        }

        private int Read()
        {
            int retval = _peekChar;
            _peekChar = _reader.Read();
            return retval;
        }

        public int Peek()
        {
            return _peekChar;
        }

        /// <summary>
        /// Reads the next html tag
        /// </summary>
        public string ReadNext()
        {
            switch (_parseState)
            {
                case ParseState.Text:
                    return ReadText();
                case ParseState.OpenTag:
                    return ReadTagName();
                case ParseState.AttributeName:
                    return ReadAttributeName();
                case ParseState.AttributeValue:
                    return ReadAttributeValue();
                case ParseState.CloseTag:
                    return ReadTagName();
                case ParseState.Comment:
                    return ReadComment();
            }
            return string.Empty;
        }

        /// <summary>
        /// Reads Text, up to the next Html tag
        /// </summary>
        /// <returns></returns>
        public string ReadText()
        {
            int l=-1;
            int c;
            char p;

            _currTok.Length = 0;

            while (true) {

                // Keep reading
                c = Read();
                p = (char)_peekChar;

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }                    
                // Open Tag: <A
                if (c == '<' && char.IsLetter(p)) {
                    _parseState = ParseState.OpenTag;
                    break;
                }
                // Comment: <! > or <!-- -->
                if (l == '<' && c == '!') {
                    _parseState = ParseState.Comment;
                    _currTok.Length--;
                    break;
                }
                // Bogus Comment type: </[NonLetter]>, eg: </ >, </@>, </_> 
                if (l == '<' && c == '/' && !char.IsLetter(p) ) {
                    _parseState = ParseState.Comment;
                    _currTok.Length--;
                    break;
                }
                // Bogus Comment type: <? >
                if (c == '<' && p == '?') {
                    _parseState = ParseState.Comment;
                    break;
                }
                // Close Tag: </A
                if ((l == '<') && (c == '/') && char.IsLetter(p)) {
                    _parseState = ParseState.CloseTag;
                    _currTok.Length--;
                    break;
                }

                // Accumulate text
                _currTok.Append((char)c); l=c;
            }

            return _currTok.ToString();
        }
        
        /// <summary>
        /// Reads Raw Character data as-is, up to the given end tag
        /// </summary>
        /// <param name="endTagName"></param>
        /// <returns></returns>
        public string ReadRCData(string endTagName)
        {
            string rcdata = ReadTo("</" + endTagName); 
            _parseState = ParseState.Text;
            SkipTo('>');
            return rcdata;
        }

        /// <summary>
        /// Read until we get EOF, or the given match
        /// </summary>
        public string ReadTo(string needle)
        {
            int c;
            int len = needle.Length;
            _currTok.Length = 0;
            while (true)
            {
                // Keep reading
                c = Read();

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }

                // Matched the last char 
                if (char.ToLowerInvariant((char)c) == char.ToLowerInvariant(needle[len - 1]))
                {
                    int matches = 1;

                    // Look backwards for full match
                    for (int i = len - 2, j = _currTok.Length - 1;
                            (i >= 0) && (j >= 0) && (char.ToLowerInvariant(needle[i]) == char.ToLowerInvariant(_currTok[j]));
                                i--, j--) {
                        matches++;
                    }

                    // Found a full match 
                    if (matches == len) {
                        _currTok.Length -= (matches-1);  // Shorten output 
                        break;
                    }
                }

                // Accumulate output
                _currTok.Append((char)c);
            }

            return _currTok.ToString();
        }

        /// <summary>
        /// Skip until we get EOF, or the given match
        /// </summary>
        public int SkipTo(char needle)
        {
            int c=-1;
            while (true)
            {
                // Keep reading
                c = Read();

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }
                // Matched 
                if (c == needle)
                    break;
            }
            return c;

        }

        /// <summary>
        /// Read until we get EOF, or the given match
        /// </summary>
        public string ReadTo(char needle)
        {
            int c;
            _currTok.Length = 0;
            while (true)
            {
                // Keep reading
                c = Read();

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }
                // Matched 
                if (c == needle)
                    break;

                // Accumulate output
                _currTok.Append((char)c);
            }

            return _currTok.ToString();
        }

        /// <summary>
        /// Reads a comment tag
        /// </summary>
        /// <returns></returns>
        public string ReadComment()
        {
            int c, len;
            bool fullcomment = false;
            _currTok.Length = 0;
            while (true)
            {
                // Keep reading
                c = Read();
                len = _currTok.Length;

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }
                // End of a regular comment
                if ((c == '>') && !fullcomment)
                    break;
                // See if we are a full comment. Comment starts with '<!--'
                if ((len == 1) && (c == '-') && (_currTok[0] == '-')) {
                    fullcomment = true;
                    _currTok.Length = 0;
                    continue; // don't accumulate this '-' character
                }
                // Look for end of a full comment "-->" 
                if ((len >= 2) && (c == '>') && fullcomment && (_currTok[len - 1] == '-') && (_currTok[len - 2] == '-')) {
                    _currTok.Length -= 2;
                    break;
                }
                // Look for end of a full comment "--!>" end-bang style
                if ((len >= 3) && (c == '>') && fullcomment && (_currTok[len - 1] == '!') && (_currTok[len - 2] == '-') && (_currTok[len - 3] == '-')) {
                    _currTok.Length -= 3;
                    break;
                }

                // Accumulate output
                _currTok.Append((char)c);
            }

            _parseState = ParseState.Text;
            return _currTok.ToString();
            
        }

        public int SkipWhiteSpaceAndForwardSlash()
        {
            int c;

            // Skip whitespace
            while (true)
            {
                // Peek ahead
                c = _peekChar;

                // EOF
                if (c < 0)
                    break;
                // Whitesapce or Forward Slash
                if (! (char.IsWhiteSpace((char)c) || c == '/') )
                    break;

                Read();
            }

            return c;
        }

        public int SkipWhiteSpace()
        {
            int c;

            // Skip whitespace
            while (true)
            {
                // Peek ahead
                c = _peekChar;

                // EOF
                if (c < 0)
                    break;
                // Whitesapce
                if ( ! char.IsWhiteSpace( (char)c )  )
                    break;

                Read();
            }

            return c;
        }

        /// <summary>
        /// Reads the name of the current tag 
        /// </summary>
        public string ReadTagName()
        {
            int c;
            _currTok.Length = 0;
            while (true)
            {
                // Keep reading
                c = Read();

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }
                // End-of-tag
                if (c == '>') {
                    _parseState = ParseState.Text;  // Check for script & style
                    break;
                }
                // Whitespace 
                if (char.IsWhiteSpace((char)c) || (c == '/') ) {
                    if (_parseState == ParseState.OpenTag)
                        _parseState = ParseState.AttributeName;
                    else if (_parseState == ParseState.CloseTag) {
                        _parseState = ParseState.Text;
                        SkipTo('>');
                    }
                    break;
                }

                // Accumulate
                _currTok.Append(char.ToLowerInvariant((char)c));
            }

            // Return the tag in lower-case
            return _currTok.ToString();
        }

        public string ReadAttributeName()
        {
            int c;

            c = SkipWhiteSpaceAndForwardSlash();

            // Next up is a non-whitespace, non forwardslash character. 
            // Accumulate the attribute name, until we get [Whitespace], '=', '/>', or '>'
            _currTok.Length = 0;
            while (true)
            {
                // Keep reading
                c = Read();

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }
                // End-of-tag
                if (c == '>') {
                    _parseState = ParseState.Text;  
                    break;
                }
                // End-of self closing tag
                if (c == '/' && _peekChar == '>') {
                    Read(); // Consume '>' 
                    _parseState = ParseState.Text;
                    break;
                }

                // Accumulate
                _currTok.Append((char)c);

                // Peek ahead - see if next character is '=', [Whitespace], or '/'
                c = _peekChar;
                if ((c == '=') || char.IsWhiteSpace((char)c) || (c == '/') ) {
                    _parseState = ParseState.AttributeValue;
                    break;
                }

            }

            return _currTok.ToString();
        }

        public string ReadAttributeValue()
        {
            int c;
            _currTok.Length = 0;

            // Skip any whitespace
            c = SkipWhiteSpace();

            // EOF
            if (c < 0) {
                _parseState = ParseState.Done;
                return string.Empty; 
            }
            // Ensure we got an '='
            if (c != '=') {
                _parseState = ParseState.AttributeName;
                return String.Empty;
            }

            // Consume the '='
            Read();

            // Skip any whitespace
            c = SkipWhiteSpace();

            // Keep reading
            c = Read();

            // EOF
            if (c < 0) {
                _parseState = ParseState.Done;
                return string.Empty;
            }

            // End of tag... We are done.
            if (c == '>') {
                _parseState = ParseState.Text; 
                return string.Empty;
            }

            // Single-quoted attribute value. Read to closing single-quote.        
            if (c == '\'') {
                _parseState = ParseState.AttributeName;
                return ReadTo('\'');
            }

            // Double-quoted attribute value. Read to closing double-quote.
            if (c == '"') {
                _parseState = ParseState.AttributeName;
                return ReadTo('"');
            }

            // Unquoted attribute value. Read to '>' or whitespace.  
            while (true)
            {
                // Accumulate
                _currTok.Append((char)c);

                // Peek ahead
                c = _peekChar;

                // EOF
                if (c < 0) {
                    _parseState = ParseState.Done;
                    break;
                }
                // End-of-tag
                if (c == '>') {
                    Read(); // eat '>'
                    _parseState = ParseState.Text; // Check for script & style
                    break;
                }
                // Whitespace
                if (char.IsWhiteSpace((char)c))
                    break;

                // Keep reading
                Read();
            }

            return _currTok.ToString();
            
        }

        public void Dispose()
        {
            ((IDisposable)_reader).Dispose();
        }
    }
}
