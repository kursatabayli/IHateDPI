using IHateDPI.Engine.Abstractions;
using IHateDPI.Engine.Models;
using IHateDPI.Engine.Native;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace IHateDPI.Engine.Services;

/// <summary>
/// The central engine class responsible for managing packet capture, processing, and injection workflows.
/// <para>
/// It interacts directly with the WinDivert driver to filter network traffic, dispatches captured packets 
/// to the registered processors, and handles the re-injection of processed packets back into the network stack.
/// </para>
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DpiEngine"/> class with the required dependencies.
/// </remarks>
public sealed class DpiEngine(IPacketProcessor[] processors, IDnsResolver dnsResolver, Channel<DnsRequestSnapshot> dnsChannel, EngineConfig config, ITtlTracker ttlTracker) : IDisposable, IPacketInjector
{
    private IntPtr _handle;
    private volatile bool _isRunning;
    private CancellationTokenSource? _cts;
    private Thread? _workerThread;

    private readonly IPacketProcessor[] _processors = [.. processors];

    public event Action<string>? OnLog;

    /// <summary>
    /// Starts the packet capture engine and initializes background services.
    /// </summary>
    /// <remarks>
    /// The startup process involves:
    /// <list type="number">
    /// <item>Constructing the WinDivert filter string and opening the driver handle.</item>
    /// <item>Starting the DoH (DNS over HTTPS) service asynchronously if enabled.</item>
    /// <item>Launching the main packet processing loop (<see cref="PacketLoop"/>) on a dedicated high-priority thread.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown when the WinDivert driver cannot be opened (e.g., missing DLL or insufficient privileges).</exception>
    public void Start()
    {
        if (_isRunning) return;

        // --- FILTER CONSTRUCTION ---

        // 1. Outbound Traffic Rule
        // Capture TCP Port 80/443 and UDP Port 443 (QUIC).
        string outboundPorts = "((tcp.DstPort == 80 or tcp.DstPort == 443) or udp.DstPort == 443)";

        // Add UDP Port 53 if DoH is enabled to intercept DNS queries.
        if (config.IsDoHEnabled)
            outboundPorts += " or udp.DstPort == 53";

        string outboundRule = $"(outbound and !loopback and {outboundPorts})";

        // 2. Inbound Traffic Rule (For TTL Tracking)
        // We need to see inbound SYN/ACK packets to calculate the hop count (TTL) from the server.
        string? inboundRule = null;
        if (config.FakePacketTTL > 0)
        {
            inboundRule = "(inbound and !loopback and tcp.Syn and tcp.Ack and (tcp.SrcPort == 80 or tcp.SrcPort == 443))";
        }

        // 3. Combine Rules
        string filter = outboundRule;
        if (!string.IsNullOrEmpty(inboundRule))
        {
            filter += $" or {inboundRule}";
        }

        OnLog?.Invoke($"Applying WinDivert Filter: {filter}");

        // --- DRIVER INITIALIZATION ---

        try
        {
            // Layer: Network (0), Priority: 0, Flags: 0
            _handle = NativeMethods.WinDivertOpen(filter, 0, 0, 0);
        }
        catch (DllNotFoundException)
        {
            throw new Exception("WinDivert.dll not found! Please ensure the DLL is present in the application directory.");
        }

        // Validate Handle (IntPtr.Zero or -1 indicates failure)
        if (_handle == IntPtr.Zero || _handle == new IntPtr(-1))
        {
            int errorCode = Marshal.GetLastWin32Error();
            string errorMsg = errorCode switch
            {
                2 => "File Not Found (Error 2). WinDivert.dll or .sys files are missing.",
                5 => "Access Denied (Error 5). Please run the application as Administrator.",
                87 => "Invalid Parameter (Error 87). The filter string is syntax-incorrect.",
                203 => "Driver Load Failed (Error 203). WinDivert64.sys is missing or architecture (x64/x86) mismatch.",
                _ => $"Unknown Error: {errorCode}"
            };

            throw new Exception($"Failed to open WinDivert! {errorMsg}");
        }

        _isRunning = true;
        _cts = new CancellationTokenSource();

        // Start Background Services
        if (config.IsDoHEnabled)
        {
            var dohService = new DnsOverHttpsService(dnsChannel.Reader, dnsResolver, this);
            Task.Run(() => dohService.RunAsync(_cts.Token));
        }

        if (config.FakePacketTTL > 0)
            Task.Run(() => ttlTracker.RunAsync(_cts.Token));

        _workerThread = new Thread(PacketLoop)
        {
            IsBackground = true,
            Name = "DpiEngineWorker",
            Priority = ThreadPriority.Highest
        };
        _workerThread.Start();

        OnLog?.Invoke("Engine started successfully.");
    }

    /// <summary>
    /// Stops the engine, closes the driver handle, and releases all resources.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _cts?.Cancel();
        dnsChannel.Writer.TryComplete();

