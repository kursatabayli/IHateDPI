using IHateDPI.Engine.Abstractions;
using IHateDPI.Engine.Helpers;
using IHateDPI.Engine.Models;
using IHateDPI.Engine.Native;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace IHateDPI.Engine.Processors;

/// <summary>
/// A packet processor responsible for handling TCP traffic and implementing various DPI evasion techniques.
/// <para>
/// Key features include TCP Window Clamping to prevent server-side data buffering, 
/// HTTP Host header manipulation to bypass string-based filters, 
/// and HTTPS fragmentation to split the TLS ClientHello packet.
/// </para>
/// </summary>
public sealed class TcpPacketProcessor(EngineConfig config, ITtlTracker ttlTracker) : IPacketProcessor
{
    private static ReadOnlySpan<byte> HostHeaderSignature => "Host: "u8;

    private static readonly byte[][] HttpMethods =
    [
        "GET "u8.ToArray(),
        "HEAD "u8.ToArray(),
        "POST "u8.ToArray(),
        "PUT "u8.ToArray(),
        "DELETE "u8.ToArray(),
        "CONNECT "u8.ToArray(),
        "OPTIONS "u8.ToArray(),
        "TRACE "u8.ToArray(),
        "PATCH "u8.ToArray()
    ];

    /// <inheritdoc />
    public unsafe bool Process(PacketContext ctx, ref uint newPacketLen, out bool shouldDrop)
    {
        shouldDrop = false;

        // Process only IPv4, Outbound, and valid TCP packets.
        if (ctx.IsIpv6 || !ctx.IsOutbound || ctx.TcpHdr == null)
            return false;

        var tcpHdr = ctx.TcpHdr;
        ushort dstPort = BinaryPrimitives.ReverseEndianness(tcpHdr->DstPort);

        // Window Clamping:
        // Modify the Window Size in SYN packets to force the server to send smaller packets.
        if (tcpHdr->Syn)
            return ProcessTcpHandshake(tcpHdr);

        if (ctx.PayloadLen <= 0)
            return false;

        // HTTPS Manipulation (Port 443):
        // Detect TLS ClientHello packets and apply fragmentation strategies.
        if (dstPort == 443)
        {
            var payload = ctx.PayloadSpan;

            // TLS Record (0x16) + Handshake (0x01)
            if (ctx.PayloadLen > 6 && payload[0] == 0x16 && payload[5] == 0x01)
            {
                return ProcessHttps(ctx, ref newPacketLen);
            }
        }
        // HTTP Manipulation (Port 80):
        // Attempt to bypass case-sensitive string filters by modifying the "Host" header.
        else if (dstPort == 80)
        {
            return ProcessHttp(ctx, ref newPacketLen);
        }

        return false;
    }

    /// <summary>
    /// Applies TCP Window Clamping to SYN packets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe bool ProcessTcpHandshake(TCPHdr* tcpHdr)
    {
        // Limits the amount of data the server can send at once (e.g., 1200 bytes).
        // This complicates reassembly for DPI systems.
        tcpHdr->Window = BinaryPrimitives.ReverseEndianness((ushort)config.MaxPayloadSize);
        return true;
    }

    /// <summary>
    /// Applies HTTP Host header manipulations (Port 80) based on the configuration.
    /// </summary>
    private unsafe bool ProcessHttp(PacketContext ctx, ref uint packetLen)
    {
        bool isHttp = false;
        var payload = ctx.PayloadSpan;

        foreach (var method in HttpMethods)
        {
            if (payload.StartsWith(method))
            {
                isHttp = true;
                break;
            }
        }

        bool modified = false;

        if (config.MixHost || config.HostNoSpace || config.AdditionalSpace)
            if (ApplyHostManipulation(ref ctx, ref packetLen))
                modified = true;

        if (isHttp && config.FragmentHttp > 0)
            if (ApplyFragmentation(ctx, ref packetLen, config.FragmentHttp))
                return true;

        return modified;
    }

