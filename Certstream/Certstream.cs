using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Certstream
{
    /// <summary>
    /// The primary class for connecting to the Certstream server. 
    /// </summary>
    public class CertstreamClient
    {
        /// <summary>
        /// How often should the WebSocket connection be pinged, in <b>milliseconds.</b>
        /// </summary>
        private readonly int PingInterval;
        /// <summary>
        /// The delay between reconnecting a closed WebSocket connection, in <b>milliseconds.</b>
        /// </summary>
        private readonly int ReconnectionDelay;
        /// <summary>
        /// The maximum limit of consecutive reconnection fails until an exception is thrown.
        /// </summary>
        private readonly int MaxRetries;
        /// <summary>
        /// The hostname to connect to.
        /// </summary>
        public readonly string Hostname;
        /// <summary>
        /// The current amount of consecutive reconnection fails.
        /// </summary>
        private int Retries = 0;
        /// <summary>
        /// Maps connection types to connection URIs.
        /// </summary>
        private readonly Dictionary<ConnectionType, Uri> ConnectionUriMap = new();
        /// <summary>
        /// The connection type to use in this instance.
        /// </summary>
        private readonly ConnectionType ConnectionType;

        private ClientWebSocket WebSocket;
        private CancellationTokenSource Source;
        private Timer Pinger;
        private bool Running;
        
        private readonly ArraySegment<byte> PingBytes = new(Encoding.UTF8.GetBytes("ping"));

        private EventHandler<LeafCertificate> FullHandler;
        /// <summary>
        /// Fired whenever a new SSL certificate is issued and received from the WebSocket connection.
        /// <para>Requires <see cref="ConnectionType.Full"/> to be used.</para>
        /// </summary>
        public event EventHandler<LeafCertificate> CertificateIssued
        {
            add
            {
                if (ConnectionType != ConnectionType.Full) throw new("Only available in connection type: Full.");

                FullHandler += value;
                if (FullHandler.GetInvocationList().Length == 1) Start();
            }
            remove
            {
                FullHandler -= value;
                if (FullHandler is null || FullHandler.GetInvocationList().Length == 0) Stop();
            }
        }

        private EventHandler<string> DomainsOnlyHandler;
        /// <summary>
        /// Fired whenever a new hostname is spotted and received from the WebSocket connection.
        /// <para>Requires <see cref="ConnectionType.DomainsOnly"/> to be used.</para>
        /// </summary>
        public event EventHandler<string> HostnameSpotted
        {
            add
            {
                if (ConnectionType != ConnectionType.DomainsOnly) throw new("Only available in connection type: Domains-Only.");

                DomainsOnlyHandler += value;
                if (DomainsOnlyHandler.GetInvocationList().Length == 1) Start();
            }
            remove
            {
                DomainsOnlyHandler -= value;
                if (DomainsOnlyHandler is null || DomainsOnlyHandler.GetInvocationList().Length == 0) Stop();
            }
        }

        /// <summary>
        /// Create a new instance of the Certstream client.
        /// </summary>
        /// <param name="hostname">The Certstream server hostname to connect to. Use this if you want to connect to your own instance.</param>
        /// <param name="maxRetries">The maximum limit of consecutive reconnection fails until an exception is thrown.<para>Set to a negative value for no limit.</para></param>
        /// <param name="pingInterval">The interval in milliseconds when a ping message is sent into the WebSocket conversation.</param>
        /// <param name="reconnectionDelay">The delay to wait before attempting to reconnect.</param>
        /// <param name="connectionType">The type of connection to use in this instance.</param>
        /// <exception cref="ArgumentException"></exception>
        public CertstreamClient(ConnectionType connectionType = Constants.ConnectionType, string hostname = Constants.Hostname, int maxRetries = Constants.MaxRetries, int pingInterval = Constants.PingInterval, int reconnectionDelay = Constants.ReconnectionDelay)
        {
            ConnectionType = connectionType;
            Hostname = hostname;
            MaxRetries = maxRetries;
            PingInterval = pingInterval;
            ReconnectionDelay = reconnectionDelay;

            bool success1 = Uri.TryCreate($"wss://{hostname}/", UriKind.Absolute, out Uri uri1);
            bool success2 = Uri.TryCreate($"wss://{hostname}/domains-only", UriKind.Absolute, out Uri uri2);
            if (!success1 || !success2) throw new ArgumentException("Invalid hostname provided.", nameof(hostname));

            ConnectionUriMap.Add(ConnectionType.Full, uri1);
            ConnectionUriMap.Add(ConnectionType.DomainsOnly, uri2);
        }

        /// <summary>
        /// Forcibly start collecting certificates.
        /// </summary>
        public async void Start()
        {
            Running = true;
            Connect();

            Pinger = new()
            {
                Interval = PingInterval
            };

            Pinger.Elapsed += async (o, e) => await Ping();
            Pinger.Start();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Forcibly stop collecting certificates.
        /// </summary>
        public async void Stop()
        {
            Running = false;
            Pinger.Stop();

            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User demands a WebSocket closure.", Source.Token);
        }

        /// <summary>
        /// The main method that connects to the WebSocket and listens for new messages.
        /// </summary>
        public async void Connect()
        {
            if (!Running) return;

            Retries++;
            Source = new();
            WebSocket = new();
            WebSocket.Options.SetRequestHeader("User-Agent", Constants.UserAgent);

            try
            {
                Uri uri = ConnectionUriMap[ConnectionType];

                await WebSocket.ConnectAsync(uri, Source.Token);
                Retries = 0;

                Debug.WriteLine($"WebSocket connected to {uri}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect to WebSocket: {ex.Message}");

                if (MaxRetries >= 0 && Retries >= MaxRetries) throw new($"Failed to connect to Certstream {Retries} times.");

                await Task.Delay(ReconnectionDelay);
                Connect();
                return;
            }

            while (WebSocket.State == WebSocketState.Open)
            {
                var receiveBuffer = new byte[16384];

                var offset = 0;
                while (true)
                {
                    try
                    {
                        ArraySegment<byte> bytesReceived = new(receiveBuffer, offset, receiveBuffer.Length);

                        WebSocketReceiveResult result = await WebSocket.ReceiveAsync(bytesReceived, Source.Token);
                        offset += result.Count;

                        if (result.EndOfMessage) break;
                    }
                    catch { break; };
                }

                if (offset != 0) OnMessage(this, Encoding.UTF8.GetString(receiveBuffer, 0, offset));
            }

            Debug.WriteLine($"Connection lost: {WebSocket.CloseStatus}{(WebSocket.CloseStatusDescription is null ? "" : $" => {WebSocket.CloseStatusDescription}")}");
            await Task.Delay(ReconnectionDelay);

            Connect();
        }

        /// <summary>
        /// The method that sends a <c>ping</c> message into the WebSocket to prevent it from closing.
        /// </summary>
        /// <returns></returns>
        public async Task Ping()
        {
            if (WebSocket.State != WebSocketState.Open) return;

            try
            {
                await WebSocket.SendAsync(PingBytes, WebSocketMessageType.Text, true, Source.Token);
                Debug.WriteLine("Pinged WebSocket connection.");
            }
            catch { };
        }

        /// <summary>
        /// Called whenever a WebSocket message is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        public void OnMessage(object sender, string msg)
        {
            if (ConnectionType == ConnectionType.Full)
            {
                CertificateMessage certMessage;

                try
                {
                    certMessage = JsonSerializer.Deserialize<CertificateMessage>(msg);
                }
                catch { return; };

                if (certMessage.MessageType != "certificate_update") return;

                FullHandler.Invoke(sender, certMessage.Data.Leaf);
            }
            else if (ConnectionType == ConnectionType.DomainsOnly)
            {
                DomainsOnlyMessage domainsOnlyMessage;

                try
                {
                    domainsOnlyMessage = JsonSerializer.Deserialize<DomainsOnlyMessage>(msg);
                }
                catch { return; };

                foreach (string hostname in domainsOnlyMessage.Hostnames)
                    DomainsOnlyHandler.Invoke(sender, hostname);
            }
            else throw new NotImplementedException();
        }
    }
}