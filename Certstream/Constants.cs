using System.Text;
using System;
using Certstream.Models;

namespace Certstream
{
    internal class Constants
    {
        /// <summary>
        /// The <c>User-Agent</c> header value to send when connecting.
        /// </summary>
        public const string UserAgent = "C# Certstream Client - https://github.com/akacdev/Certstream";

        /// <summary>
        /// The buffer size for holding incoming messages. - <b>Default: 16 KiB</b>
        /// </summary>
        public const int BufferSize = 16384;

        /// <summary>
        /// An array used to hold ping bytes that are sent periodically to the server to keep the connection alive.
        /// </summary>
        public static readonly ArraySegment<byte> PingBytes = new(Encoding.UTF8.GetBytes("ping"));

        /// <summary>
        /// This event is used when a new certificate is discovered or updated.
        /// </summary>
        public const string TargetMessageType = "certificate_update";

        /// <summary>
        /// The default connection type to use.
        /// </summary>
        public const ConnectionType ConnectionType = 0;

        /// <summary>
        /// The default hostname to connect to.
        /// </summary>
        public const string Hostname = "certstream.calidog.io";

        /// <summary>
        /// The maximum limit of consecutive reconnection fails until an exception is thrown.
        /// </summary>
        public const int MaxRetries = 10;

        /// <summary>
        /// How often should the WebSocket connection be pinged, in <b>milliseconds.</b>
        /// </summary>
        public const int PingInterval = 5000;

        /// <summary>
        /// The delay between reconnecting a closed WebSocket connection, in <b>milliseconds.</b>
        /// </summary>
        public const int ReconnectionDelay = 1000;
    }
}