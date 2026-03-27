using ImageClient.Models;
using System;

namespace ImageClient.Managers
{
    public class PacketHandler
    {
        public Packet CreatePacket(int type, int command, byte[] payload)
        {
            return new Packet
            {
                PacketType = type,
                CommandId = command,
                Payload = payload,
                PayloadLength = payload?.Length ?? 0,
                SequenceNumber = 0,
                CRC = 0
            };
        }

        public bool ValidatePacket(Packet packet)
        {
            if (packet == null) return false; 
            return true;
        }

        public byte[] GetPayload(Packet packet)
        {
            return packet.Payload;
        }
    }
}