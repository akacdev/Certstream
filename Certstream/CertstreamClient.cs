using Certstream.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace Certstream
{
    /// <summary>
    /// Certstream Websocket Client 
    /// </summary>
    public class CertstreamClient
    {
        private readonly string _hostname;
        private readonly int _pingInterval;
        private readonly int _reconnectionDelay;
        private int _retries = 0;
        private readonly int _maxRetries;
        private readonly Uri _connectionUri;
        private readonly ConnectionType _connectionType;
        private readonly Timer _pingTimer = new();

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;

        private EventHandler<LeafCertificate> _fullHandler;
        private EventHandler<string> _domainsOnlyHandler;

        private readonly ILogger _logger;

        /// <summary>
        /// Fired whenever a new SSL certificate is issued and received from the WebSocket connection.
        /// <para>Requires <see cref="ConnectionType.Full"/> to be used.</para>
        /// </summary>
        public event EventHandler<LeafCertificate> CertificateIssued
        {
            add
            {
                if (_connectionType != ConnectionType.Full)
                    throw new("Only available in connection type: Full.");

                _fullHandler += value;
            }
            remove
            {
                _fullHandler -= value;
            }
        }

        /// <summary>
        /// Fired whenever a new hostname is received from the WebSocket connection.
        /// <para>Requires <see cref="ConnectionType.DomainsOnly"/> to be used.</para>
        /// </summary>
        public event EventHandler<string> HostnameReceived
        {
            add
            {
                if (_connectionType != ConnectionType.DomainsOnly)
                    throw new("Only available in connection type: Domains-Only.");

                _domainsOnlyHandler += value;
            }
            remove
            {
                _domainsOnlyHandler -= value;
            }
        }

        /// <summary>
        /// Create a new instance of the Certstream client.
        /// </summary>
        /// <param name="connectionType">The type of connection to use in this instance.</param>
        /// <param name="secureWebsocket">Whether to connect using WebSocket secure scheme.</param>
        /// <param name="hostname">The Certstream server hostname to connect to. Use this if you want to connect to your own instance.</param>
        /// <param name="pingInterval">The interval of sending ping messages to the server.</param>
        /// <param name="maxRetries">The maximum limit of consecutive reconnection fails until an exception is thrown.<para>Set to a negative value for no limit.</para></param>
        /// <param name="reconnectionDelay">The delay to wait before attempting to reconnect.</param>
        /// <param name="logger">The logger to use for advanced logging.</param>
        /// <exception cref="ArgumentException"></exception>
        public CertstreamClient(
            ConnectionType connectionType = Constants.ConnectionType,
            bool secureWebsocket = true,
            string hostname = Constants.Hostname,
            int pingInterval = Constants.PingInterval,
            int maxRetries = Constants.MaxRetries,
            int reconnectionDelay = Constants.ReconnectionDelay,
            ILogger<CertstreamClient> logger = default)
        {
            _connectionType = connectionType;
            _hostname = hostname;
            _pingInterval = pingInterval;
            _maxRetries = maxRetries;
            _reconnectionDelay = reconnectionDelay;
            _logger = logger ?? new NullLogger<CertstreamClient>();

            string path = connectionType switch
            {
                ConnectionType.Full => "/",
                ConnectionType.DomainsOnly => "/domains-only",
                _ => throw new NotImplementedException()
            };

            string rawUrl = secureWebsocket ? string.Concat("wss://", hostname, path) : string.Concat("ws://", hostname, path);
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri uri))
                throw new ArgumentException("Invalid hostname provided.", nameof(hostname));

            _connectionUri = uri;
        }

        /// <summary>
        /// Start receiveing certificate data.
        /// </summary>
        public Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            if (!_cancellationTokenSource?.IsCancellationRequested ?? false)
                return Task.FromResult(false);

            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new();
            _ = Task.Run(async () => await ProcessMessagesAsync(_cancellationTokenSource.Token), cancellationToken);

            StartPinging(cancellationToken);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Stop receiving certificate data.
        /// </summary>
        public Task<bool> StopAsync()
        {
            if (_cancellationTokenSource is null || _cancellationTokenSource.IsCancellationRequested)
                return Task.FromResult(false);

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            StopPinging();

            return Task.FromResult(true);
        }

        private void CreateWebsocket()
        {
            _webSocket = new();
            _webSocket.Options.SetRequestHeader("User-Agent", Constants.UserAgent);
        }

        /// <summary>
        /// The main method that connects to the WebSocket and listens for new messages.
        /// </summary>
        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(ProcessMessagesAsync)} - Starting");

            byte[] receiveBuffer = new byte[Constants.BufferSize];
            int offset = 0;

            CreateWebsocket();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_webSocket.State == WebSocketState.Aborted)
                    {
                        try
                        {
                            _webSocket.Dispose();

                            CreateWebsocket();
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, $"{nameof(ProcessMessagesAsync)} - WebSocket cleanup failure");
                        }
                    }

                    if (_webSocket.State != WebSocketState.Open)
                    {
                        if (_maxRetries >= 0 && _retries >= _maxRetries)
                            throw new CertstreamException($"Failed to connect to Certstream after {_retries} retries.");

                        try
                        {
                            await _webSocket.ConnectAsync(_connectionUri, cancellationToken);
                            _retries = 0;

                            _logger.LogInformation($"{nameof(ProcessMessagesAsync)} - Connected");
                            continue;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, $"{nameof(ProcessMessagesAsync)} - Connect failure");
                            await Task.Delay(_reconnectionDelay, cancellationToken);
                            _retries++;
                        }
                    }

                    while (true)
                    {
                        try
                        {
                            int remainingBufferSpace = receiveBuffer.Length - offset;
                            if (remainingBufferSpace <= 0)
                                throw new InvalidOperationException("Buffer overflow: The receive buffer is full.");

                            ArraySegment<byte> bytesReceived = new(receiveBuffer, offset, remainingBufferSpace);
                            WebSocketReceiveResult result = await _webSocket.ReceiveAsync(bytesReceived, cancellationToken);

                            offset += result.Count;

                            if (result.EndOfMessage) break;
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, $"{nameof(ProcessMessagesAsync)} - Process Data");
                            break;
                        }
                    }

                    if (offset != 0)
                    {
                        try
                        {
                            HandleMessage(this, new ReadOnlySpan<byte>(receiveBuffer, 0, offset));
                            offset = 0;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, $"{nameof(ProcessMessagesAsync)} - Distribute Message");
                        }
                    }
                }
            }
            finally
            {
                _logger.LogInformation($"{nameof(ProcessMessagesAsync)} - Stopping");
            }
        }

        /// <summary>
        /// Called whenever a WebSocket message is received.
        /// </summary>
        private void HandleMessage(object sender, ReadOnlySpan<byte> message)
        {
            switch (_connectionType)
            {
                case ConnectionType.Full:
                    {
                        HandleFullMessage(sender, message);
                        break;
                    }
                case ConnectionType.DomainsOnly:
                    {
                        HandleDomainOnlyMessage(sender, message);
                        break;
                    }
            }
        }

        private void HandleFullMessage(object sender, ReadOnlySpan<byte> message)
        {
            CertificateMessage certMessage;

            try
            {
                certMessage = JsonSerializer.Deserialize<CertificateMessage>(message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"{nameof(HandleMessage)} - ConnectionType Full");
                return;
            }

            if (certMessage.MessageType != Constants.TargetMessageType) return;

            _fullHandler?.Invoke(sender, certMessage.Data.Leaf);
        }

        private void HandleDomainOnlyMessage(object sender, ReadOnlySpan<byte> message)
        {
            DomainsOnlyMessage domainsOnlyMessage;

            try
            {
                domainsOnlyMessage = JsonSerializer.Deserialize<DomainsOnlyMessage>(message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"{nameof(HandleMessage)} - ConnectionType DomainsOnly");
                return;
            }

            foreach (string hostname in domainsOnlyMessage.Hostnames)
                _domainsOnlyHandler?.Invoke(sender, hostname);
        }

        private void StartPinging(CancellationToken cancellationToken)
        {
            _pingTimer.Interval = _pingInterval;
            _pingTimer.Elapsed += async (sender, e) => await SendPingAsync(cancellationToken);
            _pingTimer.AutoReset = true;
            _pingTimer.Start();
        }

        private void StopPinging()
        {
            if (_pingTimer is null) return;

            _pingTimer.Stop();
            _pingTimer.Dispose();
        }

        private async Task SendPingAsync(CancellationToken cancellationToken)
        {
            if (_webSocket?.State != WebSocketState.Open) return;

            try
            {
                await _webSocket.SendAsync(Constants.PingBytes, WebSocketMessageType.Text, true, cancellationToken);

                _logger.LogInformation($"{nameof(SendPingAsync)} - Ping message sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(SendPingAsync)} - Failed to send ping message");
            }
        }
    }
}