    private unsafe bool ApplyHostManipulation(ref PacketContext ctx, ref uint packetLen)
    {
        var payload = ctx.PayloadSpan;
        int index = payload.IndexOf(HostHeaderSignature);
        if (index < 0) return false;

        bool modified = false;

        // "Host:example.com" (Remove space)
        if (config.HostNoSpace)
        {
            int spaceIndex = index + 5;
            int bytesToMove = (int)ctx.PayloadLen - (spaceIndex + 1);
            if (bytesToMove > 0)
            {
                payload.Slice(spaceIndex + 1, bytesToMove).CopyTo(payload[spaceIndex..]);
                DecreasePacketSize(ref ctx, ref packetLen, 1);
                payload = ctx.PayloadSpan; // Re-fetch span after resize
                modified = true;
            }
        }

        // "hOsT: example.com" (Mixed Case)
        if (config.MixHost)
        {
            payload[index] = (byte)'h';
            modified = true;
        }

        // "Host: example.com\t" (Additional Space/Tab)
        if (config.AdditionalSpace && ctx.PacketLen + 1 <= 65535)
        {
            int insertIndex = index + 5;
            int bytesToMove = (int)ctx.PayloadLen - insertIndex;
            if (bytesToMove > 0)
            {
                IncreasePacketSize(ref ctx, ref packetLen, 1);
                payload = ctx.PayloadSpan; // Re-fetch span after resize
                payload.Slice(insertIndex, bytesToMove).CopyTo(payload[(insertIndex + 1)..]);
                payload[insertIndex] = 0x09; // Tab character
                modified = true;
            }
        }

        return modified;
    }

    /// <summary>
    /// Decreases the packet total length by the specified amount and updates the IP header.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void DecreasePacketSize(ref PacketContext ctx, ref uint packetLen, int amount)
    {
        ctx.PacketLen -= amount;
        ctx.PayloadLen -= (uint)amount;
        packetLen -= (uint)amount;

        ushort currentLen = BinaryPrimitives.ReverseEndianness(ctx.IpHdr->Length);
        ctx.IpHdr->Length = BinaryPrimitives.ReverseEndianness((ushort)(currentLen - amount));
    }

    /// <summary>
    /// Increases the packet total length by the specified amount and updates the IP header.
    /// </summary>
    private unsafe void IncreasePacketSize(ref PacketContext ctx, ref uint packetLen, int amount)
    {
        ctx.PacketLen += amount;
        ctx.PayloadLen += (uint)amount;
        packetLen += (uint)amount;

        ushort currentLen = BinaryPrimitives.ReverseEndianness(ctx.IpHdr->Length);
        ctx.IpHdr->Length = BinaryPrimitives.ReverseEndianness((ushort)(currentLen + amount));
    }

    /// <summary>
    /// Processes HTTPS (Port 443) packets, handling TLS ClientHello detection and fragmentation.
    /// </summary>
    private unsafe bool ProcessHttps(PacketContext ctx, ref uint newPacketLen)
    {
        var payload = ctx.PayloadSpan;

        // Basic Validation: Ensure it's a TLS Handshake (0x16) and ClientHello (0x01)
        if (ctx.PayloadLen < 6 || payload[0] != 0x16 || payload[5] != 0x01)
        {
            return false;
        }

        // Legacy: Send a fake packet to trigger DPI filters early (if configured).
        if (config.FakePacketTtl > 0 || config.BadCheckSum || config.BadSequence)
        {
            // Note: Ideally, SNI should be parsed here. 
            // Using a generic fake payload is usually sufficient to prime the DPI state.
            SendFakePacket(ctx);
        }

        // Determine Fragmentation Point
        int splitPosition = 0;

        if (config.AutoSplitSni)
        {
            // Smart Splitting: Try to split exactly in the middle of the SNI extension.
            if (TlsUtils.TryGetSniRange(payload, out int sniOffset, out int sniLen))
            {
                splitPosition = sniOffset + (sniLen / 2);
            }
            else
            {
                splitPosition = 1; // Fallback: Split after first byte
            }
        }
        else if (config.FragmentHttps > 0)
        {
            splitPosition = config.FragmentHttps;
        }

        if (splitPosition > 0 && splitPosition < ctx.PayloadLen)
        {
            return ApplyFragmentation(ctx, ref newPacketLen, splitPosition);
        }

        return false;
    }

