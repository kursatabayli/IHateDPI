using IHateDPI.Engine.Native;

namespace IHateDPI.Engine.Abstractions;

/// <summary>
/// Provides a mechanism to inject raw network packets into the network stack.
/// </summary>
public unsafe interface IPacketInjector
{
    /// <summary>
    /// Injects a raw packet into the network using the specified WinDivert address information.
    /// </summary>
    /// <param name="pPacket">A pointer to the memory location containing the raw packet data.</param>
    /// <param name="packetLen">The size of the packet in bytes.</param>
    /// <param name="addr">A reference to the <see cref="WinDivertAddress"/> structure containing metadata required for injection (e.g., interface index, direction).</param>
    void Inject(byte* pPacket, uint packetLen, ref WinDivertAddress addr);
}