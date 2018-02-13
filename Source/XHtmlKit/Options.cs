using System.Text;

namespace XHtmlKit
{

#if !net20
    public class ClientOptions
    {
        public bool DetectEncoding = true;
        public Encoding DefaultEncoding = new UTF8Encoding();   // New the encoding so that it is different from the StreamReader's 
        public string UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";
    }

    public class LoaderOptions: ClientOptions
    {
        private ParserOptions _parserOptions = new ParserOptions();
        public ParserOptions ParserOptions { get { return _parserOptions; } }
    }
#endif 

    public class ParserOptions
    {
        /// <summary>
        /// If a BaseUrl is supplied, and FullyQualifyUrls is set to 'True'
        /// the parser will use the BaseUrl to fully Qualify Urls encountered in 
        /// a 'href' and  img 'src' attributes. 
        /// </summary>
        public string BaseUrl = null;

        /// <summary>
        /// Used in conjunction with 'BaseUrl' to instruct the parser to 
        /// Fully qualify Urls encountered in a 'href' and  img 'src' attributes. 
        /// </summary>
        public bool FullyQualifyUrls = true;

        /// <summary>
        /// When 'IncludeMetaData' is set to true, all of the metadata
        /// available to the html parser is inserted into the head tag
        /// of the returned XHtml document as additional meta tags. The metadata 
        /// includes thing such as: the DetectedEncoding, the OriginatingUrl, 
        /// all of the HttpHeaders parsing time etc.
        /// </summary>
        public bool IncludeMetaData = false;
    }




}
