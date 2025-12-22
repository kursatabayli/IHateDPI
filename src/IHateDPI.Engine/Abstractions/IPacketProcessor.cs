using IHateDPI.Engine.Models;

namespace IHateDPI.Engine.Abstractions;

/// <summary>
/// Defines the contract for intercepting, analyzing, and manipulating raw network packets.
/// </summary>
public unsafe interface IPacketProcessor
{
    /// <summary>
    /// Processes a captured packet, potentially modifying its content or deciding to drop it entirely.
    /// </summary>
    /// <param name="context">The low-level context containing pointers to the raw packet data and protocol headers.</param>
    /// <param name="newPacketLen">
    /// A reference to the packet length. Implementations must update this value if the payload size changes (e.g., due to fragmentation or truncation).
    /// </param>
    /// <param name="shouldDrop">
    /// When this method returns, contains a value indicating whether the packet should be dropped (blocked) and not re-injected into the network.
    /// </param>
    /// <returns>
    /// <c>true</c> if the packet was modified (requiring checksum recalculation); otherwise, <c>false</c>.
    /// </returns>
    bool Process(PacketContext context, ref uint newPacketLen, out bool shouldDrop);
}