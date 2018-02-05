#if !net20 

using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;


namespace XHtmlKit
{
    public class HtmlClientOptions
    {
        public bool DetectEncoding = true;
        public Encoding DefaultEncoding = Encoding.UTF8;
        public string UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";
    }


    /// <summary>
    /// Fetches an Html stream from the web
    /// </summary>
    public static class HtmlClient
    {
        private static HttpClient _httpClient = new HttpClient();
        public static HttpClient HttpClient
        {
            get { return _httpClient; }
            set { _httpClient = value; }
        }

        private static HtmlClientOptions _options= new HtmlClientOptions();
        public static HtmlClientOptions Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Returns a TextReader that detects the underlying stream's endoding. Allows clients to stream the 
        /// retured content using a TextReader. This method is similar in purpose to GetStreamAsync, however, GetStreamAsync
        /// doesn't detect the Stream's encoding as GetStringAsync does. 
        /// </summary>
        /// <param name="httpClient"></param>
        public static async Task<TextReader> GetTextReaderAsync(string url)
        {
#if netstandard
            // Ug... not all encodings are available with NetStandard by default 
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif

            HtmlClientOptions options = Options;

            HttpClient.DefaultRequestHeaders.Remove("User-Agent");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);

            // Get the Http response
            HttpResponseMessage responseMessage = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            // Ensure success
            responseMessage.EnsureSuccessStatusCode();

            // Check to ensure there is content to return. 
            HttpContent content = responseMessage.Content;
            if (content == null)
            {
                return new StringReader(String.Empty);
            }

            // Try to get the stream's encoding from the Response Headers, default is UTF8 
            // We will also try to detect the encoding from the Byte Order Mark if there is no encoding supplied
            // by the headers. Note - this should be augmented at some point, by the 
            // ability to detect the encoding from the <meta> tag in the document itself.
            Encoding encoding = options.DefaultEncoding;
            bool detectEncodingFromBOM = true;
            if (options.DetectEncoding)
            {
                var contentHeaders = content.Headers;
                string charset = (contentHeaders.ContentType != null) ? contentHeaders.ContentType.CharSet : null;
                encoding = (charset != null) ? Encoding.GetEncoding(charset) : options.DefaultEncoding;
                detectEncodingFromBOM = (charset == null) ? true : false;
                System.Diagnostics.Debug.WriteLine("Detected encoding: charset: " + charset + ", detect from BOM: " + detectEncodingFromBOM);
            }

            // Return the decoded stream as a TextReader
            Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            StreamReader reader = new StreamReader(stream, encoding, detectEncodingFromBOM);
            return reader;
        }

    }
}
#endif