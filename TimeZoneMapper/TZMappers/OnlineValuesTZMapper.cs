﻿namespace TimeZoneMapper.TZMappers
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Cache;

    /// <summary>
    /// Provides TimeZoneID mapping based on a current (&quot;dynamic&quot;) resource.
    /// </summary>
    public sealed class OnlineValuesTZMapper : BaseTZMapper, TimeZoneMapper.TZMappers.ITZMapper
    {
        /// <summary>
        /// Default URL used for <see cref="OnlineValuesTZMapper"/>
        /// </summary>
        public const string DEFAULTRESOURCEURL = "http://unicode.org/repos/cldr/trunk/common/supplemental/windowsZones.xml";

        /// <summary>
        /// Initializes a new instance of an <see cref="OnlineValuesTZMapper"/> with default timeout of 5 seconds and 
        /// <see cref="DEFAULTRESOURCEURL"/> as resourceURL.
        /// </summary>
        /// <remarks>
        /// By default, the data retrieved is cached for 24 hours in the user's temporary folder retrieved from
        /// <see cref="Path.GetTempPath"/>.
        /// </remarks>
        public OnlineValuesTZMapper()
            : this(5000) { }

        /// <summary>
        /// Initializes a new instance of an <see cref="OnlineValuesTZMapper"/> with the specified timeout and 
        /// <see cref="DEFAULTRESOURCEURL"/> as resourceURL.
        /// </summary>
        /// <param name="timeout">The length of time, in milliseconds, before the request times out.</param>
        /// <remarks>
        /// By default, the data retrieved is cached for 24 hours in the user's temporary folder retrieved from
        /// <see cref="Path.GetTempPath"/>.
        /// </remarks>
        public OnlineValuesTZMapper(int timeout)
            : this(timeout, DEFAULTRESOURCEURL) { }

        /// <summary>
        /// Initializes a new instance of an <see cref="OnlineValuesTZMapper"/> with the specified timeout and 
        /// resourceURL.
        /// </summary>
        /// <param name="timeout">The length of time, in milliseconds, before the request times out.</param>
        /// <param name="resourceurl">The URL to use when retrieving CLDR data.</param>
        /// <remarks>
        /// By default, the data retrieved is cached for 24 hours in the user's temporary folder retrieved from
        /// <see cref="Path.GetTempPath"/>.
        /// </remarks>
        public OnlineValuesTZMapper(int timeout, string resourceurl)
            : this(timeout, new Uri(resourceurl, UriKind.Absolute)) { }

        /// <summary>
        /// Initializes a new instance of an <see cref="OnlineValuesTZMapper"/> with the specified timeout and 
        /// resourceURI.
        /// </summary>
        /// <param name="timeout">The length of time, in milliseconds, before the request times out.</param>
        /// <param name="resourceuri">The URI to use when retrieving CLDR data.</param>
        /// <remarks>
        /// By default, the data retrieved is cached for 24 hours in the user's temporary folder retrieved from
        /// <see cref="Path.GetTempPath"/>.
        /// </remarks>
        public OnlineValuesTZMapper(int timeout, Uri resourceuri)
            : this(timeout, resourceuri, TimeSpan.FromHours(24)) { }

        /// <summary>
        /// Initializes a new instance of an <see cref="OnlineValuesTZMapper"/> with the specified timeout and 
        /// resourceURI.
        /// </summary>
        /// <param name="timeout">The length of time, in milliseconds, before the request times out.</param>
        /// <param name="resourceuri">The URI to use when retrieving CLDR data.</param>
        /// <param name="cachettl">
        /// Expiry time for downloaded data; unless this TTL has expired a cached version will be used.
        /// </param>
        /// <remarks>
        /// The default cache directory used is retrieved from <see cref="Path.GetTempPath"/>.
        /// </remarks>
        public OnlineValuesTZMapper(int timeout, Uri resourceuri, TimeSpan cachettl)
            : this(timeout, resourceuri, cachettl, Path.GetTempPath()) { }

        /// <summary>
        /// Initializes a new instance of an <see cref="OnlineValuesTZMapper"/> with the specified timeout and 
        /// resourceURI.
        /// </summary>
        /// <param name="timeout">The length of time, in milliseconds, before the request times out.</param>
        /// <param name="resourceuri">The URI to use when retrieving CLDR data.</param>
        /// <param name="cachettl">
        /// Expiry time for downloaded data; unless this TTL has expired a cached version will be used.
        /// </param>
        /// <param name="cachedirectory">The directory to use to store a cached version of the data.</param>
        public OnlineValuesTZMapper(int timeout, Uri resourceuri, TimeSpan cachettl, string cachedirectory)
            : base(new TimedWebClient(timeout, cachettl, cachedirectory).RetrieveCachedString(resourceuri)) { }

        /// <summary>
        /// Simple "wrapper class" providing timeouts.
        /// </summary>
        private class TimedWebClient : WebClient
        {
            public int Timeout { get; set; }

            public TimeSpan DefaultTTL { get; set; }

            public string CacheDirectory { get; set; }

            public TimedWebClient(int timeout, TimeSpan ttl, string cachedirectory)
            {
                this.Timeout = timeout;
                this.DefaultTTL = ttl;
                this.CacheDirectory = cachedirectory;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var wr = base.GetWebRequest(address);
                wr.Timeout = this.Timeout;
                return wr;
            }

            public string RetrieveCachedString(Uri uri)
            {
                var dest = Path.Combine(this.CacheDirectory, Path.GetFileName(uri.AbsolutePath));
                if (IsFileExpired(dest, this.DefaultTTL))
                {
                    base.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    base.DownloadFile(uri, dest);
                }

                using (var f = File.OpenRead(dest))
                using (var fr = new StreamReader(f))
                {
                    return fr.ReadToEnd();
                }
            }

            private static bool IsFileExpired(string path, TimeSpan ttl)
            {
                return (!File.Exists(path) || (DateTime.UtcNow - new FileInfo(path).CreationTimeUtc) > ttl);
            }
        }
    }
}