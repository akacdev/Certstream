using System;
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
    /// The primary class for connecting to a Certstream server. 
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
        /// The calculated connection Uri.
        /// </summary>
        private readonly Uri ConnectionUri;
        /// <summary>
        /// The connection type to use in this instance.
        /// </summary>
        private readonly ConnectionType ConnectionType;

        private ClientWebSocket WebSocket;
        private CancellationTokenSource CancellationSource;
        private Timer Pinger;
        private bool Running;

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
        /// <param name="connectionType">The type of connection to use in this instance.</param>
        /// <param name="hostname">The Certstream server hostname to connect to. Use this if you want to connect to your own instance.</param>
        /// <param name="maxRetries">The maximum limit of consecutive reconnection fails until an exception is thrown.<para>Set to a negative value for no limit.</para></param>
        /// <param name="pingInterval">The interval in milliseconds when a ping message is sent into the WebSocket conversation.</param>
        /// <param name="reconnectionDelay">The delay to wait before attempting to reconnect.</param>
        /// <exception cref="ArgumentException"></exception>
        public CertstreamClient(
            ConnectionType connectionType = Constants.ConnectionType,
            string hostname = Constants.Hostname,
            int maxRetries = Constants.MaxRetries,
            int pingInterval = Constants.PingInterval,
            int reconnectionDelay = Constants.ReconnectionDelay)
        {
            ConnectionType = connectionType;
            Hostname = hostname;
            MaxRetries = maxRetries;
            PingInterval = pingInterval;
            ReconnectionDelay = reconnectionDelay;

            string path = connectionType switch
            {
                ConnectionType.Full => "/",
                ConnectionType.DomainsOnly => "/domains-only",
                _ => throw new NotImplementedException()
            };

            string rawUrl = string.Concat("wss://", hostname, path);
            
            bool success = Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri uri);
            if (!success) throw new ArgumentException("Invalid hostname provided.", nameof(hostname));

            ConnectionUri = uri;
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

            Pinger.Elapsed += async (sender, args) => await Ping();
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

            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User demands a WebSocket closure.", CancellationSource.Token);
        }

        /// <summary>
        /// The main method that connects to the WebSocket and listens for new messages.
        /// </summary>
        public async void Connect()
        {
            if (!Running) return;

            Retries++;
            CancellationSource = new();
            WebSocket = new();
            WebSocket.Options.SetRequestHeader("User-Agent", Constants.UserAgent);

            try
            {
                await WebSocket.ConnectAsync(ConnectionUri, CancellationSource.Token);
                Retries = 0;

                Debug.WriteLine($"WebSocket connected to {ConnectionUri}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect to WebSocket: {ex.Message}");

                if (MaxRetries >= 0 && Retries >= MaxRetries) throw new CertstreamException($"Failed to connect to Certstream after {Retries} retries.");

                await Task.Delay(ReconnectionDelay);
                Connect();
                return;
            }

            while (WebSocket.State == WebSocketState.Open)
            {
                byte[] receiveBuffer = new byte[Constants.BufferSize];
                int offset = 0;

                while (true)
                {
                    try
                    {
                        int remainingBufferSpace = receiveBuffer.Length - offset;

                        if (remainingBufferSpace <= 0)
                        {
                            throw new InvalidOperationException("Buffer overflow: The receive buffer is full.");
                        }

                        ArraySegment<byte> bytesReceived = new(receiveBuffer, offset, remainingBufferSpace);

                        WebSocketReceiveResult result = await WebSocket.ReceiveAsync(bytesReceived, CancellationSource.Token);

                        offset += result.Count;

                        if (result.EndOfMessage) break;
                    }
                    catch { break; };
                }

                if (offset != 0) OnMessage(this, Encoding.UTF8.GetString(receiveBuffer, 0, offset));
            }

            Debug.WriteLine(string.Concat($"Connection lost: {WebSocket.CloseStatus}", WebSocket.CloseStatusDescription is null ? "" : $" => {WebSocket.CloseStatusDescription}"));
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
                await WebSocket.SendAsync(Constants.PingBytes, WebSocketMessageType.Text, true, CancellationSource.Token);
                Debug.WriteLine("Pinged WebSocket connection.");
            }
            catch { };
        }

        /// <summary>
        /// Called whenever a WebSocket message is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnMessage(object sender, string message)
        {
            switch (ConnectionType)
            {
                case ConnectionType.Full:
                    {
                        CertificateMessage certMessage;

                        try
                        {
                            certMessage = JsonSerializer.Deserialize<CertificateMessage>(message);
                        }
                        catch { return; };

                        if (certMessage.MessageType != Constants.TargetMessageType) return;

                        FullHandler.Invoke(sender, certMessage.Data.Leaf);

                        break;
                    }
                case ConnectionType.DomainsOnly:
                    {
                        DomainsOnlyMessage domainsOnlyMessage;

                        try
                        {
                            domainsOnlyMessage = JsonSerializer.Deserialize<DomainsOnlyMessage>(message);
                        }
                        catch { return; };

                        foreach (string hostname in domainsOnlyMessage.Hostnames)
                            DomainsOnlyHandler.Invoke(sender, hostname);

                        break;
                    }
            }
        }
    }
}