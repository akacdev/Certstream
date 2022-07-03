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
    /// The main class to connect to the Certstream server.
    /// </summary>
    public class CertstreamClient
    {
        /// <summary>
        /// The WebSocket URL to connect to.
        /// </summary>
        public const string URL = "wss://certstream.calidog.io";
        /// <summary>
        /// The UserAgent to use when connecting.
        /// </summary>
        public const string UserAgent = "C# Certstream Client - https://github.com/actually-akac/Certstream";
        /// <summary>
        /// How often should the WebSocket be pinged, in <b>milliseconds.</b>
        /// </summary>
        public const int PingInterval = 5000;
        /// <summary>
        /// The delay between reconnecting to a closed WebSocket connection, in <b>milliseconds.</b>
        /// </summary>
        public const int ReconnectionDelay = 1000;
        /// <summary>
        /// The maximum limit of consecutive reconnection fails until an exception is thrown.
        /// </summary>
        private const int MaxRetries = 10;

        private ClientWebSocket WS;
        private CancellationTokenSource Source;
        private Timer Pinger;
        private bool Running;
        private int Retries = 0;
        private readonly ArraySegment<byte> PingBytes = new(Encoding.UTF8.GetBytes("ping"));

        private EventHandler<LeafCert> Handler;
        /// <summary>
        /// Fired whenever a new SSL certificate is issued and received in the WebSocket.
        /// </summary>
        public event EventHandler<LeafCert> CertificateIssued
        {
            add
            {
                Handler += value;
                if (Handler.GetInvocationList().Length == 1) Start();
            }
            remove
            {
                Handler -= value;
                if (Handler is null || Handler.GetInvocationList().Length == 0) Stop();
            }
        }

        public CertstreamClient()
        {
            
        }

        /// <summary>
        /// Forcibly start collecting certificates.
        /// </summary>
        public async void Start()
        {
            Running = true;
            Connect();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Forcibly stop collecting certificates.
        /// </summary>
        public async void Stop()
        {
            Running = false;
            await WS.CloseAsync(WebSocketCloseStatus.NormalClosure, "User demands a WebSocket closure.", Source.Token);
        }

        /// <summary>
        /// The main method that connects to the WebSocket and listens for new messages.
        /// </summary>
        public async void Connect()
        {
            if (!Running) return;

            Retries++;
            Source = new();
            WS = new();
            WS.Options.SetRequestHeader("User-Agent", UserAgent);

            Pinger = new();
            Pinger.Interval = PingInterval;
            Pinger.Elapsed += async (o, e) => await Ping();
            Pinger.Start();

            try
            {
                await WS.ConnectAsync(new Uri(URL), Source.Token);
                Retries = 0;

                Debug.WriteLine($"WebSocket connected to {URL}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect to WebSocket: {ex.Message}");

                if (Retries >= MaxRetries) throw new($"Failed to connect to Certstream {Retries} times.");

                await Task.Delay(ReconnectionDelay);
                Connect();
                return;
            }

            while (WS.State == WebSocketState.Open)
            {
                var receiveBuffer = new byte[16384];

                var offset = 0;
                while (true)
                {
                    try
                    {
                        ArraySegment<byte> bytesReceived = new(receiveBuffer, offset, receiveBuffer.Length);

                        WebSocketReceiveResult result = await WS.ReceiveAsync(bytesReceived, Source.Token);
                        offset += result.Count;

                        if (result.EndOfMessage) break;
                    }
                    catch { break; };
                }

                if (offset != 0) OnMessage(this, Encoding.UTF8.GetString(receiveBuffer, 0, offset));
            }

            Debug.WriteLine($"Connection lost: {WS.CloseStatus} => {WS.CloseStatusDescription}");
            await Task.Delay(ReconnectionDelay);
            Connect();
        }

        /// <summary>
        /// The method that sends a <c>ping</c> message into the WebSocket to prevent it from closing.
        /// </summary>
        /// <returns></returns>
        public async Task Ping()
        {
            if (WS.State != WebSocketState.Open) Pinger.Stop();

            try
            {
                await WS.SendAsync(PingBytes, WebSocketMessageType.Text, true, Source.Token);
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
            CertMessage certMsg;

            try
            {
                certMsg = JsonSerializer.Deserialize<CertMessage>(msg);
            }
            catch { return; };

            if (certMsg.MessageType != "certificate_update") return;

            Handler.Invoke(this, certMsg.Data.LeafCert);
        }
    }
}