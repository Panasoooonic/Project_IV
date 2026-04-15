using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageServer.Models;
using ImageServer.Utils;

namespace ImageServer.Managers
{
    /// <summary>
/// Handles communication for a single client session.
/// </summary>
/// <remarks>
/// Processes incoming packets, maintains session state,
/// and executes client commands.
/// </remarks>
    public class ClientSession
    {
        private readonly TcpClient _client;
        private readonly Logger _logger;
        private readonly PacketHandler _packetHandler;
        private readonly AuthManager _authManager;
        private readonly StateManager _stateManager;
        private readonly ImageTransferManager _imageTransferManager;
        private readonly Action<string> _uiLog;
        private readonly Action _onSessionClosed;

        public bool IsAuthenticated { get; private set; }
        public string AuthenticatedUsername { get; private set; } = string.Empty;

        public ClientSession(
            TcpClient client,
            ServerConfig config,
            Logger logger,
            Action<string> uiLog,
            Action onSessionClosed)
        {
            _client = client;
            _logger = logger;
            _uiLog = uiLog;
            _onSessionClosed = onSessionClosed;

            _packetHandler = new PacketHandler();
            _authManager = new AuthManager();
            _stateManager = new StateManager();
            _imageTransferManager = new ImageTransferManager(config, logger);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await using NetworkStream networkStream = _client.GetStream();

            string endpoint = _client.Client.RemoteEndPoint?.ToString() ?? "Unknown client";
            _stateManager.MarkConnected();

            _logger.Log($"CLIENT CONNECTED | Endpoint={endpoint}");
            _uiLog($"Client connected: {endpoint}");

            try
            {
                while (!cancellationToken.IsCancellationRequested && _client.Connected)
                {
                    Packet packet = await _packetHandler.ReadPacketAsync(networkStream, cancellationToken);

                    _logger.LogPacket("RECEIVED", packet.PacketType, packet.CommandId, packet.SequenceNumber, packet.PayloadLength);
                    _uiLog($"Received packet | Type={packet.PacketType} | Command={packet.CommandId} | Seq={packet.SequenceNumber} | Bytes={packet.PayloadLength}");

                    await ProcessPacketAsync(networkStream, packet, cancellationToken);
                }
            }
            catch (InvalidDataException ex)
            {
                _logger.LogMalformedPacket(ex.Message);
                _uiLog($"Malformed packet received: {ex.Message}");

                if (_client.Connected)
                {
                    try
                    {
                        await SendErrorAsync(networkStream, "Malformed packet. Closing session.", cancellationToken);
                    }
                    catch
                    {
                    }
                }
            }
            catch (EndOfStreamException)
            {
                _logger.Log("CLIENT DISCONNECTED | Remote side closed connection.");
                _uiLog("Client disconnected.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Session error: {ex.Message}");
                _uiLog($"Session error: {ex.Message}");

                if (_client.Connected)
                {
                    try
                    {
                        await SendErrorAsync(networkStream, ex.Message, cancellationToken);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                _stateManager.MarkClosed();
                _client.Close();
                _onSessionClosed();

                _logger.Log("CLIENT SESSION CLOSED");
                _uiLog("Client session closed.");
            }
        }

        private async Task ProcessPacketAsync(NetworkStream stream, Packet packet, CancellationToken cancellationToken)
        {
            switch (packet.CommandId)
            {
                case CommandIds.Login:
                    await HandleLoginAsync(stream, packet, cancellationToken);
                    break;

                case CommandIds.RequestImage:
                    await HandleImageRequestAsync(stream, packet, cancellationToken);
                    break;

                case CommandIds.Logout:
                    await HandleLogoutAsync(stream, cancellationToken);
                    break;

                default:
                    _logger.LogInvalidCommand(packet.CommandId);
                    await SendErrorAsync(stream, "Invalid command received.", cancellationToken);
                    break;
            }
        }

        private async Task HandleLoginAsync(NetworkStream stream, Packet packet, CancellationToken cancellationToken)
        {
            if (!_stateManager.CanAuthenticate())
            {
                await SendErrorAsync(stream, "Login not allowed in the current session state.", cancellationToken);
                return;
            }

            bool isValid = _authManager.TryValidateCredentials(packet.Payload, out string username);

            if (!isValid)
            {
                _logger.LogAuthFailure(username);
                _uiLog($"Authentication failed for user: {username}");
                await SendErrorAsync(stream, "Authentication failed.", cancellationToken);
                return;
            }

            IsAuthenticated = true;
            AuthenticatedUsername = username;

            _stateManager.MarkAuthenticated();
            _stateManager.MarkReady();

            _logger.LogAuthSuccess(username);
            _uiLog($"Authentication successful for user: {username}");

            await SendAckAsync(stream, "Authentication successful.", cancellationToken);
        }

        private async Task HandleImageRequestAsync(NetworkStream stream, Packet packet, CancellationToken cancellationToken)
        {
            if (!_stateManager.CanRequestImage(IsAuthenticated))
            {
                await SendErrorAsync(stream, "Authenticate first. Image request not allowed in current state.", cancellationToken);
                return;
            }

            string requestedFileName = Encoding.UTF8.GetString(packet.Payload ?? Array.Empty<byte>());
            string displayName = string.IsNullOrWhiteSpace(requestedFileName) ? "(default image)" : requestedFileName;

            _stateManager.MarkSendingImage();
            _uiLog($"Beginning image transfer for: {displayName}");

            await SendAckAsync(stream, $"Sending image: {displayName}", cancellationToken);

            try
            {
                int chunksSent = await _imageTransferManager.SendImageAsync(
                    requestedFileName,
                    stream,
                    _packetHandler,
                    cancellationToken);

                _uiLog($"Image transfer complete. Chunks sent: {chunksSent}");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError($"File not found: {ex.FileName}");
                _uiLog($"Requested file not found: {displayName}");
                await SendErrorAsync(stream, "Requested image was not found.", cancellationToken);
            }
            finally
            {
                if (_client.Connected)
                {
                    _stateManager.MarkReady();
                }
            }
        }

        private async Task HandleLogoutAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            await SendAckAsync(stream, "Logout successful.", cancellationToken);
            _stateManager.MarkClosed();
            _client.Close();
        }

        private async Task SendAckAsync(NetworkStream stream, string message, CancellationToken cancellationToken)
        {
            Packet packet = new Packet
            {
                PacketType = PacketTypes.Response,
                CommandId = CommandIds.Ack,
                SequenceNumber = 0,
                Payload = Encoding.UTF8.GetBytes(message)
            };

            _logger.LogPacket("SENT", packet.PacketType, packet.CommandId, packet.SequenceNumber, packet.Payload.Length);
            _uiLog($"Sent ACK: {message}");

            await _packetHandler.WritePacketAsync(stream, packet, cancellationToken);
        }

        private async Task SendErrorAsync(NetworkStream stream, string message, CancellationToken cancellationToken)
        {
            Packet packet = new Packet
            {
                PacketType = PacketTypes.Error,
                CommandId = CommandIds.Error,
                SequenceNumber = 0,
                Payload = Encoding.UTF8.GetBytes(message)
            };

            _logger.LogPacket("SENT", packet.PacketType, packet.CommandId, packet.SequenceNumber, packet.Payload.Length);
            _uiLog($"Sent ERROR: {message}");

            await _packetHandler.WritePacketAsync(stream, packet, cancellationToken);
        }
    }
}