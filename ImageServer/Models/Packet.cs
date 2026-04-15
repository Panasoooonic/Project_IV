using System;

namespace ImageServer.Models
{
    /// <summary>
/// Represents a structured packet used for communication between client and server.
/// </summary>
/// <remarks>
/// Contains metadata such as packet type, command ID, payload length,
/// sequence number, and a validation field for integrity checking.
/// </remarks>
    public class Packet
    {
        /// <summary>
/// Type of packet (Request, Response, Data, Error)
/// </summary>
        public int PacketType { get; set; }
        /// <summary>
/// Command identifier for the packet
/// </summary>
        public int CommandId { get; set; }
        /// <summary>
/// Length of the payload in bytes
/// </summary>
        public int PayloadLength { get; set; }
        /// <summary>
/// Raw payload data
/// </summary>
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        /// <summary>
/// Sequence number used for ordering packets during transfer
/// </summary>
        public int SequenceNumber { get; set; }
        /// <summary>
/// Validation field used to verify packet integrity
/// </summary>
        public int ValidationField { get; set; }
    }
}