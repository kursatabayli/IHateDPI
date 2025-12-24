using IHateDPI.Engine.Abstractions;
using IHateDPI.Engine.Helpers;
using IHateDPI.Engine.Models;
using IHateDPI.Engine.Native;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        // This makes it harder for DPI systems to reassemble and analyze the stream.
        if (tcpHdr->Syn)
            return ProcessTcpHandshake(tcpHdr);

        if (ctx.PayloadLen <= 0)
            return false;


        // HTTPS Manipulation (Port 443):
        // Detect TLS ClientHello packets (0x16 = Handshake, 0x01 = ClientHello) and apply fragmentation.
        if (dstPort == 443)
        {
            var payload = ctx.PayloadSpan;

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
        // Limits the amount of data the server can send at once.
        // Safe values are typically between 1200-1500 bytes.
        tcpHdr->Window = BinaryPrimitives.ReverseEndianness((ushort)config.MaxPayloadSize);
        return true;
    }

    /// <summary>
    /// Applies HTTP Host header manipulations (Port 80) based on the configuration.
    /// </summary>
    private unsafe bool ProcessHttp(PacketContext ctx, ref uint packetLen)
    {
        // Early exit if no manipulation is enabled.
        if (!config.MixHost && !config.HostNoSpace && !config.AdditionalSpace)
            return false;

        var payload = ctx.PayloadSpan;

        // Search for the "Host: " signature (Length: 6 bytes).
        int index = payload.IndexOf(HostHeaderSignature);

        // If not found, do nothing.
        if (index < 0) return false;

        bool modified = false;

        // 1. HostNoSpace: "Host: " -> "Host:" (Remove space)
        if (config.HostNoSpace)
        {
            // The space character is at index 5 relative to the start of "Host: ".
            int spaceIndex = index + 5;

            // Calculate length of data to move.
            int bytesToMove = (int)ctx.PayloadLen - (spaceIndex + 1);

            if (bytesToMove > 0)
            {
                // Shift memory 1 byte LEFT to overwrite the space.
                // Source: After space -> Destination: At space
                payload.Slice(spaceIndex + 1, bytesToMove).CopyTo(payload[spaceIndex..]);

                // Decrease packet size (TCP Payload shrunk by 1 byte).
                DecreasePacketSize(ref ctx, ref packetLen, 1);

                // Update payload reference as the length has changed.
                payload = ctx.PayloadSpan;
                modified = true;
            }
        }

        // 2. MixHost: "Host:" -> "host:" (Case randomization)
        if (config.MixHost)
        {
            // Change 'H' to 'h'.
            // Note: Even if HostNoSpace ran, 'index' still points to the start of "Host".
            payload[index] = (byte)'h';
            modified = true;
        }

        // 3. AdditionalSpace: "Host:" -> "Host:  " (Append TAB/Space)
        if (config.AdditionalSpace)
        {
            // Character to add (e.g., TAB - 0x09).
            byte charToAdd = 0x09;

            // Insertion point: After "Host:".
            // If HostNoSpace ran, it's at index+5. If not, it's normally at index+6.
            // For simplicity, find the ':' and insert after it.
            int insertIndex = index + 5;

            // Buffer overflow check (WinDivert MTU is usually large enough, but safety first).
            if (ctx.PacketLen + 1 <= 65535)
            {
                int bytesToMove = (int)ctx.PayloadLen - insertIndex;

                if (bytesToMove > 0)
                {
                    // First, increase the packet size.
                    IncreasePacketSize(ref ctx, ref packetLen, 1);

                    // Refresh payload span.
                    payload = ctx.PayloadSpan;

                    // Shift memory 1 byte RIGHT to make space.
                    // Span.CopyTo handles overlapping memory safely.
                    payload.Slice(insertIndex, bytesToMove).CopyTo(payload[(insertIndex + 1)..]);

                    // Write the character into the newly created gap.
                    payload[insertIndex] = charToAdd;

                    modified = true;
                }
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

        // TLS ClientHello Validation:
        // Byte 0: Content Type (0x16 = Handshake)
        // Byte 1-2: Version (TLS 1.0/1.2/1.3)
        // Byte 5: Handshake Type (0x01 = ClientHello)
        if (ctx.PayloadLen < 6 || payload[0] != 0x16 || payload[5] != 0x01)
        {
            return false;
        }

        // Send a fake packet to fool the DPI state machine if configured.
        if (config.FakePacketTTL > 0 || config.BadCheckSum || config.BadSequence)
        {
            // Note: Ideally, we should parse the SNI here. 
            // For now, using a default/random domain is usually sufficient to trigger DPI processing.
            SendFakePacket(ctx, "www.google.com");
        }

        // Fragmentation
        if (config.FragmentHttps > 0)
        {
            return ApplyFragmentation(ctx, ref newPacketLen, config.FragmentHttps);
        }

        return false;
    }

    /// <summary>
    /// Splits the packet into two fragments based on the configuration strategy (Normal or Reverse).
    /// </summary>
    private unsafe bool ApplyFragmentation(PacketContext ctx, ref uint packetLen, int splitSize)
    {
        int payloadLen = (int)ctx.PayloadLen;

        // If the payload is too small to split, skip.
        if (payloadLen <= splitSize) return false;

        // --- SCENARIO A: REVERSE FRAGMENTATION (2 -> 1) ---
        // Highly effective against stateful DPI.
        if (config.ReverseFragmentation)
        {
            // 1. Inject the SECOND part immediately.
            // Data: From splitSize to end.
            // Seq: Original Seq + splitSize.
            InjectFragment(ctx, offset: splitSize, length: payloadLen - splitSize, seqAdjustment: splitSize);

            // 2. Modify the current packet to become the FIRST part.
            // Just truncate the length; no data movement needed.
            TruncatePacket(ref ctx, ref packetLen, length: splitSize);
        }
        // --- SCENARIO B: NORMAL FRAGMENTATION (1 -> 2) ---
        else
        {
            // 1. Inject the FIRST part immediately.
            // Data: From 0 to splitSize.
            // Seq: Unchanged.
            InjectFragment(ctx, offset: 0, length: splitSize, seqAdjustment: 0);

            // 2. Modify the current packet to become the SECOND part.
            // Shift data to the left (beginning).
            ShiftPacketPayload(ref ctx, ref packetLen, splitSize: splitSize);
        }

        return true;
    }

    /// <summary>
    /// Constructs and injects a "Fake Request" packet to confuse the DPI system.
    /// <para>
    /// The fake packet uses techniques like low TTL, bad checksum, or invalid sequence numbers 
    /// to ensure it reaches the DPI inspector but is discarded by the destination server or intermediate routers.
    /// </para>
    /// </summary>
    [SkipLocalsInit]
    private unsafe void SendFakePacket(PacketContext ctx, string sniDomain)
    {
        int headerLen = ctx.IpHdr->HdrLength + ctx.TcpHdr->HeaderLength;

        Span<byte> fakeBuffer = stackalloc byte[2048];

        // Copy original headers
        ctx.FullPacketSpan[..headerLen].CopyTo(fakeBuffer);

        // Generate a fake TLS ClientHello payload
        if (!TlsPacketBuilder.TryWriteFakeClientHello(sniDomain, fakeBuffer[headerLen..], out int fakePayloadLen))
        {
            // Abort if buffer is insufficient (security safety).
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
            if (config.FakePacketTTL > 0)
            {
                int autoTtl = ttlTracker.GetCalculatedTtl(
                    ctx.IpHdr->DstAddr,
                    ctx.IpHdr->SrcAddr,
                    ctx.TcpHdr->DstPort,
                    ctx.TcpHdr->SrcPort,
                    config.FakePacketTTL
                );
                fakeIp->TTL = autoTtl > 0 ? (byte)autoTtl : (byte)config.FakePacketTTL;
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

            // 3. Bad Checksum (Invalidate packet on receiving end)
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
    /// <param name="offset">The start index in the payload to copy from.</param>
    /// <param name="length">The number of bytes to copy.</param>
    /// <param name="seqAdjustment">The amount to increase the TCP Sequence Number by.</param>
    [SkipLocalsInit]
    private unsafe void InjectFragment(PacketContext ctx, int offset, int length, int seqAdjustment)
    {
        var ipHeader = ctx.IpHdr;
        var tcpHeader = ctx.TcpHdr;
        int headerLen = ipHeader->HdrLength + tcpHeader->HeaderLength;
        int totalPacketLen = headerLen + length;

        // Use stackalloc for fast, zero-allocation memory (limit 2KB).
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

            // Recalculate Checksums & Inject
            NativeMethods.WinDivertHelperCalcChecksums(pBuffer, (uint)totalPacketLen, ref ctx.AddressRef, 0);
            NativeMethods.WinDivertSend(ctx.Handle, pBuffer, (uint)totalPacketLen, out _, ref ctx.AddressRef);
        }
    }

    /// <summary>
    /// Truncates the current packet by removing data from the end. Used for Part 1 in Reverse Fragmentation.
    /// </summary>
    private unsafe void TruncatePacket(ref PacketContext ctx, ref uint packetLen, int length)
    {
        var ipHeader = ctx.IpHdr;
        var tcpHeader = ctx.TcpHdr;
        int headerLen = ipHeader->HdrLength + tcpHeader->HeaderLength;

        // Set total length to encompass headers + new truncated payload length.
        packetLen = (uint)(headerLen + length);

        // Update IP header length.
        ipHeader->Length = BinaryPrimitives.ReverseEndianness((ushort)packetLen);

        // Note: Sequence number remains unchanged.
    }

    /// <summary>
    /// Removes data from the beginning of the packet and shifts the remaining data to the front. Used for Part 2 in Normal Fragmentation.
    /// </summary>
    private unsafe void ShiftPacketPayload(ref PacketContext ctx, ref uint packetLen, int splitSize)
    {
        var ipHeader = ctx.IpHdr;
        var tcpHeader = ctx.TcpHdr;
        var payload = ctx.PayloadSpan;
        payload[splitSize..].CopyTo(payload);

        packetLen -= (uint)splitSize;
        ipHeader->Length = BinaryPrimitives.ReverseEndianness((ushort)packetLen);

        uint currentSeq = BinaryPrimitives.ReverseEndianness(tcpHeader->SeqNum);
        tcpHeader->SeqNum = BinaryPrimitives.ReverseEndianness(currentSeq + (uint)splitSize);
    }
}