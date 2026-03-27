namespace ImageClient.Models
{
    public class Packet
    {
        public int PacketType { get; set; }
        public int CommandId { get; set; }
        public int PayloadLength { get; set; }
        public byte[] Payload { get; set; }
        public int SequenceNumber { get; set; }
        public int CRC { get; set; }
    }
}