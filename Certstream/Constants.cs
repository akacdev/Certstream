namespace Certstream
{
    internal class Constants
    {
        /// <summary>
        /// How often should the WebSocket connection be pinged, in <b>milliseconds.</b>
        /// </summary>
        public const int PingInterval = 5000;
        /// <summary>
        /// The delay between reconnecting a closed WebSocket connection, in <b>milliseconds.</b>
        /// </summary>
        public const int ReconnectionDelay = 1000;
        /// <summary>
        /// The maximum limit of consecutive reconnection fails until an exception is thrown.
        /// </summary>
        public const int MaxRetries = 10;
        /// <summary>
        /// The hostname to connect to.
        /// </summary>
        public const string Hostname = "certstream.calidog.io";
        /// <summary>
        /// The <c>User-Agent</c> header value to send when connecting.
        /// </summary>
        public const string UserAgent = "C# Certstream Client - https://github.com/actually-akac/Certstream";
        /// <summary>
        /// The connection type to use.
        /// </summary>
        public const ConnectionType ConnectionType = 0;
    }
}