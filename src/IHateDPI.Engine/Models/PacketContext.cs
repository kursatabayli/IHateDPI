using IHateDPI.Engine.Native;

namespace IHateDPI.Engine.Models;

/// <summary>
/// Represents a low-level context for a single network packet captured by WinDivert.
/// </summary>
/// <remarks>
/// Designed as a <see langword="ref struct"/> to minimize allocations by operating directly on raw memory pointers
/// without copying data to the managed heap.
/// </remarks>
public unsafe ref struct PacketContext
{
    /// <summary>
    /// Gets the WinDivert handle associated with the capture session. 
    /// Required for re-injecting the packet back into the network stack.
    /// </summary>
    public WinDivertHandle Handle { get; init; }

    /// <summary>
    /// Gets the pointer to the start of the raw packet buffer.
    /// </summary>
    public byte* RawPacket { get; init; }

    /// <summary>
    /// Gets or sets the total length of the packet in bytes.
    /// </summary>
    public int PacketLen { get; set; }

    /// <summary>
    /// Gets the pointer to the <see cref="WinDivertAddress"/> structure containing metadata 
    /// (e.g., interface index, direction, timestamp).
    /// </summary>
    public WinDivertAddress* Address { get; init; }

    /// <summary>
    /// Gets a reference to the <see cref="WinDivertAddress"/> structure for easier access.
    /// </summary>
    public readonly ref WinDivertAddress AddressRef => ref *Address;

    /// <summary>
    /// Gets the pointer to the IPv4 header, or <c>null</c> if not present.
    /// </summary>
    public IPHdr* IpHdr { get; init; }

    /// <summary>
    /// Gets the pointer to the IPv6 header, or <c>null</c> if not present.
    /// </summary>
    public IPHdr* Ipv6Hdr { get; init; }

    /// <summary>
    /// Gets the pointer to the TCP header, or <c>null</c> if the protocol is not TCP.
    /// </summary>
    public TCPHdr* TcpHdr { get; init; }

    /// <summary>
    /// Gets the pointer to the UDP header, or <c>null</c> if the protocol is not UDP.
    /// </summary>
    public UDPHdr* UdpHdr { get; init; }

    /// <summary>
    /// Gets the pointer to the packet payload (data following the transport header).
    /// </summary>
    public byte* Payload { get; init; }

    /// <summary>
    /// Gets or sets the length of the payload in bytes.
    /// </summary>
    public uint PayloadLen { get; set; }

    /// <summary>
    /// Gets a value indicating whether the packet represents outbound traffic.
    /// </summary>
    public readonly bool IsOutbound => Address->Outbound;

    /// <summary>
    /// Gets a value indicating whether the packet is IPv6.
    /// </summary>
    public readonly bool IsIpv6 => Address->IPv6;

    /// <summary>
    /// Gets a <see cref="Span{T}"/> representing the packet payload.
    /// </summary>
    public readonly Span<byte> PayloadSpan => Payload != null ? new Span<byte>(Payload, (int)PayloadLen) : [];

    /// <summary>
    /// Gets a <see cref="Span{T}"/> representing the entire raw packet.
    /// </summary>
    public readonly Span<byte> FullPacketSpan => new(RawPacket, PacketLen);
}