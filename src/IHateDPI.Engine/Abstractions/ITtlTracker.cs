namespace IHateDPI.Engine.Abstractions;

/// <summary>
/// Defines a contract for tracking packet TTLs (Time To Live) to calculate the distance (hop count) to the destination.
/// <para>
/// This information is used to set the TTL of fake packets so that they reach the DPI inspector but expire before reaching the server.
/// </para>
/// </summary>
public interface ITtlTracker
{
    /// <summary>
    /// Starts the background service responsible for processing accumulated TTL data.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues the TTL information of a captured packet for asynchronous processing (Non-blocking).
    /// </summary>
    void TrackPacket(uint srcIp, uint dstIp, ushort srcPort, ushort dstPort, byte ttl);

    /// <summary>
    /// Calculates or retrieves the appropriate "Fake TTL" value for a specific connection flow.
    /// </summary>
    /// <param name="defaultTtl">The fallback TTL value to use if no data is available for this connection.</param>
    /// <returns>The calculated TTL value ensuring the packet drops after the DPI checkpoint.</returns>
    int GetCalculatedTtl(uint serverIp, uint clientIp, ushort serverPort, ushort clientPort, int defaultTtl);
}