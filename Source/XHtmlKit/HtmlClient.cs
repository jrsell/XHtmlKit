#if !net20 

using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;


namespace XHtmlKit
{
    /// <summary>
    /// Fetches an Html stream from the web
    /// </summary>
    public static class HtmlClient
    {

        /// <summary>
        /// Returns a TextReader that detects the underlying stream's endoding. Allows clients to stream the 
        /// retured content using a TextReader. This method is similar in purpose to GetStreamAsync, however, GetStreamAsync
        /// doesn't detect the Stream's encoding as GetStringAsync does. 
        /// </summary>
        /// <param name="httpClient"></param>
        public static async Task<HtmlTextReader> GetTextReaderAsync(this HttpClient httpClient, string url)
        {
            // Get the Http response
            HttpResponseMessage responseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            // Ensure success
            responseMessage.EnsureSuccessStatusCode();

            // Check to ensure there is content to return. 
            HttpContent content = responseMessage.Content;
            if (content == null)
            {
                return new HtmlTextReader(new StringReader(String.Empty));
            }

            // Try to get the stream's encoding from the Response Headers
            var contentHeaders = content.Headers;
            string charset = (contentHeaders.ContentType != null) ? contentHeaders.ContentType.CharSet : null;
            Encoding encoding = EncodingUtils.GetEncoding(charset);

            // Return the stream as an HtmlTextReader
            Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false);            

            return new HtmlTextReader(stream, encoding);
        }

    }
}
#endif