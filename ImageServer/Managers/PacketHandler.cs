using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageServer.Models;

namespace ImageServer.Managers
{
    public class PacketHandler
    {
        private const int HeaderSize = sizeof(int) * 5;

        public byte[] Serialize(Packet packet)
        {
            packet.Payload ??= Array.Empty<byte>();
            packet.PayloadLength = packet.Payload.Length;
            packet.ValidationField = CalculateValidationField(packet);

            byte[] buffer = new byte[HeaderSize + packet.PayloadLength + sizeof(int)];
            int offset = 0;

            WriteInt(buffer, ref offset, packet.PacketType);
            WriteInt(buffer, ref offset, packet.CommandId);
            WriteInt(buffer, ref offset, packet.PayloadLength);
            WriteInt(buffer, ref offset, packet.SequenceNumber);
            WriteInt(buffer, ref offset, packet.ValidationField);

            if (packet.PayloadLength > 0)
            {
                Buffer.BlockCopy(packet.Payload, 0, buffer, offset, packet.PayloadLength);
                offset += packet.PayloadLength;
            }

            WriteInt(buffer, ref offset, packet.ValidationField);
            return buffer;
        }

        public async Task<Packet> ReadPacketAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] header = await ReadExactAsync(stream, HeaderSize, cancellationToken);
            int offset = 0;

            int packetType = ReadInt(header, ref offset);
            int commandId = ReadInt(header, ref offset);
            int payloadLength = ReadInt(header, ref offset);
            int sequenceNumber = ReadInt(header, ref offset);
            int validationField = ReadInt(header, ref offset);

            if (payloadLength < 0)
            {
                throw new InvalidDataException("Negative payload length received.");
            }

            byte[] payload = payloadLength == 0
                ? Array.Empty<byte>()
                : await ReadExactAsync(stream, payloadLength, cancellationToken);

            byte[] trailerBuffer = await ReadExactAsync(stream, sizeof(int), cancellationToken);
            int trailerOffset = 0;
            int trailerValidationField = ReadInt(trailerBuffer, ref trailerOffset);

            Packet packet = new Packet
            {
                PacketType = packetType,
                CommandId = commandId,
                PayloadLength = payloadLength,
                SequenceNumber = sequenceNumber,
                ValidationField = validationField,
                Payload = payload
            };

            if (trailerValidationField != packet.ValidationField)
            {
                throw new InvalidDataException("Trailer validation field mismatch.");
            }

            int expected = CalculateValidationField(packet);
            if (expected != packet.ValidationField)
            {
                throw new InvalidDataException("Packet integrity validation failed.");
            }

            return packet;
        }

        public async Task WritePacketAsync(Stream stream, Packet packet, CancellationToken cancellationToken)
        {
            byte[] bytes = Serialize(packet);
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private async Task<byte[]> ReadExactAsync(Stream stream, int size, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[size];
            int totalRead = 0;

            while (totalRead < size)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead, size - totalRead), cancellationToken);
                if (read == 0)
                {
                    throw new EndOfStreamException("Remote client disconnected while reading packet data.");
                }

                totalRead += read;
            }

            return buffer;
        }

        private int CalculateValidationField(Packet packet)
        {
            int value = 0;
            value ^= packet.PacketType;
            value ^= packet.CommandId;
            value ^= packet.PayloadLength;
            value ^= packet.SequenceNumber;

            foreach (byte b in packet.Payload ?? Array.Empty<byte>())
            {
                value ^= b;
            }

            return value;
        }

        private void WriteInt(byte[] buffer, ref int offset, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, sizeof(int)), value);
            offset += sizeof(int);
        }

        private int ReadInt(byte[] buffer, ref int offset)
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset, sizeof(int)));
            offset += sizeof(int);
            return value;
        }
    }
}