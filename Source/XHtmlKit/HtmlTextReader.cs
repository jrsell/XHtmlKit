using System;
using System.Text;
using System.IO;

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

    /// <summary>
    /// A simple HTML Tag Tokenizer
    /// </summary>
    public class HtmlTextReader
    {
        private TextReader _reader;
        private ParseState _parseState;
        private StringBuilder _currTok;
        private int _peekChar = 0;

        public HtmlTextReader(TextReader reader)
        {
            _parseState = ParseState.Text;
            _reader = reader;
            _currTok = new StringBuilder();
            _peekChar = _reader.Read(); 
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
                if (c == needle[len - 1])
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
                _currTok.Append((char)c);
            }

            // Return the tag in lower-case
            return _currTok.ToString().ToLowerInvariant();
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

    }
}