    /// <summary>
    /// Splits the packet into two fragments based on the configuration strategy (Normal or Reverse).
    /// </summary>
    private unsafe bool ApplyFragmentation(PacketContext ctx, ref uint packetLen, int splitSize)
    {
        int payloadLen = (int)ctx.PayloadLen;
        if (payloadLen <= splitSize) return false;

        // --- SCENARIO A: REVERSE FRAGMENTATION (Part 2 sent before Part 1) ---
        // Highly effective against stateful DPI systems expecting sequential arrival.
        if (config.ReverseFragmentation)
        {
            if (config.BufferPoisoning)
            {
                InjectJunkPacket(ctx, splitSize);
            }

            // 1. Inject Part 2 immediately (Offset: splitSize -> End)
            // Seq: Original + splitSize
            InjectFragment(ctx, offset: splitSize, length: payloadLen - splitSize, seqAdjustment: splitSize);

            // 2. Truncate current packet to be Part 1 (Offset: 0 -> splitSize)
            TruncatePacket(ref ctx, ref packetLen, length: splitSize);
        }
        // --- SCENARIO B: NORMAL FRAGMENTATION (Part 1 sent before Part 2) ---
        else
        {
            // 1. Inject Part 1 immediately (Offset: 0 -> splitSize)
            // Seq: Unchanged
            InjectFragment(ctx, offset: 0, length: splitSize, seqAdjustment: 0);

            if (config.BufferPoisoning)
            {
                InjectJunkPacket(ctx, splitSize);
            }

            // 2. Shift current packet to be Part 2 (Offset: splitSize -> End)
            ShiftPacketPayload(ref ctx, ref packetLen, splitSize: splitSize);
        }

        return true;
    }

    /// <summary>
    /// Injects random "Junk" packets starting exactly from the split offset to pollute the DPI stream.
    /// <para>
    /// This effectively creates a continuous stream of garbage data where the DPI expects the real packet.
    /// Techniques include Desynchronization (shifting Seq/Ack) and Overlap (overwriting expected Seq).
    /// </para>
    /// </summary>
    [SkipLocalsInit]
    private unsafe void InjectJunkPacket(PacketContext ctx, int seqStartOffset)
    {
        int packetCount = config.JunkPacketCount > 0 ? config.JunkPacketCount : 1;
        int junkSize = config.JunkPacketSize;

        // Calculate Header and Total Sizes
        int headerLen = ctx.IpHdr->HdrLength + ctx.TcpHdr->HeaderLength;
        int totalLen = headerLen + junkSize;

        // Allocate buffer on stack for performance (avoid GC allocation)
        Span<byte> junkBuffer = stackalloc byte[totalLen];

        // 1. Copy original headers as a template
        ctx.FullPacketSpan[..headerLen].CopyTo(junkBuffer);

        fixed (byte* pBuffer = junkBuffer)
        {
            IPHdr* ip = (IPHdr*)pBuffer;
            TCPHdr* tcp = (TCPHdr*)(pBuffer + ctx.IpHdr->HdrLength);

            // 2. Update IP Length
            ip->Length = BinaryPrimitives.ReverseEndianness((ushort)totalLen);

            // 3. TTL Configuration
            // For Desync, we usually want the packet to reach the DPI but not the server (TTL manipulation).
            // For Overlap, we want it to die right after the DPI.
            if (config.JunkPacketTtl > 0)
            {
                int autoTtl = ttlTracker.GetCalculatedTtl(
                    ctx.IpHdr->DstAddr, ctx.IpHdr->SrcAddr, ctx.TcpHdr->DstPort, ctx.TcpHdr->SrcPort, config.JunkPacketTtl);
                ip->TTL = autoTtl > 0 ? (byte)autoTtl : (byte)config.JunkPacketTtl;
            }
            else
            {
                ip->TTL = 64; // Default: Let it travel towards the server.
            }

            // --- SEQUENCE CALCULATIONS ---

            uint baseSeq = BinaryPrimitives.ReverseEndianness(ctx.TcpHdr->SeqNum);
            uint baseAck = BinaryPrimitives.ReverseEndianness(ctx.TcpHdr->AckNum);

            // The injection starts where the "Real Data" (Part 2) is expected to begin.
            uint injectionStartSeq = baseSeq + (uint)seqStartOffset;

            // Access payload area
            Span<byte> payloadSlice = junkBuffer[headerLen..];

            // 4. Transmission Loop
            for (int i = 0; i < packetCount; i++)
            {
                // A) Payload: Pure Random Noise
                Random.Shared.NextBytes(payloadSlice);

                // B) Sequence Logic
                // Increment sequence for each junk packet so DPI sees them as a contiguous stream.
                uint currentStreamSeq = injectionStartSeq + (uint)(i * junkSize);

                if (config.JunkPacketBadSequence)
                {
                    // DESYNC STRATEGY:
                    // Shift Seq/Ack significantly to poison the DPI state machine without affecting the real connection.
                    // Server will likely drop this due to being out of window.
                    tcp->SeqNum = BinaryPrimitives.ReverseEndianness(currentStreamSeq - 10000);
                    tcp->AckNum = BinaryPrimitives.ReverseEndianness(baseAck - 66000);
                }
                else
                {
                    // OVERLAP STRATEGY:
                    // Write garbage data exactly where the real data belongs.
                    // Relies on TTL or Checksum to ensure the server rejects this, while DPI processes it.
                    tcp->SeqNum = BinaryPrimitives.ReverseEndianness(currentStreamSeq);
                    tcp->AckNum = BinaryPrimitives.ReverseEndianness(baseAck);
                }

                // C) Checksum Calculation
                // Must be recalculated for every packet since payload/seq changes.
                tcp->Checksum = 0;
                NativeMethods.WinDivertHelperCalcChecksums(pBuffer, (uint)totalLen, ref ctx.AddressRef, 0);

                // Optional: Corrupt Checksum to ensure server rejection
                if (config.JunkPacketBadChecksum)
                {
                    ushort correctChecksum = BinaryPrimitives.ReverseEndianness(tcp->Checksum);
                    tcp->Checksum = BinaryPrimitives.ReverseEndianness((ushort)(correctChecksum ^ 0xFFFF));
                }

                // D) Send
                NativeMethods.WinDivertSend(ctx.Handle, pBuffer, (uint)totalLen, out _, ref ctx.AddressRef);
            }
        }
    }

