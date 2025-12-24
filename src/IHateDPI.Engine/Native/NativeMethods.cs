using System.Runtime.InteropServices;

namespace IHateDPI.Engine.Native;

/// <summary>
/// Contains P/Invoke definitions providing low-level access to the WinDivert.dll library.
/// <para>
/// Utilizes the source-generated <see cref="LibraryImportAttribute"/> compliant with modern .NET standards
/// for improved performance and Native AOT compatibility.
/// </para>
/// </summary>
public static partial class NativeMethods
{
    private const string DllName = "WinDivert.dll";
    public const int WINDIVERT_SHUTDOWN_RECV = 0x1;
    public const int WINDIVERT_SHUTDOWN_SEND = 0x2;
    public const int WINDIVERT_SHUTDOWN_BOTH = 0x3;

    /// <summary>
    /// Opens a handle to the WinDivert driver to capture packets matching the specified filter.
    /// </summary>
    /// <param name="filter">A string containing the packet filter rule (WinDivert syntax).</param>
    /// <param name="layer">The layer to intercept (e.g., Network, Flow).</param>
    /// <param name="priority">The priority of the handle (-1000 to 1000). Higher priority handles receive packets first.</param>
    /// <param name="flags">Configuration flags for the handle.</param>
    /// <returns>A valid handle if successful; otherwise, <see cref="IntPtr.Zero"/>.</returns>
    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial WinDivertHandle WinDivertOpen(string filter, int layer, short priority, ulong flags);

    /// <summary>
    /// Receives a packet from the driver queue.
    /// </summary>
    /// <param name="handle">The handle obtained from <see cref="WinDivertOpen"/>.</param>
    /// <param name="pPacket">Pointer to the buffer where the packet data will be written.</param>
    /// <param name="packetLen">The size of the buffer.</param>
    /// <param name="pRecvLen">Receives the number of bytes actually read.</param>
    /// <param name="pAddr">Reference to the <see cref="WinDivertAddress"/> structure to receive packet metadata.</param>
    /// <returns><c>true</c> if a packet was successfully received; otherwise, <c>false</c>.</returns>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WinDivertRecv(
        WinDivertHandle handle, void* pPacket, uint packetLen, out uint pRecvLen, ref WinDivertAddress pAddr);

    /// <summary>
    /// Injects a packet into the network stack.
    /// </summary>
    /// <param name="handle">The handle obtained from <see cref="WinDivertOpen"/>.</param>
    /// <param name="pPacket">Pointer to the buffer containing the packet to be injected.</param>
    /// <param name="packetLen">The length of the packet to inject.</param>
    /// <param name="pSendLen">Receives the number of bytes actually injected.</param>
    /// <param name="pAddr">Reference to the <see cref="WinDivertAddress"/> structure specifying injection parameters (e.g., interface, direction).</param>
    /// <returns><c>true</c> if the packet was successfully injected; otherwise, <c>false</c>.</returns>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WinDivertSend(
        WinDivertHandle handle, void* pPacket, uint packetLen, out uint pSendLen, ref WinDivertAddress pAddr);

    /// <summary>
    /// Re-calculates the checksums for the IP, TCP, and UDP headers.
    /// <para>Should be called after modifying the packet content.</para>
    /// </summary>
    /// <param name="pPacket">Pointer to the start of the packet buffer.</param>
    /// <param name="packetLen">The total length of the packet.</param>
    /// <param name="pAddr">Reference to the packet metadata.</param>
    /// <param name="flags">Flags indicating which checksums to calculate.</param>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WinDivertHelperCalcChecksums(
        void* pPacket, uint packetLen, ref WinDivertAddress pAddr, ulong flags);

    /// <summary>
    /// Parses a raw packet and returns pointers to the various protocol headers.
    /// </summary>
    /// <param name="pPacket">Pointer to the raw packet buffer.</param>
    /// <param name="packetLen">Length of the packet buffer.</param>
    /// <param name="ppIpHdr">Receives a pointer to the IPv4 header.</param>
    /// <param name="ppIpv6Hdr">Receives a pointer to the IPv6 header.</param>
    /// <param name="pProtocol">Receives the next protocol number.</param>
    /// <param name="ppIcmpHdr">Receives a pointer to the ICMP header.</param>
    /// <param name="ppIcmpv6Hdr">Receives a pointer to the ICMPv6 header.</param>
    /// <param name="ppTcpHdr">Receives a pointer to the TCP header.</param>
    /// <param name="ppUdpHdr">Receives a pointer to the UDP header.</param>
    /// <param name="ppData">Receives a pointer to the payload data.</param>
    /// <param name="pDataLen">Receives the length of the payload data.</param>
    /// <param name="ppNext">Receives a pointer to the next packet (if available).</param>
    /// <param name="pNextLen">Receives the length of the next packet.</param>
    /// <returns><c>true</c> if parsing was successful; otherwise, <c>false</c>.</returns>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WinDivertHelperParsePacket(
        void* pPacket,
        uint packetLen,
        out IPHdr* ppIpHdr,
        out void* ppIpv6Hdr,
        out byte pProtocol,
        out void* ppIcmpHdr,
        out void* ppIcmpv6Hdr,
        out TCPHdr* ppTcpHdr,
        out UDPHdr* ppUdpHdr,
        out void* ppData,
        out uint pDataLen,
        out void* ppNext,
        out uint pNextLen);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WinDivertHelperParsePacket(
    void* pPacket,
    uint packetLen,
    out IPHdr* ppIpHdr,
    IntPtr ppIpv6Hdr,
    IntPtr pProtocol,
    IntPtr ppIcmpHdr,
    IntPtr ppIcmpv6Hdr,
    out TCPHdr* ppTcpHdr,
    out UDPHdr* ppUdpHdr,
    out void* ppData,
    out uint pDataLen,
    IntPtr ppNext,   
    IntPtr pNextLen);

    /// <summary>
    /// Closes the WinDivert handle and traffic.
    /// </summary>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinDivertShutdown(WinDivertHandle handle, int how);

    /// <summary>
    /// Closes the WinDivert handle and releases resources.
    /// </summary>
    /// <param name="handle">The handle to close.</param>
    /// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WinDivertClose(IntPtr handle);
}