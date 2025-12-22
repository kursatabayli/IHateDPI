using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace IHateDPI.Engine.Helpers;

/// <summary>
/// Provides helper methods for constructing raw TLS ClientHello packets used for "Fake Request" evasion techniques.
/// </summary>
public static class TlsPacketBuilder
{
    private static readonly ushort[] GreaseValues =
    [
        0x0A0A, 0x1A1A, 0x2A2A, 0x3A3A, 0x4A4A, 0x5A5A, 0x6A6A, 0x7A7A,
        0x8A8A, 0x9A9A, 0xAAAA, 0xBABA, 0xCACA, 0xDADA, 0xEAEA, 0xFAFA
    ];

    private static readonly ushort[] CommonCipherSuites =
    [
        0x1301, 0x1302, 0x1303, 0xC02B, 0xC02F, 0xC02C, 0xC030
    ];

    /// <summary>
    /// Constructs a randomized TLS ClientHello packet directly into the provided buffer without heap allocations.
    /// </summary>
    /// <param name="sniDomain">The target Service Name Indication (SNI) domain.</param>
    /// <param name="buffer">The destination buffer to write the packet to.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written to the buffer.</param>
    /// <returns><c>true</c> if the packet was successfully written; otherwise, <c>false</c> if the buffer was too small.</returns>
    public static bool TryWriteFakeClientHello(string sniDomain, Span<byte> buffer, out int bytesWritten)
    {
        bytesWritten = 0;

        // --- 1. PREPARATION (Stack Allocation) ---

        // Allocate stack memory for random values (Zero Heap Allocation)
        Span<byte> randomBytes = stackalloc byte[64];
        Random.Shared.NextBytes(randomBytes);

        // Prepare Cipher Suite list on the stack
        // Size: Common + 1 (Grease)
        Span<ushort> ciphers = stackalloc ushort[CommonCipherSuites.Length + 1];

        // Pick a Grease value and place it at the beginning
        ushort greaseVal = GreaseValues[Random.Shared.Next(GreaseValues.Length)];
        ciphers[0] = greaseVal;

        // Copy common suites
        CommonCipherSuites.CopyTo(ciphers[1..]);

        // Shuffle to make the fingerprint look random
        Shuffle(ciphers);

        // Domain length (ASCII is usually sufficient for SNI)
        int domainLen = Encoding.ASCII.GetByteCount(sniDomain);

        // Random Padding (10-100 bytes)
        int paddingLen = Random.Shared.Next(10, 100);

        // --- 2. LENGTH CALCULATION ---

        // Extension Lengths
        int extSniLen = 9 + domainLen;
        // ALPN: h2(3) + http/1.1(9) + headers(6) = 18
        int extAlpnLen = 18;
        // SupVer: Header(5) + Grease(2) + TLS1.3(2) + TLS1.2(2) = 11
        int extSupVerLen = 11;
        // KeyShare: Header(8) + Key(32) = 40
        int extKeyShareLen = 40;
        // SupGroups: Header(6) + Grease(2) + X25519(2) + Secp(2) = 12
        int extSupGroupsLen = 12;
        // Padding
        int extPadLen = 4 + paddingLen;

        int totalExtLen = extSniLen + extAlpnLen + extSupVerLen + extKeyShareLen + extSupGroupsLen + extPadLen;

        // Base Packet Size: 
        // RecHead(5) + HandHead(4) + Ver(2) + Rand(32) + SessID(33) + Ciphers(2 + Len*2) + Comp(2) + ExtLen(2)
        int baseHeadersSize = 5 + 4 + 2 + 32 + 33 + 2 + (ciphers.Length * 2) + 2 + 2;
        int totalPacketSize = baseHeadersSize + totalExtLen;

        // Buffer check
        if (buffer.Length < totalPacketSize)
            return false;

        // --- 3. WRITING PAYLOAD ---
        int offset = 0;

        // A. Record Layer
        buffer[offset++] = 0x16; // Content Type: Handshake
        buffer[offset++] = 0x03; // Version Major
        buffer[offset++] = 0x01; // Version Minor (TLS 1.0 for compatibility)
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(totalPacketSize - 5));
        offset += 2;

        // B. Handshake Header
        buffer[offset++] = 0x01; // Msg Type: ClientHello

