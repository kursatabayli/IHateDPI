namespace IHateDPI.Engine.Abstractions;

/// <summary>
/// Defines a contract for services capable of resolving DNS queries.
/// </summary>
public interface IDnsResolver
{
    /// <summary>
    /// Asynchronously resolves a raw DNS query and writes the response directly into the destination buffer.
    /// </summary>
    /// <param name="dnsQuery">The raw byte data of the DNS query packet.</param>
    /// <param name="destBuffer">The memory buffer where the DNS response will be written.</param>
    /// <param name="token">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains the number of bytes written to the <paramref name="destBuffer"/>, or 0 if the resolution failed.
    /// </returns>
    Task<int> ResolveToBufferAsync(ReadOnlyMemory<byte> dnsQuery, Memory<byte> destBuffer, CancellationToken token = default);
}