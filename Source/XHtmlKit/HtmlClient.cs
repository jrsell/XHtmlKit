#if !net20 

using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;


namespace XHtmlKit.Network
{
    public class HtmlClientOptions
    {
        public bool DetectEncoding = true;
        public Encoding DefaultEncoding = new UTF8Encoding();   // New the encoding so that it is different from the StreamReader's 
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
        public static async Task<HtmlTextReader> GetHtmlTextReaderAsync(string url, HtmlClientOptions options)
        {
            HtmlClientOptions optionsToUse = options == null ? HtmlClient.Options : options;
            Uri uri = new Uri(url);

            // See if the url pointed to a file. If so, return a reader with an underlying file stream.
            if (uri.IsFile) {
                FileStream fs = File.OpenRead(uri.AbsolutePath);
                HtmlStream stream = new HtmlStream(fs);
                HtmlTextReader reader = new HtmlTextReader(stream, options.DefaultEncoding, EncodingConfidence.Tentative);
                reader.OriginatingUrl = url;
                return reader;
            }

            // Set user agent if one was specified
            if (!string.IsNullOrEmpty(optionsToUse.UserAgent)) {
                HttpClient.DefaultRequestHeaders.Remove("User-Agent");
                HttpClient.DefaultRequestHeaders.Add("User-Agent", optionsToUse.UserAgent);
            }

            // Get the Http response
            HttpResponseMessage responseMessage = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            // Ensure success
            responseMessage.EnsureSuccessStatusCode();

            // Check to ensure there is content to return. 
            HttpContent content = responseMessage.Content;
            if (content == null)
            {
                HtmlTextReader reader = new HtmlTextReader(String.Empty);
                reader.OriginatingUrl = url;
                foreach (var header in content.Headers) {
                    reader.OriginatingHttpHeaders.Add(new KeyValuePair<string, string>(header.Key, string.Join(";", header.Value)));
                }
                return reader;
            }
            
            // Try to get the stream's encoding from the Response Headers, or fall back on default. 
            // We will also try to detect the encoding from the Byte Order Mark if there is no encoding supplied
            // by the headers. If both of these fail, the Parser should look for an encoding in the <meta> tags of 
            // the html itself.
            Encoding encoding = optionsToUse.DefaultEncoding;

            // Try to detect the encoding from Http Headers
            bool gotEncodingFromHttpHeaders = false;
            if (optionsToUse.DetectEncoding)
            {
                var contentHeaders = content.Headers;
                string charset = (contentHeaders.ContentType != null) ? contentHeaders.ContentType.CharSet : null;
                encoding = EncodingUtils.GetEncoding(charset);
                gotEncodingFromHttpHeaders = encoding != null;
                encoding = (encoding == null ? optionsToUse.DefaultEncoding : encoding);
                System.Diagnostics.Debug.WriteLine("Detected encoding: charset: " + charset + ", got encoding from headers: " + gotEncodingFromHttpHeaders);
            }

            // Out of band encoding can be either passed in by clients, or found in the http headers...
            bool gotEncodingFromOutOfBandSource = !optionsToUse.DetectEncoding || gotEncodingFromHttpHeaders;
            EncodingConfidence encodingConfidence = gotEncodingFromOutOfBandSource ? EncodingConfidence.Certain : EncodingConfidence.Tentative;

            // If encoding was NOT supplied out of band, then we will try to detect it from the stream's BOM
            bool tryToDetectEncodingFromByteOrderMark = (encodingConfidence == EncodingConfidence.Tentative);

            // Get the stream from the network
            Stream networkStream = await content.ReadAsStreamAsync().ConfigureAwait(false);

            // If we are still tentative about the encoding, pop the stream into a wrapper that let's us re-wind.
            Stream baseStream = (encodingConfidence == EncodingConfidence.Tentative) ? new HtmlStream(networkStream) : networkStream;

            // Return a HtmlTextReader with the encoding as detected so far... 
            HtmlTextReader htmlReader = new HtmlTextReader(baseStream, encoding, encodingConfidence);

            // Store some metadata about how the stream came to be...
            htmlReader.OriginatingUrl = url;
            foreach (var header in content.Headers)
            {
                htmlReader.OriginatingHttpHeaders.Add(new KeyValuePair<string, string>(header.Key, string.Join(";", header.Value)));
            }

            return htmlReader;
        }

    }

    
}
#endif