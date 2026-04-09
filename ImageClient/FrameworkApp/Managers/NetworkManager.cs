using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FrameworkApp.Models;
using FrameworkApp.Utils;

namespace FrameworkApp.Managers
{
    public class NetworkManager
    {
        private readonly string _host;
        private readonly int _port;
        private readonly Logger _logger;
        private readonly PacketHandler _packetHandler;

        private TcpClient? _client;
        private NetworkStream? _stream;

        public bool IsConnected => _client?.Connected == true;

        public NetworkManager(string host, int port, Logger logger)
        {
            _host = host;
            _port = port;
            _logger = logger;
            _packetHandler = new PacketHandler();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (IsConnected)
            {
                return;
            }

            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port, cancellationToken);
            _stream = _client.GetStream();

            _logger.Log($"CONNECTED | Host={_host} | Port={_port}");
        }

        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch
            {
            }

            _logger.Log("DISCONNECTED");
        }

        public async Task<(bool Success, string Message)> LoginAsync(string username, string password, CancellationToken cancellationToken)
        {
            EnsureConnected();

            string credentials = $"{username}:{password}";

            Packet loginPacket = new Packet
            {
                PacketType = PacketTypes.Request,
                CommandId = CommandIds.Login,
                SequenceNumber = 0,
                Payload = Encoding.UTF8.GetBytes(credentials)
            };

            await SendPacketAsync(loginPacket, cancellationToken);
            Packet response = await ReceivePacketAsync(cancellationToken);

            string message = Encoding.UTF8.GetString(response.Payload ?? Array.Empty<byte>());

            return response.CommandId == CommandIds.Ack
                ? (true, message)
                : (false, message);
        }

        public async Task<(bool Success, string Message, string? SavedPath)> RequestImageAsync(
            string fileName,
            ImageManager imageManager,
            IProgress<int>? progress,
            CancellationToken cancellationToken)
        {
            EnsureConnected();

            imageManager.Reset();

            Packet requestPacket = new Packet
            {
                PacketType = PacketTypes.Request,
                CommandId = CommandIds.RequestImage,
                SequenceNumber = 0,
                Payload = Encoding.UTF8.GetBytes(fileName ?? string.Empty)
            };

            await SendPacketAsync(requestPacket, cancellationToken);

            Packet ackOrError = await ReceivePacketAsync(cancellationToken);
            string firstMessage = Encoding.UTF8.GetString(ackOrError.Payload ?? Array.Empty<byte>());

            if (ackOrError.CommandId == CommandIds.Error)
            {
                return (false, firstMessage, null);
            }

            int chunkCounter = 0;

            while (true)
            {
                Packet packet = await ReceivePacketAsync(cancellationToken);

                if (packet.CommandId == CommandIds.ImageChunk)
                {
                    imageManager.AddChunk(packet);
                    chunkCounter++;
                    progress?.Report(Math.Min(95, chunkCounter * 5));
                    continue;
                }

                if (packet.CommandId == CommandIds.ImageComplete)
                {
                    string finalName = Encoding.UTF8.GetString(packet.Payload ?? Array.Empty<byte>());
                    imageManager.SetFinalFileName(finalName);

                    string path = imageManager.SaveImage();
                    progress?.Report(100);

                    _logger.Log($"IMAGE SAVED | Path={path} | Chunks={imageManager.ChunkCount}");
                    return (true, "Image transfer complete.", path);
                }

                if (packet.CommandId == CommandIds.Error)
                {
                    string errorMessage = Encoding.UTF8.GetString(packet.Payload ?? Array.Empty<byte>());
                    return (false, errorMessage, null);
                }
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken)
        {
            if (!IsConnected || _stream == null)
            {
                return;
            }

            Packet logoutPacket = new Packet
            {
                PacketType = PacketTypes.Request,
                CommandId = CommandIds.Logout,
                SequenceNumber = 0,
                Payload = Array.Empty<byte>()
            };

            await SendPacketAsync(logoutPacket, cancellationToken);
            _logger.Log("LOGOUT SENT");
        }

        private async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken)
        {
            EnsureConnected();

            _logger.LogPacket("SENT", packet.PacketType, packet.CommandId, packet.SequenceNumber, packet.Payload?.Length ?? 0);
            await _packetHandler.WritePacketAsync(_stream!, packet, cancellationToken);
        }

        private async Task<Packet> ReceivePacketAsync(CancellationToken cancellationToken)
        {
            EnsureConnected();

            Packet packet = await _packetHandler.ReadPacketAsync(_stream!, cancellationToken);
            _logger.LogPacket("RECEIVED", packet.PacketType, packet.CommandId, packet.SequenceNumber, packet.PayloadLength);
            return packet;
        }

        private void EnsureConnected()
        {
            if (_client == null || _stream == null || !_client.Connected)
            {
                throw new InvalidOperationException("Client is not connected to the server.");
            }
        }
    }
}