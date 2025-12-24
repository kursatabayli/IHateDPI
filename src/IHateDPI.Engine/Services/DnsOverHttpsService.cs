using IHateDPI.Engine.Abstractions;
using IHateDPI.Engine.Models;
using IHateDPI.Engine.Native;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace IHateDPI.Engine.Services;

/// <summary>
/// A background service that resolves captured DNS requests via the DoH (DNS over HTTPS) protocol.
/// <para>
/// It listens to a channel for DNS snapshots, resolves them asynchronously, and injects the responses 
/// back into the network stack as synthetic UDP packets, effectively bypassing local DNS tampering/poisoning.
/// </para>
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DnsOverHttpsService"/> class.
/// </remarks>
public sealed class DnsOverHttpsService(ChannelReader<DnsRequestSnapshot> reader, IDnsResolver dnsResolver, IPacketInjector injector)
{

    /// <summary>
    /// Starts the main service loop. Listens for and processes DNS requests arriving via the channel.
    /// </summary>
    /// <param name="token">A token to monitor for cancellation requests.</param>
    public async Task RunAsync(CancellationToken token)
    {
        await Parallel.ForEachAsync(reader.ReadAllAsync(token),
                    new ParallelOptions
                    {
                        CancellationToken = token,
                        MaxDegreeOfParallelism = 64 // Handle up to 64 concurrent DNS queries
                    },
                    async (snapshot, ct) =>
                    {
                        // Rent a temporary buffer from the shared pool to minimize heap allocations.
                        byte[] responseBuffer = ArrayPool<byte>.Shared.Rent(4096);
                        try
                        {
                            var requestMemory = new Memory<byte>(snapshot.PayloadBuffer, 0, snapshot.PayloadLength);
                            var responseMemory = new Memory<byte>(responseBuffer);

                            // Resolve the DNS query via HTTP/2 (DoH).
                            int bytesRead = await dnsResolver.ResolveToBufferAsync(requestMemory, responseMemory, token);

                            if (bytesRead > 0)
                            {
                                InjectResponse(snapshot, responseBuffer, bytesRead);
                            }
                        }
                        catch (Exception)
                        {
                            // Swallow exceptions to prevent the entire processing loop from crashing due to a single failed query.
                            // (Optional: Log the error)
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(responseBuffer);
                            ArrayPool<byte>.Shared.Return(snapshot.PayloadBuffer);
                        }
                    });
    }

    /// <summary>
    /// Constructs a raw UDP/IP packet using the response data from the DoH server and injects it into the network stack.
    /// </summary>
    /// <param name="req">The snapshot of the original DNS request (used for routing and port information).</param>
    /// <param name="responsePayload">The buffer containing the resolved DNS response data.</param>
    /// <param name="payloadLength">The length of the valid data in the response buffer.</param>
    [SkipLocalsInit]
    private unsafe void InjectResponse(in DnsRequestSnapshot req, byte[] responsePayload, int payloadLength)
    {
        int totalLen = 20 + 8 + payloadLength; // IP Header (20) + UDP Header (8) + DNS Data

        // Use stack memory for packet construction to avoid heap allocations (Zero-Allocation).
        Span<byte> injectionSpan = stackalloc byte[totalLen > 2048 ? totalLen : 2048];

        fixed (byte* pBuffer = injectionSpan)
        {
            // -- IP Header (IPv4) --
            IPHdr* ip = (IPHdr*)pBuffer;
            ip->HdrLengthAndVersion = 0x45;
            ip->Length = BinaryPrimitives.ReverseEndianness((ushort)totalLen);
            ip->TTL = 128;
            ip->Protocol = 17; // UDP
            ip->SrcAddr = req.DstIp; // Source: The original destination (e.g., Google DNS)
            ip->DstAddr = req.SrcIp; // Destination: The original source (Local Machine)

            // -- UDP Header --
            UDPHdr* udp = (UDPHdr*)(pBuffer + 20);
            udp->SrcPort = req.DstPort; // Port 53
            udp->DstPort = req.SrcPort; // Client's ephemeral port
            udp->Length = BinaryPrimitives.ReverseEndianness((ushort)(8 + payloadLength));

            // -- Payload --
            new Span<byte>(responsePayload, 0, payloadLength).CopyTo(new Span<byte>(pBuffer + 28, payloadLength));

            // -- Injection --
            // Re-inject using the original interface metadata.
            WinDivertAddress addr = req.OriginalAddressStruct;
            addr.SetOutbound(false); // Mark as an INBOUND packet so the OS accepts it.

            // Recalculate checksums because we built a fresh packet.
            NativeMethods.WinDivertHelperCalcChecksums(pBuffer, (uint)totalLen, ref addr, 0);

            injector.Inject(pBuffer, (uint)totalLen, ref addr);
        }
    }
}