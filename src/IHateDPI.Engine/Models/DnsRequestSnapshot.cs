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
public sealed class DnsRequestSnapshot : IDisposable
{
    /// <summary>
    /// Gets the source IP address in host byte order.
    /// </summary>
    public uint SrcIp { get; init; }

    /// <summary>
    /// Gets the destination IP address in host byte order.
    /// </summary>
    public uint DstIp { get; init; }

    /// <summary>
    /// Gets the source port number.
    /// </summary>
    public ushort SrcPort { get; init; }

    /// <summary>
    /// Gets the destination port number.
    /// </summary>
    public ushort DstPort { get; init; }

    /// <summary>
    /// Gets the managed buffer containing the raw DNS query data.
    /// </summary>
    public byte[]? PayloadBuffer { get; init; }

    /// <summary>
    /// Gets the length of the valid data within the <see cref="PayloadBuffer"/>.
    /// </summary>
    public int PayloadLength { get; init; }

    /// <summary>
    /// Gets the WinDivert address information captured with the original packet.
    /// <para>Required to inject the DNS response back into the correct network interface.</para>
    /// </summary>
    public WinDivertAddress OriginalAddressStruct { get; init; }

    /// <summary>
    /// Returns the rented <see cref="PayloadBuffer"/> to the shared <see cref="ArrayPool{T}"/>.
    /// </summary>
    public void Dispose()
    {
        if (PayloadBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(PayloadBuffer);
        }
        GC.SuppressFinalize(this);
    }
}