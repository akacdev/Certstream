using Certstream.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Certstream
{
    /// <summary>
    /// Certstream Websocket Client 
    /// </summary>
    public class CertstreamClient
    {
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// How often should the WebSocket connection be pinged, in <b>milliseconds.</b>
        /// </summary>
        private readonly int _pingInterval;

        /// <summary>
        /// The delay between reconnecting a closed WebSocket connection, in <b>milliseconds.</b>
        /// </summary>
        private readonly int _reconnectionDelay;

        /// <summary>
        /// The maximum limit of consecutive reconnection fails until an exception is thrown.
        /// </summary>
        private readonly int _maxRetries;

        /// <summary>
        /// The hostname to connect to.
        /// </summary>
        public readonly string _hostname;

        /// <summary>
        /// The current amount of consecutive reconnection fails.
        /// </summary>
        private int _retries = 0;

        /// <summary>
        /// The calculated connection Uri.
        /// </summary>
        private readonly Uri _connectionUri;

        /// <summary>
        /// The connection type to use in this instance.
        /// </summary>
        private readonly ConnectionType _connectionType;

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;

        private EventHandler<LeafCertificate> _fullHandler;
        private EventHandler<string> _domainsOnlyHandler;

        /// <summary>
        /// Fired whenever a new SSL certificate is issued and received from the WebSocket connection.
        /// <para>Requires <see cref="ConnectionType.Full"/> to be used.</para>
        /// </summary>
        public event EventHandler<LeafCertificate> CertificateIssued
        {
            add
            {
                if (_connectionType != ConnectionType.Full)
                {
                    throw new("Only available in connection type: Full.");
                }

                _fullHandler += value;
            }
            remove
            {
                _fullHandler -= value;
            }
        }

        /// <summary>
        /// Fired whenever a new hostname is spotted and received from the WebSocket connection.
        /// <para>Requires <see cref="ConnectionType.DomainsOnly"/> to be used.</para>
        /// </summary>
        public event EventHandler<string> HostnameSpotted
        {
            add
            {
                if (_connectionType != ConnectionType.DomainsOnly)
                {
                    throw new("Only available in connection type: Domains-Only.");
                }

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
        /// <param name="secureWebsocket">Use WebSocket Secure</param>
        /// <param name="hostname">The Certstream server hostname to connect to. Use this if you want to connect to your own instance.</param>
        /// <param name="maxRetries">The maximum limit of consecutive reconnection fails until an exception is thrown.<para>Set to a negative value for no limit.</para></param>
        /// <param name="reconnectionDelay">The delay to wait before attempting to reconnect.</param>
        /// <param name="logger">The delay to wait before attempting to reconnect.</param>
        /// <exception cref="ArgumentException"></exception>
        public CertstreamClient(
            ConnectionType connectionType = Constants.ConnectionType,
            bool secureWebsocket = true,
            string hostname = Constants.Hostname,
            int maxRetries = Constants.MaxRetries,
            int reconnectionDelay = Constants.ReconnectionDelay,
            ILogger<CertstreamClient> logger = default)
        {
            _connectionType = connectionType;
            _hostname = hostname;
            _maxRetries = maxRetries;
            _reconnectionDelay = reconnectionDelay;

            _logger = logger ?? new NullLogger<CertstreamClient>();

            string path = connectionType switch
            {
                ConnectionType.Full => "/",
                ConnectionType.DomainsOnly => "/domains-only",
                _ => throw new NotImplementedException()
            };

            string rawUrl;
            if (secureWebsocket)
            {
                rawUrl = string.Concat("wss://", hostname, path);
            }
            else
            {
                rawUrl = string.Concat("ws://", hostname, path);
            }

            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException("Invalid hostname provided.", nameof(hostname));
            }

            _connectionUri = uri;
        }

        /// <summary>
        /// Start receive certificate data
        /// </summary>
        public Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            if (!_cancellationTokenSource?.IsCancellationRequested ?? false)
            {
                return Task.FromResult(true);
            }

            _cancellationTokenSource = new();
            _ = Task.Run(async () => await MessageProcessorAsync(), cancellationToken);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Stop receive certificate data
        /// </summary>
        public Task<bool> StopAsync(CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource == null)
            {
                return Task.FromResult(false);
            }

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return Task.FromResult(false);
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

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
        private async Task MessageProcessorAsync()
        {
            this._logger.LogInformation($"{nameof(MessageProcessorAsync)} - Start");

            byte[] receiveBuffer = new byte[Constants.BufferSize];
            int offset = 0;

            CreateWebsocket();

            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
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
                            this._logger.LogError(exception, $"{nameof(MessageProcessorAsync)} - Websocket cleanup failure");
                        }
                    }

                    if (_webSocket.State != WebSocketState.Open)
                    {
                        if (_maxRetries >= 0 && _retries >= _maxRetries)
                        {
                            throw new CertstreamException($"Failed to connect to Certstream after {_retries} retries.");
                        }

                        try
                        {
                            await _webSocket.ConnectAsync(_connectionUri, _cancellationTokenSource.Token);
                            _retries = 0;
                            continue;
                        }
                        catch (Exception exception)
                        {
                            this._logger.LogError(exception, $"{nameof(MessageProcessorAsync)} - Connect failure");
                            await Task.Delay(_reconnectionDelay);
                            _retries++;
                        }
                    }

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

                            WebSocketReceiveResult result = await _webSocket.ReceiveAsync(bytesReceived, _cancellationTokenSource.Token);

                            offset += result.Count;

                            if (result.EndOfMessage) break;
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch (Exception exception)
                        {
                            this._logger.LogError(exception, $"{nameof(MessageProcessorAsync)} - Process Data");
                            break;
                        }
                    }

                    if (offset != 0)
                    {
                        try
                        {
                            OnMessage(this, Encoding.UTF8.GetString(receiveBuffer, 0, offset));
                            offset = 0;
                        }
                        catch (Exception exception)
                        {
                            this._logger.LogError(exception, $"{nameof(MessageProcessorAsync)} - Distribute Message");
                        }
                    }
                }
            }
            finally
            {
                this._logger.LogInformation($"{nameof(MessageProcessorAsync)} - Stop");
            }
        }

        /// <summary>
        /// Called whenever a WebSocket message is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnMessage(object sender, string message)
        {
            switch (_connectionType)
            {
                case ConnectionType.Full:
                    {
                        CertificateMessage certMessage;

                        try
                        {
                            certMessage = JsonSerializer.Deserialize<CertificateMessage>(message);
                        }
                        catch (Exception exception)
                        {
                            this._logger.LogError(exception, $"{nameof(OnMessage)} - ConnectionType Full");
                            return;
                        };

                        if (certMessage.MessageType != Constants.TargetMessageType)
                        {
                            return;
                        }

                        _fullHandler.Invoke(sender, certMessage.Data.Leaf);

                        break;
                    }
                case ConnectionType.DomainsOnly:
                    {
                        DomainsOnlyMessage domainsOnlyMessage;

                        try
                        {
                            domainsOnlyMessage = JsonSerializer.Deserialize<DomainsOnlyMessage>(message);
                        }
                        catch (Exception exception)
                        {
                            this._logger.LogError(exception, $"{nameof(OnMessage)} - ConnectionType DomainsOnly");
                            return;
                        };

                        foreach (string hostname in domainsOnlyMessage.Hostnames)
                        {
                            _domainsOnlyHandler.Invoke(sender, hostname);
                        }

                        break;
                    }
            }
        }
    }
}
