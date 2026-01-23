using IHateDPI.Engine.Abstractions;
using IHateDPI.Engine.Models;
using IHateDPI.Engine.Native;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace IHateDPI.Engine.Processors;

/// <summary>
/// Handles UDP traffic inspection and management.
/// <para>
/// Responsible for blocking QUIC (HTTP/3) to force TCP fallback and intercepting standard DNS queries (Port 53)
/// for redirection to the encrypted DoH service.
/// </para>
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UdpPacketProcessor"/> class.
/// </remarks>
/// <param name="config">The engine configuration.</param>
/// <param name="dnsWriter">The channel writer used to offload captured DNS requests for asynchronous processing.</param>
public sealed class UdpPacketProcessor(EngineConfig config, ChannelWriter<DnsRequestSnapshot> dnsWriter) : IPacketProcessor
{

    /// <inheritdoc />
    public unsafe bool Process(PacketContext ctx, ref uint newPacketLen, out bool shouldDrop)
    {
        shouldDrop = false;

        // Basic Validation: IPv4, Outbound, and valid UDP Header.
        if (ctx.IsIpv6 || !ctx.IsOutbound || ctx.UdpHdr == null)
            return false;

        ushort dstPort = BinaryPrimitives.ReverseEndianness(ctx.UdpHdr->DstPort);

        // 1. QUIC Blocking (Port 443 / UDP)
        if (dstPort == 443)
        {
            return ProcessQuic(ref ctx, out shouldDrop);
        }

        // 2. DNS Interception (Port 53 / UDP)
        else if (dstPort == 53)
        {
            return ProcessDns(ref ctx, out shouldDrop);
        }

        return false;
    }

    /// <summary>
    /// Processes QUIC (HTTP/3) traffic and optionally blocks it based on configuration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ProcessQuic(ref PacketContext ctx, out bool shouldDrop)
    {
        shouldDrop = false;

        // If BlockQuic is disabled, allow the packet to pass.
        if (!config.BlockQuic)
            return false;

        // Blocking Logic: Send an ICMP "Port Unreachable" message to the source.
        // This explicitly tells the browser/client that UDP 443 is closed, forcing an immediate fallback to TCP/TLS (HTTP/2).
        SendIcmpPortUnreachable(ref ctx);

        shouldDrop = true; // Drop the original UDP packet.
        return false;      // No modification to the original packet, just a drop.
    }

    /// <summary>
    /// Intercepts standard DNS (Port 53) queries and redirects them to the DoH service if enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe bool ProcessDns(ref PacketContext ctx, out bool shouldDrop)
    {
        shouldDrop = false;

        // If DoH is disabled or the payload is empty, ignore.
        if (!config.IsDohEnabled || ctx.PayloadLen <= 0)
            return false;

        // Rent a buffer from the shared pool to avoid allocations during hot-path processing.
        byte[] pooledBuffer = ArrayPool<byte>.Shared.Rent((int)ctx.PayloadLen);
        ctx.PayloadSpan.CopyTo(pooledBuffer);

        // Copy the WinDivertAddress struct because it's required later for response injection.
        var addressCopy = *ctx.Address;

        var snapshot = new DnsRequestSnapshot(
            ctx.IpHdr->SrcAddr,
            ctx.IpHdr->DstAddr,
            ctx.UdpHdr->SrcPort,
            ctx.UdpHdr->DstPort,
            pooledBuffer,
            (int)ctx.PayloadLen,
            addressCopy
        );

        // Offload the snapshot to the asynchronous channel.
        // TryWrite is used to be non-blocking.
        if (dnsWriter.TryWrite(snapshot))
        {
            shouldDrop = true; // Block the original UDP packet (it will be handled via DoH).
        }
        else
        {
            // Fail-Open Logic:
            // If the channel is full (backpressure), do not block the DNS query.
            // Let it pass through unencrypted to prevent internet connectivity loss.
            ArrayPool<byte>.Shared.Return(pooledBuffer); // Return the unused buffer.
            shouldDrop = false;
        }

        return false;
    }

    /// <summary>
    /// Constructs and injects an ICMP Destination Unreachable (Type 3, Code 3 - Port Unreachable) message back to the source.
    /// </summary>
    /// <param name="ctx">The context of the packet triggering the error.</param>
    [SkipLocalsInit]
    private unsafe void SendIcmpPortUnreachable(ref PacketContext ctx)
    {
        int originalIpHeaderLen = ctx.IpHdr->HdrLength;

        // According to RFC 792, the ICMP error message must contain the IP header and the first 8 bytes of the original datagram's data.
        int dataToCopyLen = originalIpHeaderLen + 8;
        int newPacketLen = 20 + 8 + dataToCopyLen; // IP Header (20) + ICMP Header (8) + Original Data

        // Use stack allocation for speed.
        Span<byte> icmpPacketSpan = stackalloc byte[newPacketLen];

        fixed (byte* pIcmpPacket = icmpPacketSpan)
        {
            // -- Construct IP Header --
            IPHdr* newIp = (IPHdr*)pIcmpPacket;
            newIp->HdrLengthAndVersion = 0x45;
            newIp->Length = BinaryPrimitives.ReverseEndianness((ushort)newPacketLen);
            newIp->TTL = 64;
            newIp->Protocol = 1; // ICMP
            newIp->SrcAddr = ctx.IpHdr->DstAddr; // Swap Source/Dest: Error comes FROM the destination.
            newIp->DstAddr = ctx.IpHdr->SrcAddr;

            // -- Construct ICMP Header --
            ICMPHdr* icmp = (ICMPHdr*)(pIcmpPacket + 20);
            icmp->Type = 3; // Destination Unreachable
            icmp->Code = 3; // Port Unreachable

            // -- Copy Original Data --
            byte* pPayloadArea = (byte*)(icmp + 1);
            ctx.FullPacketSpan[..dataToCopyLen].CopyTo(new Span<byte>(pPayloadArea, dataToCopyLen));

            // -- Injection --
            // Create a temporary address struct for injection to avoid modifying the original context.
            WinDivertAddress tempAddr = *ctx.Address;

            tempAddr.Flags = 0;
            tempAddr.SetOutbound(false); // Inject as an INBOUND packet (simulating a response from the network).

            NativeMethods.WinDivertHelperCalcChecksums(pIcmpPacket, (uint)newPacketLen, ref tempAddr, 0);
            NativeMethods.WinDivertSend(ctx.Handle, pIcmpPacket, (uint)newPacketLen, out _, ref tempAddr);
        }
    }
}