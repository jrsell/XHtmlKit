using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;


namespace XHtmlKit.Network
{
    /// <summary>
    /// Extensions for the Dotnet HtmlClient class. 
    /// </summary>
    public static class HtmlClientExtensions
    {

        /// <summary>
        /// Returns a TextReader that detects the underlying stream's endoding. Allows clients to stream the 
        /// retured content using a TextReader. This method is similar in purpose to GetStreamAsync, however, GetStreamAsync
        /// doesn't detect the Stream's encoding as GetStringAsync does. 
        /// </summary>
        /// <param name="httpClient"></param>
        public static async Task<TextReader> GetTextReaderAsync(this HttpClient httpClient, string url)
        {
            // Get the Http response
            HttpResponseMessage responseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            // Ensure success
            responseMessage.EnsureSuccessStatusCode();

            // Check to ensure there is content to return. 
            HttpContent content = responseMessage.Content;
            if (content == null)
            {
                return new StringReader(String.Empty);
            }

            // Try to get the stream's encoding from the Response Headers, default is UTF8 
            // We will try to detect the encoding from the Byte Order Mark if there is no encoding supplied
            var contentHeaders = content.Headers;
            string charset = (contentHeaders.ContentType != null) ? contentHeaders.ContentType.CharSet : null;
            Encoding encoding = (charset != null) ? Encoding.GetEncoding(charset) : Encoding.UTF8; 
            bool detectEncoding = (charset == null) ?  true: false;

            // Return the decoded stream as a TextReader
            Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            StreamReader reader = new StreamReader(stream, encoding, detectEncoding);

            return reader;
        }

    }
}
