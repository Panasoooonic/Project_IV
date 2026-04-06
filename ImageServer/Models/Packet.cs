using System;

namespace ImageServer.Models
{
    public class Packet
    {
        public int PacketType { get; set; }
        public int CommandId { get; set; }
        public int PayloadLength { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        public int SequenceNumber { get; set; }
        public int ValidationField { get; set; }
    }
}