    /// <summary>
    /// Constructs and injects a "Fake Request" packet to confuse the DPI system.
    /// <para>
    /// Uses techniques like low TTL, bad checksum, or invalid sequence numbers to ensure 
    /// the packet reaches the DPI inspector but is discarded by the destination.
    /// </para>
    /// </summary>
    [SkipLocalsInit]
    private unsafe void SendFakePacket(PacketContext ctx)
    {
        int headerLen = ctx.IpHdr->HdrLength + ctx.TcpHdr->HeaderLength;

        Span<byte> fakeBuffer = stackalloc byte[2048];

        // Copy original headers
        ctx.FullPacketSpan[..headerLen].CopyTo(fakeBuffer);

        // Generate fake TLS ClientHello
        if (!TlsPacketBuilder.TryWriteFakeClientHello("www.google.com", fakeBuffer[headerLen..], out int fakePayloadLen))
        {
            return;
        }

        int fakePacketSize = headerLen + fakePayloadLen;

        fixed (byte* pFake = fakeBuffer)
        {
            IPHdr* fakeIp = (IPHdr*)pFake;
            TCPHdr* fakeTcp = (TCPHdr*)(pFake + ctx.IpHdr->HdrLength);

            fakeIp->Length = BinaryPrimitives.ReverseEndianness((ushort)fakePacketSize);

            // --- Apply Evasion Techniques ---

            // 1. TTL Manipulation
            if (config.FakePacketTtl > 0)
            {
                int autoTtl = ttlTracker.GetCalculatedTtl(
                    ctx.IpHdr->DstAddr, ctx.IpHdr->SrcAddr, ctx.TcpHdr->DstPort, ctx.TcpHdr->SrcPort, config.FakePacketTtl);
                fakeIp->TTL = autoTtl > 0 ? (byte)autoTtl : (byte)config.FakePacketTtl;
            }
            else
            {
                fakeIp->TTL = 64;
            }

            // 2. Bad Sequence (Desynchronization)
            if (config.BadSequence)
            {
                uint currentAck = BinaryPrimitives.ReverseEndianness(fakeTcp->AckNum);
                uint currentSeq = BinaryPrimitives.ReverseEndianness(fakeTcp->SeqNum);

                fakeTcp->AckNum = BinaryPrimitives.ReverseEndianness(currentAck - 66000);
                fakeTcp->SeqNum = BinaryPrimitives.ReverseEndianness(currentSeq - 10000);
            }

            // Calculate correct checksum first
            NativeMethods.WinDivertHelperCalcChecksums(pFake, (uint)fakePacketSize, ref ctx.AddressRef, 0);

            // 3. Bad Checksum
            if (config.BadCheckSum)
            {
                ushort correctChecksum = BinaryPrimitives.ReverseEndianness(fakeTcp->Checksum);
                fakeTcp->Checksum = BinaryPrimitives.ReverseEndianness((ushort)(correctChecksum - 1));
            }

            // Resend loop
            for (int i = 0; i < config.FakeRequestResendCount; i++)
            {
                NativeMethods.WinDivertSend(ctx.Handle, pFake, (uint)fakePacketSize, out _, ref ctx.AddressRef);
            }
        }
    }

