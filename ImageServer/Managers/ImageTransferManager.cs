using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageServer.Models;
using ImageServer.Utils;

namespace ImageServer.Managers
{
    public class ImageTransferManager
    {
        private readonly ServerConfig _config;
        private readonly Logger _logger;

        public ImageTransferManager(ServerConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;

            Directory.CreateDirectory(_config.ImageDirectory);
        }

        public async Task<int> SendImageAsync(
            string requestedFileName,
            Stream stream,
            PacketHandler packetHandler,
            CancellationToken cancellationToken)
        {
            string imagePath = _config.GetImagePath(requestedFileName);

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Requested image was not found.", imagePath);
            }

            string fileName = Path.GetFileName(imagePath);
            _logger.LogTransferStart(fileName);

            await using FileStream file = File.OpenRead(imagePath);

            int sequenceNumber = 1;
            int chunksSent = 0;
            byte[] buffer = new byte[_config.ChunkSize];
            int bytesRead;

            while ((bytesRead = await file.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                byte[] payload = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, payload, 0, bytesRead);

                Packet chunkPacket = new Packet
                {
                    PacketType = PacketTypes.Data,
                    CommandId = CommandIds.ImageChunk,
                    SequenceNumber = sequenceNumber++,
                    Payload = payload
                };

                await packetHandler.WritePacketAsync(stream, chunkPacket, cancellationToken);
                chunksSent++;
            }

            Packet completePacket = new Packet
            {
                PacketType = PacketTypes.Response,
                CommandId = CommandIds.ImageComplete,
                SequenceNumber = sequenceNumber,
                Payload = Encoding.UTF8.GetBytes(fileName)
            };

            await packetHandler.WritePacketAsync(stream, completePacket, cancellationToken);

            _logger.LogTransferComplete(fileName, chunksSent);
            return chunksSent;
        }
    }
}