        NativeMethods.WinDivertClose(_handle);
        _handle = IntPtr.Zero;

        if (_workerThread != null && _workerThread.IsAlive)
        {
            _workerThread.Join(1000);
        }
        OnLog?.Invoke("[Engine] Stopped.");
    }

    /// <inheritdoc />
    public unsafe void Inject(byte* pPacket, uint packetLen, ref WinDivertAddress addr)
    {
        if (!_isRunning || _handle == IntPtr.Zero) return;
        NativeMethods.WinDivertSend(_handle, pPacket, packetLen, out _, ref addr);
    }

    /// <summary>
    /// Executes the main loop that continuously captures, processes, and re-injects network packets.
    /// </summary>
    /// <remarks>
    /// The loop follows this sequence:
    /// <list type="bullet">
    /// <item>Receive raw packet data from WinDivert.</item>
    /// <item>Parse headers to identify protocol and metadata.</item>
    /// <item>Pass the packet through the chain of <see cref="IPacketProcessor"/>s.</item>
    /// <item>Recalculate checksums if the packet content was modified.</item>
    /// <item>Re-inject the packet into the network stack (unless dropped).</item>
    /// </list>
    /// </remarks>
    [SkipLocalsInit]
    private unsafe void PacketLoop()
    {
        // Allocate a buffer large enough for the MTU (64KB is safe for loopback/jumbo frames).
        byte[] buffer = ArrayPool<byte>.Shared.Rent(65535);
        WinDivertAddress addr = new();

        try
        {
            fixed (byte* pBuffer = buffer)
            {
                WinDivertAddress* pAddr = &addr;

                while (_isRunning)
                {
                    try
                    {
                        if (!NativeMethods.WinDivertRecv(_handle, pBuffer, 65535, out uint readLen, ref addr))
                            continue;

                        // Parse packet headers
                        NativeMethods.WinDivertHelperParsePacket(pBuffer, readLen,
                            out IPHdr* ipHdr, out _, out _, out _, out _,
                            out TCPHdr* tcpHdr, out UDPHdr* udpHdr,
                            out void* payloadPtr, out uint payloadLen, out _, out _);

                        var context = new PacketContext
                        {
                            Handle = _handle,
                            RawPacket = pBuffer,
                            PacketLen = (int)readLen,
                            Address = pAddr,
                            IpHdr = ipHdr,
                            TcpHdr = tcpHdr,
                            UdpHdr = udpHdr,
                            Payload = (byte*)payloadPtr,
                            PayloadLen = payloadLen
                        };

                        // --- INBOUND PACKET PROCESSING ---
                        if (!context.IsOutbound)
                        {
                            // Track TTL from inbound SYN/ACK packets to estimate server distance.
                            if (tcpHdr != null && tcpHdr->Syn && tcpHdr->Ack)
                            {
                                ttlTracker.TrackPacket(
                                    ipHdr->SrcAddr, ipHdr->DstAddr,
                                    tcpHdr->SrcPort, tcpHdr->DstPort,
                                    ipHdr->TTL
                                );
                            }

                            // Inject inbound packets back immediately without modification.
                            NativeMethods.WinDivertSend(_handle, pBuffer, readLen, out _, ref addr);
                            continue;
                        }

                        // --- OUTBOUND PACKET PROCESSING ---
                        bool isModified = false;
                        bool shouldDrop = false;
                        uint newLen = readLen;

                        try
                        {
                            foreach (var processor in _processors)
                            {
                                // ref newLen: Allows processors to truncate or extend the packet.
                                if (processor.Process(context, ref newLen, out bool dropDecision))
                                {
                                    isModified = true;
                                    // If a processor modifies the packet, we usually stop the chain.
                                    break;
                                }

                                if (dropDecision)
                                {
                                    shouldDrop = true;
                                    break;
                                }
                            }

                            // If the packet is marked for dropping (e.g., QUIC blocked, DNS diverted), skip injection.
                            if (shouldDrop) continue;

                            // Recalculate checksums if content changed.
                            if (isModified)
                            {
                                NativeMethods.WinDivertHelperCalcChecksums(pBuffer, newLen, ref addr, 0);
                            }

                            // Inject the packet back into the network.
                            NativeMethods.WinDivertSend(_handle, pBuffer, newLen, out _, ref addr);
                        }
                        catch (Exception procEx)
                        {
                            // Fail-Open: If a processor crashes, log the error but allow the original packet to pass
                            // to avoid interrupting internet connectivity.
                            OnLog?.Invoke($"[Processor Error] {procEx.Message}");
                            NativeMethods.WinDivertSend(_handle, pBuffer, readLen, out _, ref addr);
                        }

                    }
                    catch (Exception ex)
                    {
                        if (_isRunning) OnLog?.Invoke($"[Error] {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}