    /// <summary>
    /// Creates a new packet fragment from the original payload and injects it into the network.
    /// </summary>
    /// <param name="offset">Start index in the payload.</param>
    /// <param name="length">Number of bytes to copy.</param>
    /// <param name="seqAdjustment">Amount to increase the TCP Sequence Number by.</param>
    [SkipLocalsInit]
    private unsafe void InjectFragment(PacketContext ctx, int offset, int length, int seqAdjustment)
    {
        var ipHeader = ctx.IpHdr;
        var tcpHeader = ctx.TcpHdr;
        int headerLen = ipHeader->HdrLength + tcpHeader->HeaderLength;
        int totalPacketLen = headerLen + length;

        // Stackalloc limit check (safe guard)
        if (totalPacketLen > 2048) return;

        Span<byte> buffer = stackalloc byte[totalPacketLen];

        // 1. Copy Headers
        ctx.FullPacketSpan[..headerLen].CopyTo(buffer);

        // 2. Copy Payload Segment
        ctx.PayloadSpan.Slice(offset, length).CopyTo(buffer[headerLen..]);

        fixed (byte* pBuffer = buffer)
        {
            IPHdr* newIp = (IPHdr*)pBuffer;
            TCPHdr* newTcp = (TCPHdr*)(pBuffer + ipHeader->HdrLength);

            // Update IP Length
            newIp->Length = BinaryPrimitives.ReverseEndianness((ushort)totalPacketLen);

            // Adjust Sequence Number
            if (seqAdjustment > 0)
            {
                uint currentSeq = BinaryPrimitives.ReverseEndianness(newTcp->SeqNum);
                newTcp->SeqNum = BinaryPrimitives.ReverseEndianness(currentSeq + (uint)seqAdjustment);
            }

            // Recalculate Checksums & Send
            NativeMethods.WinDivertHelperCalcChecksums(pBuffer, (uint)totalPacketLen, ref ctx.AddressRef, 0);
            NativeMethods.WinDivertSend(ctx.Handle, pBuffer, (uint)totalPacketLen, out _, ref ctx.AddressRef);
        }
    }

    /// <summary>
    /// Truncates the current packet (Part 1 in Reverse Fragmentation).
    /// </summary>
    private unsafe void TruncatePacket(ref PacketContext ctx, ref uint packetLen, int length)
    {
        var ipHeader = ctx.IpHdr;
        var tcpHeader = ctx.TcpHdr;
        int headerLen = ipHeader->HdrLength + tcpHeader->HeaderLength;

        // Update length to Headers + New Payload Length
        packetLen = (uint)(headerLen + length);
        ipHeader->Length = BinaryPrimitives.ReverseEndianness((ushort)packetLen);

        ctx.PacketLen = (int)packetLen;
        ctx.PayloadLen = (uint)length;
        // Sequence number remains unchanged for Part 1.
    }

    /// <summary>
    /// Shifts the payload to the beginning (Part 2 in Normal Fragmentation).
    /// </summary>
    private unsafe void ShiftPacketPayload(ref PacketContext ctx, ref uint packetLen, int splitSize)
    {
        var ipHeader = ctx.IpHdr;
        var tcpHeader = ctx.TcpHdr;
        var payload = ctx.PayloadSpan;

        // Shift data: Move [splitSize..end] to [0..newLength]
        payload[splitSize..].CopyTo(payload);

        // Reduce packet size
        packetLen -= (uint)splitSize;
        ipHeader->Length = BinaryPrimitives.ReverseEndianness((ushort)packetLen);

        // Increment Sequence Number for Part 2
        uint currentSeq = BinaryPrimitives.ReverseEndianness(tcpHeader->SeqNum);
        tcpHeader->SeqNum = BinaryPrimitives.ReverseEndianness(currentSeq + (uint)splitSize);

        ctx.PacketLen -= splitSize;
        ctx.PayloadLen -= (uint)splitSize;
    }
}