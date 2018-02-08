using System;
using System.Text;

namespace XHtmlKit
{
    internal static class EncodingUtils
    {
        public static string GetCharset(string charset, string httpEquiv, string content)
        {
            // HTML5: <meta charset="UTF-8">
            // We passed in a value for the charset - so look it up
            if (!string.IsNullOrEmpty(charset))
                return charset;

            // HTML 4.0.1: <meta http-equiv="content-type" content="text/html; charset=UTF-8">
            if (httpEquiv.ToLower().Trim() == "content-type" && !string.IsNullOrEmpty(content))
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, "charset\\s*=[\\s\"']*([^\\s\"' />]*)");
                if (match.Success)
                {
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

            // Look up the encoding in the list of Registered Providers
            Encoding retval = null;
            try
            {
                retval = Encoding.GetEncoding(charsetToFind);
            }
            catch
            {
                retval = null;
            }

            return retval;
        }

    }
}