        // Length (24-bit int) -> Need to write 3 bytes
        int handshakeLen = totalPacketSize - 9;
        buffer[offset++] = 0x00; // High byte (0 is safe as packets won't exceed 64KB)
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)handshakeLen);
        offset += 2;

        // Protocol Version (TLS 1.2 - 0x0303)
        buffer[offset++] = 0x03;
        buffer[offset++] = 0x03;

        // Random (32 bytes)
        randomBytes[..32].CopyTo(buffer[offset..]);
        offset += 32;

        // Session ID
        buffer[offset++] = 32; // ID Length
        randomBytes[32..].CopyTo(buffer[offset..]); // ID Random Data
        offset += 32;

        // Cipher Suites
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(ciphers.Length * 2));
        offset += 2;
        foreach (var c in ciphers)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], c);
            offset += 2;
        }

        // Compression Methods (01 00 - Null)
        buffer[offset++] = 0x01;
        buffer[offset++] = 0x00;

        // Extensions Length Total
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)totalExtLen);
        offset += 2;

        // --- EXTENSIONS ---

        // 1. SNI
        WriteExtensionHeader(buffer, ref offset, 0x0000, extSniLen - 4);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(extSniLen - 6)); // List Len
        offset += 2;
        buffer[offset++] = 0x00; // Type: HostName
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)domainLen);
        offset += 2;
        Encoding.ASCII.GetBytes(sniDomain, buffer[offset..]);
        offset += domainLen;

        // 2. ALPN
        WriteExtensionHeader(buffer, ref offset, 0x0010, extAlpnLen - 4);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(extAlpnLen - 6));
        offset += 2;
        // h2
        buffer[offset++] = 2; buffer[offset++] = (byte)'h'; buffer[offset++] = (byte)'2';
        // http/1.1
        buffer[offset++] = 8;
        Encoding.ASCII.GetBytes("http/1.1", buffer[offset..]);
        offset += 8;

        // 3. Supported Versions
        WriteExtensionHeader(buffer, ref offset, 0x002b, extSupVerLen - 4);
        buffer[offset++] = 6; // List Len
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], greaseVal); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 0x0304); offset += 2; // TLS 1.3
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 0x0303); offset += 2; // TLS 1.2

        // 4. Supported Groups
        WriteExtensionHeader(buffer, ref offset, 0x000a, extSupGroupsLen - 4);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(extSupGroupsLen - 6));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], greaseVal); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 0x001d); offset += 2; // X25519
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 0x0017); offset += 2; // Secp256r1

        // 5. Key Share
        WriteExtensionHeader(buffer, ref offset, 0x0033, extKeyShareLen - 4);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(extKeyShareLen - 6));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 0x001d); offset += 2; // Group
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 32); offset += 2; // Key Len
        randomBytes[..32].CopyTo(buffer[offset..]); // Fake Key
        offset += 32;

        // 6. Padding
        WriteExtensionHeader(buffer, ref offset, 0x0015, paddingLen);
        // No need to zero out padding area as random/dirty data is acceptable (and maybe better) for padding.
        offset += paddingLen;

        bytesWritten = offset;
        return true;
    }

    /// <summary>
    /// Rents a buffer from the shared <see cref="ArrayPool{T}"/> and populates it with a fake ClientHello packet.
    /// </summary>
    /// <param name="sniDomain">The target Service Name Indication (SNI) domain.</param>
    /// <param name="length">When this method returns, contains the length of the valid data in the buffer.</param>
    /// <returns>A byte array containing the packet.</returns>
    /// <remarks>
    /// <b>Important:</b> The caller is responsible for returning the array to <see cref="ArrayPool{T}.Shared"/> when done.
    /// </remarks>
    public static byte[] RentFakeClientHello(string sniDomain, out int length)
    {
        // Average ClientHello is around 300-500 bytes. 1024 is sufficient.
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);

        if (!TryWriteFakeClientHello(sniDomain, buffer, out length))
        {
            // Rare case: If domain is very long, 1024 might not be enough.
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = ArrayPool<byte>.Shared.Rent(4096);
            TryWriteFakeClientHello(sniDomain, buffer, out length);
        }

        return buffer;
    }

    private static void WriteExtensionHeader(Span<byte> buffer, ref int offset, ushort type, int length)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], type);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)length);
        offset += 2;
    }

    private static void Shuffle(Span<ushort> list)
    {
        int n = list.Length;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}