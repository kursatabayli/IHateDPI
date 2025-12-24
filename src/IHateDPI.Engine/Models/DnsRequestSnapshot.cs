using IHateDPI.Engine.Native;
using System.Buffers;

namespace IHateDPI.Engine.Models;

/// <summary>
/// Represents a thread-safe snapshot of a captured DNS request.
/// </summary>
/// <remarks>
/// Since the original packet memory is freed in the WinDivert loop, this class holds a persistent copy of the data
/// required for asynchronous processing (e.g., DoH resolution).
/// </remarks>
public readonly record struct DnsRequestSnapshot(
    /// <summary>
    /// Gets the source IP address in host byte order.
    /// </summary>
    uint SrcIp,

    /// <summary>
    /// Gets the destination IP address in host byte order.
    /// </summary>
    uint DstIp,

    /// <summary>
    /// Gets the source port number.
    /// </summary>
    ushort SrcPort,

    /// <summary>
    /// Gets the destination port number.
    /// </summary>
    ushort DstPort,

    /// <summary>
    /// Gets the managed buffer containing the raw DNS query data.
    /// </summary>
    byte[]? PayloadBuffer,

    /// <summary>
    /// Gets the length of the valid data within the <see cref="PayloadBuffer"/>.
    /// </summary>
    int PayloadLength,

    /// <summary>
    /// Gets the WinDivert address information captured with the original packet.
    /// <para>Required to inject the DNS response back into the correct network interface.</para>
    /// </summary>
    WinDivertAddress OriginalAddressStruct);
