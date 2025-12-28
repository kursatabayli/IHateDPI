using System.Text.Json.Serialization;

namespace IHateDPI.Launcher.Models;

/// <summary>
/// Represents the runtime configuration and operational settings for the DPI engine.
/// <para>
/// Controls traffic manipulation strategies including Fragmentation, Window Clamping, 
/// Buffer Poisoning, and DNS over HTTPS (DoH).
/// </para>
/// </summary>
public sealed record EngineConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether DNS over HTTPS (DoH) is enabled.
    /// <para>Encrypts DNS queries to prevent censorship at the DNS level.</para>
    /// </summary>
    [JsonPropertyName("isDoHEnabled")]
    public bool IsDoHEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the URL of the DNS over HTTPS (DoH) provider.
    /// <para>Default: Cloudflare (https://1.1.1.1/dns-query).</para>
    /// </summary>
    [JsonPropertyName("dohProviderUrl")]
    public string DohProviderUrl { get; set; } = "https://1.1.1.1/dns-query";

    /// <summary>
    /// Gets or sets a value indicating whether to block UDP-based QUIC/HTTP3 traffic.
    /// <para>
    /// Blocking QUIC forces browsers to fall back to TCP/TLS, which allows 
    /// this engine to intercept and manipulate packets effectively.
    /// </para>
    /// </summary>
    [JsonPropertyName("blockQuic")]
    public bool BlockQuic { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum TCP Window size (TCP clamping).
    /// <para>
    /// Reducing this forces the server to send data in smaller chunks, 
    /// making it harder for DPI systems to reassemble the stream.
    /// </para>
    /// </summary>
    [JsonPropertyName("maxPayloadSize")]
    public int MaxPayloadSize { get; set; } = 1200;

    /// <summary>
    /// Gets or sets the fragmentation size for HTTP (Port 80) packets.
    /// <para>A value of 0 disables this feature.</para>
    /// </summary>
    [JsonPropertyName("fragmentHttp")]
    public int FragmentHttp { get; set; } = 0;

    /// <summary>
    /// Gets or sets the fixed fragmentation size for HTTPS (TLS ClientHello) packets.
    /// <para>
    /// Only used if <see cref="AutoSplitSni"/> is disabled or if SNI cannot be detected.
    /// Splitting the ClientHello header prevents DPI from identifying the protocol.
    /// </para>
    /// </summary>
    [JsonPropertyName("fragmentHttps")]
    public int FragmentHttps { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether "Smart Splitting" is enabled for TLS packets.
    /// <para>
    /// If true, the engine attempts to find the SNI (Server Name Indication) extension 
    /// and split it exactly in half. If SNI is missing (e.g. Encrypted ClientHello), 
    /// it falls back to a blind split at the first byte.
    /// </para>
    /// </summary>
    [JsonPropertyName("autoSplitSni")]
    public bool AutoSplitSni { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to send fragmented packets in reverse order.
    /// <para>
    /// Sends Part 2 before Part 1. Highly effective against stateful DPI systems 
    /// that expect sequential flow reassembly.
    /// </para>
    /// </summary>
    [JsonPropertyName("reverseFragmentation")]
    public bool ReverseFragmentation { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to inject "Junk" packets between valid fragments.
    /// <para>
    /// This technique aims to pollute the DPI's reassembly buffer with garbage data.
    /// The junk packet is designed to be accepted by the DPI but rejected by the destination server.
    /// </para>
    /// </summary>
    [JsonPropertyName("bufferPoisoning")]
    public bool BufferPoisoning { get; set; } = false;

    /// <summary>
    /// Gets or sets the size of the injected junk packet (in bytes).
    /// </summary>
    [JsonPropertyName("junkPacketSize")]
    public int JunkPacketSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of junk packets to inject per operation.
    /// <para>
    /// Increasing this count floods the DPI buffer more aggressively but adds network overhead.
    /// </para>
    /// </summary>
    [JsonPropertyName("junkPacketCount")]
    public ushort JunkPacketCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the base TTL (Time To Live) for the junk packet.
    /// <para>
    /// If set to > 0, the engine tries to drop the packet *after* the DPI but *before* the server.
    /// If 0, the packet relies on Checksum/Sequence manipulation to be dropped by the server.
    /// </para>
    /// </summary>
    [JsonPropertyName("junkPacketTTL")]
    public int JunkPacketTTL { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to corrupt the TCP Checksum of the junk packet.
    /// <para>
    /// This ensures the destination server discards the packet, preventing connection corruption,
    /// while many DPI systems (optimizing for speed) might still process it.
    /// </para>
    /// </summary>
    [JsonPropertyName("junkPacketBadChecksum")]
    public bool JunkPacketBadChecksum { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to manipulate the TCP Sequence number of the junk packet.
    /// <para>
    /// Creates overlaps or desynchronization in the DPI state machine without affecting the real stream.
    /// </para>
    /// </summary>
    [JsonPropertyName("junkPacketBadSequence")]
    public bool JunkPacketBadSequence { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to randomize the casing of the "Host" header.
    /// <para>Example: "hOsT: example.com"</para>
    /// </summary>
    [JsonPropertyName("mixHost")]
    public bool MixHost { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to remove the space after the "Host:" header key.
    /// <para>Example: "Host:example.com"</para>
    /// </summary>
    [JsonPropertyName("hostNoSpace")]
    public bool HostNoSpace { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to append a non-standard space or tab character to the "Host" header value.
    /// </summary>
    [JsonPropertyName("additionalSpace")]
    public bool AdditionalSpace { get; set; } = false;

    /// <summary>
    /// Gets or sets the TTL for standalone fake ClientHello packets.
    /// <para>
    /// This is a legacy method separate from <see cref="BufferPoisoning"/>.
    /// It sends a decoy packet *before* the real connection to trigger DPI filters early.
    /// </para>
    /// </summary>
    [JsonPropertyName("fakePacketTTL")]
    public int FakePacketTTL { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to use invalid sequence numbers for the standalone fake packet.
    /// </summary>
    [JsonPropertyName("badSequence")]
    public bool BadSequence { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use invalid checksums for the standalone fake packet.
    /// </summary>
    [JsonPropertyName("badCheckSum")]
    public bool BadCheckSum { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of times to re-transmit the standalone fake packet.
    /// </summary>
    [JsonPropertyName("fakeRequestResendCount")]
    public int FakeRequestResendCount { get; set; } = 1;
}