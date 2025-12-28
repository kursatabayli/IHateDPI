using System;
using System.Buffers.Binary;

namespace IHateDPI.Engine.Helpers;

/// <summary>
/// Provides utility methods for parsing TLS (Transport Layer Security) structures.
/// Primarily used to identify Server Name Indication (SNI) for surgical packet fragmentation.
/// </summary>
public static class TlsUtils
{
    /// <summary>
    /// Attempts to locate the SNI (Server Name Indication) extension within a TLS ClientHello payload.
    /// <para>
    /// This method is designed to be permissive and fault-tolerant. It attempts to parse
    /// as much as possible even if the packet is segmented (incomplete), avoiding exceptions.
    /// </para>
    /// </summary>
    /// <param name="payload">The TCP payload containing the TLS record.</param>
    /// <param name="offset">Output: The starting index of the SNI domain name relative to the payload.</param>
    /// <param name="length">Output: The length of the SNI domain name.</param>
    /// <returns>
    /// <c>true</c> if the SNI extension was found and the full domain name is contained within the payload;
    /// <c>false</c> if SNI is not found, the packet is not a ClientHello, or the SNI data is truncated.
    /// </returns>
    public static bool TryGetSniRange(ReadOnlySpan<byte> payload, out int offset, out int length)
    {
        offset = 0;
        length = 0;

        // Basic validation: Minimal packet size for a TLS handshake.
        if (payload.Length < 40) return false;

        var reader = payload;
        int position = 0;

        // 1. TLS Record Header Validation
        // Byte 0: Content Type (0x16 = Handshake)
        if (reader[0] != 0x16) return false;

        reader = reader[5..];
        position += 5;

        // 2. Handshake Header Validation
        // Byte 0: Handshake Type (0x01 = ClientHello)
        if (reader.Length < 4 || reader[0] != 0x01) return false;

        reader = reader[4..];
        position += 4;

        // 3. Skip Fixed Fields
        // Protocol Version (2 bytes) + Client Random (32 bytes) = 34 bytes
        if (reader.Length < 34) return false;

        reader = reader[34..];
        position += 34;

        // 4. Skip Session ID (Variable Length)
        if (reader.Length < 1) return false;
        int sessLen = reader[0];

        // Check if Session ID data exceeds packet bounds
        if (reader.Length < 1 + sessLen) return false;

        reader = reader[(1 + sessLen)..];
        position += 1 + sessLen;

        // 5. Skip Cipher Suites (Variable Length)
        // Length is prefixed by 2 bytes
        if (reader.Length < 2) return false;
        ushort cipherLen = BinaryPrimitives.ReadUInt16BigEndian(reader);

        // Note: Cipher Suites list can be large. If the packet is segmented here, 
        // we cannot reach extensions, so we must abort.
        if (reader.Length < 2 + cipherLen) return false;

        reader = reader[(2 + cipherLen)..];
        position += 2 + cipherLen;

        // 6. Skip Compression Methods (Variable Length)
        // Length is prefixed by 1 byte
        if (reader.Length < 1) return false;
        int compLen = reader[0];

        if (reader.Length < 1 + compLen) return false;

        reader = reader[(1 + compLen)..];
        position += 1 + compLen;

        // 7. Extensions Block
        // Length is prefixed by 2 bytes
        if (reader.Length < 2) return false;

        // Total declared length of extensions (might exceed available payload if segmented)
        // ushort extTotalLen = BinaryPrimitives.ReadUInt16BigEndian(reader); 

        reader = reader[2..];
        position += 2;

        var extensions = reader;

        // Iterate through Extensions
        // Ensure we have at least 4 bytes (Type + Length) to read the header
        while (extensions.Length >= 4)
        {
            ushort extType = BinaryPrimitives.ReadUInt16BigEndian(extensions);
            ushort extLen = BinaryPrimitives.ReadUInt16BigEndian(extensions[2..]);

            // Check for SNI Extension (Type 0x0000)
            if (extType == 0x0000)
            {
                // Do we have the full SNI data in this packet?
                if (extensions.Length >= 4 + extLen)
                {
                    var sniData = extensions[4..];

                    // SNI Structure: ListLength(2) + NameType(1) + NameLength(2) + Name
                    // Minimum size 5 bytes for the internal header
                    if (sniData.Length > 5 && sniData[2] == 0x00) // NameType 0x00 = HostName
                    {
                        ushort nameLen = BinaryPrimitives.ReadUInt16BigEndian(sniData[3..]);

                        // Calculate absolute offset relative to the original payload
                        // position: End of previous block
                        // 4: Extension Header (Type+Len)
                        // 5: SNI Internal Header (ListLen+Type+NameLen)
                        offset = position + 4 + 5;
                        length = nameLen;

                        // Final sanity check: Does the calculated range fall within payload bounds?
                        if (offset + length <= payload.Length)
                        {
                            return true;
                        }
                    }
                }
                // SNI found but data is truncated (e.g. split across TCP segments).
                // We cannot perform surgical splitting.
                return false;
            }

            // Move to the next extension
            int jump = 4 + extLen;

            // If the next extension is beyond the packet end, stop (Segmentation reached).
            if (extensions.Length < jump) break;

            extensions = extensions[jump..];
            position += jump;
        }

        return false;
    }
}