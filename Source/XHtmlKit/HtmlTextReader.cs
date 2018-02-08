using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace XHtmlKit
{

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

    public enum EncodingConfidence
    {
        Irrelevant,
        Tentative,
        Certain
    }

    //public class HtmlStreamMetaData
    //{
    //    public string OriginatingUrl = String.Empty;
    //    public List<KeyValuePair<string, string>> HttpHeaders = new List<KeyValuePair<string, string>>();
    //}

    /// <summary>
    /// A fast & memory-efficient, HTML Tag Tokenizer.
    /// </summary>
    public class HtmlTextReader: IDisposable
    {
        private HtmlStream _htmlStream;
        private TextReader _reader;
        private ParseState _parseState;
        private StringBuilder _currTok;
        private int _peekChar = 0;
        private EncodingConfidence _currentEncodingConfidence;
        private EncodingConfidence _initialEncodingConfidence;
        private Encoding _initialEncoding;
        private string _originatingUrl = string.Empty;
        public List<KeyValuePair<string, string>> _originatingHttpHeaders = new List<KeyValuePair<string, string>>();

        public HtmlTextReader(string html)
        {
            Init(new StringReader(html));
        }

        public HtmlTextReader(TextReader reader)
        {
            Init(reader);
        }

        private void Init(TextReader reader)
        {
            _initialEncoding = (_reader is StreamReader) ? ((StreamReader)_reader).CurrentEncoding : null;
            _currentEncodingConfidence = reader is StringReader ? EncodingConfidence.Irrelevant : EncodingConfidence.Certain;
            _initialEncodingConfidence = _currentEncodingConfidence;
            _htmlStream = null;
            _reader = reader;
            _currTok = new StringBuilder();
            _parseState = ParseState.Text;
            _peekChar = _reader.Read();
        }

        public HtmlTextReader(Stream stream, Encoding encoding, EncodingConfidence encodingConfidence)
        {
            _initialEncoding = encoding;
            _initialEncodingConfidence = encodingConfidence;

            Init(stream, encoding, encodingConfidence);
        }

        private void Init(Stream stream, Encoding encoding, EncodingConfidence encodingConfidence)
        {
            _currentEncodingConfidence = encodingConfidence;
            _htmlStream = stream is HtmlStream ? (HtmlStream)stream : null;
            _reader = new StreamReader(stream, encoding, encodingConfidence == EncodingConfidence.Tentative);
            _currTok = new StringBuilder();
            _parseState = ParseState.Text;
            _peekChar = _reader.Read();
        }

        public TextReader BaseReader
        {
            get { return _reader; }
        }

        public string OriginatingUrl
        {
            get { return _originatingUrl; }
            set { _originatingUrl = value; }
        }

        public List<KeyValuePair<string, string>> OriginatingHttpHeaders
        {
            get { return _originatingHttpHeaders; }
        }

        public bool CanRewind
        {
            get { return (_htmlStream != null) && _htmlStream.CanRewind; }
        }

        public void Rewind(Encoding newEncoding)
        {
            if (!CanRewind)
                throw new Exception("Internal Error: Cannot rewind this Stream.");

            // Rewind our underlying stream, back to the beginning
            _htmlStream.Rewind();

            // Re-initialize ourselves with a new TextReader and the new Encoding...
            Init(_htmlStream, newEncoding, EncodingConfidence.Certain);
        }

        /// <summary>
        /// Returns the encoding the reader was initialized with. The
        /// CurrentEncoding may change during the course of reading from the 
        /// underlying stream - such as when Rewind() is called with a different
        /// encoding.
        /// </summary>
        public Encoding InitialEncoding
        {
            get { return _initialEncoding; }
        }

        /// <summary>
        /// Returns the EncodingConfidence that the HtmlTextReader was 
        /// initialzed with. If an encoding was supplied by an out-of-band 
        /// source (such as an HttpHeader, or a hard-coded user-supplied
        /// value) then the confidence will be Certain. If the HtmlTextReader
        /// was initialized by a String, the EncodingConfidence is 
        /// Irrelevant, since this is a stream of Unicode characters.
        /// </summary>
        public EncodingConfidence InitialEncodingConfidence
        {
            get { return _initialEncodingConfidence; }
        }

        /// <summary>
        /// Returns the current encoding being used on the underlying stream
        /// </summary>
        public Encoding CurrentEncoding
        {
            get { return (_reader is StreamReader) ? ((StreamReader)_reader).CurrentEncoding : null; }
        }

        /// <summary>
        /// Returns the current encoding confidence for the underlying stream
        /// </summary>
        public EncodingConfidence CurrentEncodingConfidence
        {
            get {

                // Here is a special case: we initialized the stream with a Tentative
                // Encoding, and the underlying StringReader detected the Encoding from the
                // Byte order mark... In this case - we will say that the current encoding
                // is Certain - (i.e. we will ignore any future <meta> charset) data.
                if (_currentEncodingConfidence == EncodingConfidence.Tentative &&
                    !object.Equals(_initialEncoding, CurrentEncoding))
                {
                    return EncodingConfidence.Certain;
                }                    

                return _currentEncodingConfidence;
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
                if (char.IsWhiteSpace((char)c)) {
                    _parseState = ParseState.AttributeName;
                    break;
                }

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
