using System;
using System.IO;

namespace XHtmlKit
{
    /// <summary>
    /// A simple Stream wrapper that caches the first 1024 characters of the stream.
    /// This allows re-winding to the beginning, for cases where we wish to 
    /// re-parse the stream (i.e. we detect a meta charset tag)
    /// </summary>
    internal class HtmlStream : Stream
    {
        const int MaxCacheSize = 4096;
        const int MinCacheSize = 0;
        const int DefaultCacheSize = 1024;

        int _cacheSize; 
        bool _writeToCache; 
        bool _readFromCache; 
        bool _canRewind; 

        MemoryStream _cache;
        Stream _stream;

        public HtmlStream(Stream baseStream)
        {
            Init(baseStream, DefaultCacheSize);
        }

        public HtmlStream(Stream baseStream, int cacheSize)
        {
            Init(baseStream, cacheSize);
        }

        private void Init(Stream baseStream, int cacheSize)
        {
            // Ensure valid cache size
            if (cacheSize < MinCacheSize || cacheSize > MaxCacheSize) {
                throw new Exception("Invalid cache size. Must be between: " + MinCacheSize + " and " + MaxCacheSize);
            }

            // Ensure stream is not null
            if (baseStream == null) {
                throw new Exception("Base Stream cannot be null.");
            }

            _stream = baseStream;

            // If we have specified a cache 
            _cacheSize = cacheSize;
            _cache = (cacheSize > 0) ? new MemoryStream(_cacheSize) : null;
            _writeToCache = (cacheSize > 0);
            _readFromCache = false;
            _canRewind = false;
        }

        public override bool CanRead { get { return _stream.CanRead; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { throw new NotImplementedException(); } }
        public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public override void Flush()
        {
            _cache.Flush();
            _stream.Flush(); 
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesread = 0;
            if (_readFromCache) {

                bytesread = _cache.Read(buffer, offset, count);

                // If we have finished reading from the cache
                if (_cache.Position == _cache.Length ) {
                    _readFromCache = false;
                }
            }

            // Read from the stream
            if (bytesread < count) {
                bytesread += _stream.Read(buffer, offset + bytesread, count - bytesread);
                _canRewind = false; 
            }

            // Write to the cache
            if (_writeToCache) {
                _cache.Write(buffer, offset, bytesread);

                // We have data in the cache, we can now Rewind
                _canRewind = true; 

                // If we have filled the cache, stopped writing to it.
                if (_cache.Length >= _cacheSize) {
                    _writeToCache = false;
                }
            }

            // Return the total number of bytes read from either the cache or stream or both.     
            return bytesread;
        }

        public override bool CanSeek
        {
            get { return _canRewind; }
        }

        public bool CanRewind
        {
            get { return _canRewind; }
        }

        public void Rewind()
        {
            Seek(0, SeekOrigin.Begin);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // Ensure our cache is ready for seeking
            if (!_canRewind)
                throw new Exception("Internal Error. Cannot seek to origin.");

            // Ensure that user requested a seek to the beginning.
            if (!(origin == SeekOrigin.Begin && offset == 0))
                throw new Exception("Internal Error. Seeking on an HtmlStream can only be used to rewind to the beginning.");

            // Move the pointer in our cache to the beginning.
            _cache.Seek(0, SeekOrigin.Begin);
            _readFromCache = true;
            _writeToCache = false;

            // Return new position (0)
            return 0;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
