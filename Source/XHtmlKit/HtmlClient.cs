#if !net20 

using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;


namespace XHtmlKit.Network
{
    /// <summary>
    /// Fetches an Html stream from the web
    /// </summary>
    internal static class HtmlClient
    {
        private static HttpClient _httpClient = new HttpClient();
        public static HttpClient HttpClient
        {
            get { return _httpClient; }
            set { _httpClient = value; }
        }

        private static ClientOptions _options= new ClientOptions();
        public static ClientOptions Options
        {
            get { return _options; }
        }

        public static async Task<HtmlTextReader> GetHtmlTextReaderAsync(this HttpContent content, Encoding defaultEncoding, bool detectEncoding)
        {
            // Try to get the stream's encoding from the Response Headers, or fall back on default. 
            // We will also try to detect the encoding from the Byte Order Mark if there is no encoding supplied
            // by the headers. If both of these fail, the Parser should look for an encoding in the <meta> tags of 
            // the html itself.
            Encoding encoding = defaultEncoding;

            // Try to detect the encoding from Http Headers
            bool gotEncodingFromHttpHeaders = false;
            if (detectEncoding)
            {
                var contentHeaders = content.Headers;
                string charset = (contentHeaders.ContentType != null) ? contentHeaders.ContentType.CharSet : null;
                encoding = EncodingUtils.GetEncoding(charset);
                gotEncodingFromHttpHeaders = encoding != null;
                encoding = (encoding == null ? defaultEncoding : encoding);
                System.Diagnostics.Debug.WriteLine("Detected encoding: charset: " + charset + ", got encoding from headers: " + gotEncodingFromHttpHeaders);
            }

            // Out of band encoding can be either passed in by clients, or found in the http headers...
            bool gotEncodingFromOutOfBandSource = !detectEncoding|| gotEncodingFromHttpHeaders;
            EncodingConfidence encodingConfidence = gotEncodingFromOutOfBandSource ? EncodingConfidence.Certain : EncodingConfidence.Tentative;

            // If encoding was NOT supplied out of band, then we will try to detect it from the stream's BOM
            bool tryToDetectEncodingFromByteOrderMark = (encodingConfidence == EncodingConfidence.Tentative);

            // Get the stream from the network
            Stream networkStream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            // If we are still tentative about the encoding, pop the stream into a wrapper that let's us re-wind.
            Stream baseStream = (encodingConfidence == EncodingConfidence.Tentative) ? new HtmlStream(networkStream) : networkStream;

            // Return a HtmlTextReader with the encoding as detected so far... 
            HtmlTextReader htmlReader = new HtmlTextReader(baseStream, encoding, encodingConfidence);

            return htmlReader;
        }

        public static async Task<HtmlTextReader> GetHtmlTextReaderAsync(string url)
        {
            return await GetHtmlTextReaderAsync(url, HtmlClient.Options);
        }

        /// <summary>
        /// Returns a TextReader that detects the underlying stream's endoding. Allows clients to stream the 
        /// retured content using a TextReader. This method is similar in purpose to GetStreamAsync, however, GetStreamAsync
        /// doesn't detect the Stream's encoding as GetStringAsync does. 
        /// </summary>
        /// <param name="httpClient"></param>
        public static async Task<HtmlTextReader> GetHtmlTextReaderAsync(string url, ClientOptions options)
        {
            HtmlTextReader reader;
            ClientOptions optionsToUse = options == null ? HtmlClient.Options : options;
            Uri uri = new Uri(url);

            // See if the url pointed to a file. If so, return a reader with a file stream
            // under the hood.
            if (uri.IsFile) {
                FileStream fs = File.OpenRead(uri.AbsolutePath);
                HtmlStream stream = new HtmlStream(fs);
                reader = new HtmlTextReader(stream, options.DefaultEncoding, EncodingConfidence.Tentative);
                reader.OriginatingUrl = url;
                return reader;
            }

            // Set a user agent if one was specified
            if (!string.IsNullOrEmpty(optionsToUse.UserAgent)) {
                HttpClient.DefaultRequestHeaders.Remove("User-Agent");
                HttpClient.DefaultRequestHeaders.Add("User-Agent", optionsToUse.UserAgent);
            }

            // Get the Http response (only read the headers at this point) and ensure succes
            HttpResponseMessage responseMessage = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();

            // If there is no content to return, return an empty HtmlTextReader 
            HttpContent content = responseMessage.Content;
            if (content == null) {
                reader = new HtmlTextReader(String.Empty);
            } else {
                reader = await content.GetHtmlTextReaderAsync(optionsToUse.DefaultEncoding, optionsToUse.DetectEncoding);
            }

            // Store some metadata on the reader. Could be used by a parser. 
            reader.OriginatingUrl = url;
            foreach (var header in content.Headers) {
                reader.OriginatingHttpHeaders.Add(new KeyValuePair<string, string>(header.Key, string.Join(";", header.Value)));
            }

            return reader;
        }

    }

    
}
#endif