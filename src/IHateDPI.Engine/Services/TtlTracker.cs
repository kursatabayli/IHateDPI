using IHateDPI.Engine.Abstractions;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace IHateDPI.Engine.Services;

/// <summary>
/// Tracks Time To Live (TTL) values of incoming packets to estimate the distance (hop count) to the destination server.
/// </summary>
public sealed class TtlTracker : ITtlTracker, IDisposable
{
    // Simple key holding only the IP pair. Ports are irrelevant as the route is host-dependent.
    private readonly record struct IpPairKey(uint SrcIp, uint DstIp)
    {
        public override int GetHashCode()
        {
            return (int)(SrcIp ^ DstIp);
        }
    }
    // Lightweight data packet to be transported via the channel.
    private readonly record struct TtlUpdate(IpPairKey Key, byte Ttl);

    // Cache Entry: IP Pair -> (TTL, Last Seen Tick).
    private record struct CachedTtl(byte Ttl, long LastSeenTicks);

    // Thread-Safe Dictionary for fast lookups.
    private readonly ConcurrentDictionary<IpPairKey, CachedTtl> _cache = new();

    // High-performance data channel.
    private readonly Channel<TtlUpdate> _updateChannel;
    private readonly CancellationTokenSource _cts = new();
    private Task? _cleanupTask;

    public TtlTracker()
    {
        // Channel Settings:
        // Capacity is limited to 1000 since we are moving lightweight structs and processing is very fast.
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // The most recent TTL is always more valuable.
            SingleReader = true,
            SingleWriter = true
        };
        _updateChannel = Channel.CreateBounded<TtlUpdate>(options);
    }

    /// <inheritdoc />
    public void TrackPacket(uint srcIp, uint dstIp, ushort srcPort, ushort dstPort, byte ttl)
    {
        // Ignore ports. All connections to the same server follow the same route/TTL.
        var key = new IpPairKey(srcIp, dstIp);

        // Create a struct without heap allocation and push it to the channel.
        // TryWrite returns immediately; no await is needed.
        _updateChannel.Writer.TryWrite(new TtlUpdate(key, ttl));
    }

    /// <inheritdoc />
    public int GetCalculatedTtl(uint serverIp, uint clientIp, ushort serverPort, ushort clientPort, int defaultTtl)
    {
        // Query Direction: Server -> Client
        // TrackPacket (Incoming): Server(Src) -> Client(Dst)
        // We must use the same order here.
        var key = new IpPairKey(serverIp, clientIp);

        if (_cache.TryGetValue(key, out var record))
        {
            // No need to update 'LastSeenTicks' on read to keep read operations lock-free and fast.
            // Just calculate and return.
            return CalculateFakeTtl(record.Ttl);
        }

        return defaultTtl;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken externalCt)
    {
        // Stop if external token or internal token is cancelled.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, _cts.Token);
        var token = linkedCts.Token;

        // Start the cleanup task (Fire-and-forget but controlled via token).
        _cleanupTask = CleanupLoopAsync(token);

        var reader = _updateChannel.Reader;

        try
        {
            // Main loop focuses solely on processing data. No timers here.
            while (await reader.WaitToReadAsync(token))
            {
                while (reader.TryRead(out var update))
                {
                    // Copying structs takes nanoseconds. Dictionary updates are very fast.
                    _cache[update.Key] = new CachedTtl(update.Ttl, Environment.TickCount64);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Background task that periodically removes stale entries from the cache.
    /// </summary>
    private async Task CleanupLoopAsync(CancellationToken ct)
    {
        // Runs every 5 minutes.
        var interval = TimeSpan.FromMinutes(5);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(interval, ct);

                long now = Environment.TickCount64;
                long expireThreshold = 5 * 60 * 1000; // 5 minutes in milliseconds

                // Scan cache and remove old entries.
                foreach (var kvp in _cache)
                {
                    if (now - kvp.Value.LastSeenTicks > expireThreshold)
                    {
                        _cache.TryRemove(kvp.Key, out _);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Calculates the "Fake TTL" based on the estimated distance (hop count) to the destination.
    /// Derived from the GoodbyeDPI algorithm.
    /// </summary>
    private static int CalculateFakeTtl(byte ttl, int autoTtl1 = 1, int autoTtl2 = 4, int minHops = 3, int maxTtl = 10)
    {
        int nhops;

        // Standard initial TTL values for different OS families:
        // Linux/Unix: 64
        // Windows: 128
        // Solaris/Cisco: 255
        if (ttl > 128) nhops = 255 - ttl;
        else if (ttl > 64) nhops = 128 - ttl;
        else nhops = 64 - ttl;

        // If hop count is invalid or too small, do not use fake packet.
        if (nhops <= 0) return 0;

        // If server is too close (e.g., local network or ISP), skip fake packet.
        if (nhops < minHops) return 0;

        // Fuzzer logic / Error margin
        if (nhops <= autoTtl1) return 0; // Too risky if distance is very short.

        int fakeTtl = nhops - autoTtl2;

        // Smoothing formula for short distances
        if (fakeTtl < autoTtl2 && nhops <= 9)
        {
            float reduction = (autoTtl2 - autoTtl1) * (nhops / 10.0f);
            fakeTtl = nhops - autoTtl1 - (int)reduction;
        }

        // Safety bounds
        if (fakeTtl <= 0) fakeTtl = 1; // Must be at least 1 to leave the router.
        if (maxTtl > 0 && fakeTtl > maxTtl) fakeTtl = maxTtl;

        return fakeTtl;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}