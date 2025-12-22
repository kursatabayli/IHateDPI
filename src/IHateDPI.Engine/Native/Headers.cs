using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace IHateDPI.Engine.Native;

// NOTE: These structs map directly to raw network bits.
// The "Pack = 1" alignment prevents the compiler from adding padding bytes,
// ensuring the struct size matches the protocol header exactly.

/// <summary>
/// Represents the IPv4 packet header structure (20 bytes minimum).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct IPHdr
{
    /// <summary>
    /// Contains both the Version (high 4 bits) and Internet Header Length (low 4 bits).
    /// <para>Bit masking is required to access individual values.</para>
    /// </summary>
    public byte HdrLengthAndVersion;

    /// <summary>
    /// Type of Service (TOS) / Differentiated Services Code Point (DSCP).
    /// Used for packet prioritization (QoS).
    /// </summary>
    public byte TOS;

    /// <summary>
    /// Total length of the packet (Header + Payload).
    /// <para>Stored in Network Byte Order (Big Endian).</para>
    /// </summary>
    public ushort Length;

    /// <summary>
    /// Identification field used primarily for reassembling fragmented packets.
    /// </summary>
    public ushort Id;

    /// <summary>
    /// Fragment Offset and Flags (Don't Fragment / More Fragments).
    /// </summary>
    public ushort FragOff0;

    /// <summary>
    /// Time To Live (TTL). Decremented by one at each hop (router) to prevent infinite loops.
    /// </summary>
    public byte TTL;

    /// <summary>
    /// Protocol used in the data portion of the IP datagram (e.g., 6 = TCP, 17 = UDP, 1 = ICMP).
    /// </summary>
    public byte Protocol;

    /// <summary>
    /// Header Checksum used for error checking the header contents.
    /// </summary>
    public ushort Checksum;

    /// <summary>
    /// Source IP address (32-bit).
    /// </summary>
    public uint SrcAddr;

    /// <summary>
    /// Destination IP address (32-bit).
    /// </summary>
    public uint DstAddr;

    // --- Helpers ---

    /// <summary>
    /// Gets the Internet Header Length (IHL) in bytes.
    /// </summary>
    public readonly int HdrLength => (HdrLengthAndVersion & 0x0F) * 4;

    /// <summary>
    /// Gets the IP Version (usually 4).
    /// </summary>
    public readonly int Version => (HdrLengthAndVersion >> 4);
}

/// <summary>
/// Represents the TCP protocol header structure.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct TCPHdr
{
    /// <summary>
    /// Source port number (Network Byte Order / Big Endian).
    /// </summary>
    public ushort SrcPort;

    /// <summary>
    /// Destination port number (Network Byte Order / Big Endian).
    /// </summary>
    public ushort DstPort;

    /// <summary>
    /// Sequence Number. Tracks the byte order of the sent data.
    /// </summary>
    public uint SeqNum;

    /// <summary>
    /// Acknowledgment Number. Confirms receipt of data.
    /// </summary>
    public uint AckNum;

    /// <summary>
    /// Data Offset (4 bits), Reserved (3 bits), and NS flag (1 bit).
    /// </summary>
    public byte HdrLengthAndRes;

    /// <summary>
    /// Control flags (CWR, ECE, URG, ACK, PSH, RST, SYN, FIN).
    /// </summary>
    public byte Flags;

    /// <summary>
    /// The size of the receive window, specifying the number of bytes the sender is willing to receive.
    /// </summary>
    public ushort Window;

    /// <summary>
    /// Checksum field used for error checking of the header and data.
    /// </summary>
    public ushort Checksum;

    /// <summary>
    /// Urgent Pointer (if URG flag is set).
    /// </summary>
    public ushort UrgPtr;

    // --- Flag Helpers ---

    /// <summary>
    /// Gets the TCP Header length in bytes.
    /// </summary>
    public readonly int HeaderLength => (HdrLengthAndRes >> 4) * 4;

    public readonly bool Fin => (Flags & 0x01) != 0;
    public readonly bool Syn => (Flags & 0x02) != 0;
    public readonly bool Rst => (Flags & 0x04) != 0;
    public readonly bool Psh => (Flags & 0x08) != 0;
    public readonly bool Ack => (Flags & 0x10) != 0;
}

/// <summary>
/// Represents the UDP protocol header structure (8 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UDPHdr
{
    public ushort SrcPort;
    public ushort DstPort;
    /// <summary>
    /// Length of the UDP header and data.
    /// </summary>
    public ushort Length;
    public ushort Checksum;
}

/// <summary>
/// Represents the ICMP (Internet Control Message Protocol) header structure.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ICMPHdr
{
    /// <summary>
    /// ICMP Message Type (e.g., 3 = Destination Unreachable).
    /// </summary>
    public byte Type;

    /// <summary>
    /// ICMP Message Code (subtype of the message).
    /// </summary>
    public byte Code;

    public ushort Checksum;

    /// <summary>
    /// The rest of the header/body data depending on the type.
    /// </summary>
    public uint Body;
}