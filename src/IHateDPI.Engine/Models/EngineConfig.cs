using System.Text.Json.Serialization;

namespace IHateDPI.Engine.Models;

/// <summary>
/// Represents the runtime configuration and operational settings for the DPI engine.
/// </summary>
public sealed record EngineConfig
{
    // Network & Connection Settings

    /// <summary>
    /// Gets or sets a value indicating whether DNS over HTTPS (DoH) is enabled.
    /// <para>Default value: <c>true</c></para>
    /// </summary>
    [JsonPropertyName("isDoHEnabled")]
    public bool IsDoHEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the URL of the DNS over HTTPS (DoH) provider.
    /// <para>Example: <c>https://1.1.1.1/dns-query</c></para>
    /// </summary>
    [JsonPropertyName("dohProviderUrl")]
    public string DohProviderUrl { get; set; } = "https://1.1.1.1/dns-query";

    /// <summary>
    /// Gets or sets a value indicating whether to block QUIC/HTTP3 traffic.
    /// <para>
    /// Blocking UDP-based QUIC forces browsers to fall back to TCP, 
    /// which allows for more effective packet manipulation.
    /// </para>
    /// </summary>
    [JsonPropertyName("blockQuic")]
    public bool BlockQuic { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum TCP payload size. 
    /// Reducing this value can help evade DPI systems that analyze large packets.
    /// <para>Example: <c>1200</c></para>
    /// </summary>
    [JsonPropertyName("maxPayloadSize")]
    public int MaxPayloadSize { get; set; } = 1200;

    // Fragmentation Settings

    /// <summary>
    /// Gets or sets the fragmentation size for the initial HTTP packet. 
    /// A value of 0 disables this feature.
    /// <para>Example: <c>2</c> (Sends the first 2 bytes of the GET request separately)</para>
    /// </summary>
    [JsonPropertyName("fragmentHttp")]
    public int FragmentHttp { get; set; } = 0;

    /// <summary>
    /// Gets or sets the fragmentation size for the initial HTTPS (TLS ClientHello) packet.
    /// <para>Critical for masking the SNI (Server Name Indication) field.</para>
    /// </summary>
    [JsonPropertyName("fragmentHttps")]
    public int FragmentHttps { get; set; } = 2;

    /// <summary>
    /// Gets or sets the fragmentation size for persistent (Keep-Alive) HTTP connections.
    /// </summary>
    [JsonPropertyName("fragmentPersistentHttp")]
    public int FragmentPersistentHttp { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether to use OS-level (native) TCP fragmentation.
    /// </summary>
    [JsonPropertyName("nativeFragmentation")]
    public bool NativeFragmentation { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to send fragmented packets in reverse order.
    /// <para>
    /// Sending the second fragment before the first is highly effective against stateful DPI systems.
    /// </para>
    /// </summary>
    [JsonPropertyName("reverseFragmentation")]
    public bool ReverseFragmentation { get; set; } = true;

    // Header Manipulation Settings

    /// <summary>
    /// Gets or sets a value indicating whether to randomize the case of the "Host" header (e.g., "hOsT").
    /// </summary>
    [JsonPropertyName("mixHost")]
    public bool MixHost { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to remove the space after the "Host:" header key.
    /// <para>Example: Converts "Host: google.com" to "Host:google.com".</para>
    /// </summary>
    [JsonPropertyName("hostNoSpace")]
    public bool HostNoSpace { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to append a space or tab character to the "Host" header.
    /// </summary>
    [JsonPropertyName("additionalSpace")]
    public bool AdditionalSpace { get; set; } = false;

    // Fake Packet Settings

    /// <summary>
    /// Gets or sets the Time To Live (TTL) value for fake packets.
    /// <para>
    /// Low values ensure the packet reaches the DPI inspector but expires before reaching the destination server.
    /// </para>
    /// </summary>
    [JsonPropertyName("fakePacketTTL")]
    public int FakePacketTTL { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to use an invalid TCP sequence number for fake packets.
    /// </summary>
    [JsonPropertyName("badSequence")]
    public bool BadSequence { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use an invalid TCP checksum for fake packets.
    /// </summary>
    [JsonPropertyName("badCheckSum")]
    public bool BadCheckSum { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of times to resend the fake request packet.
    /// </summary>
    [JsonPropertyName("fakeRequestResendCount")]
    public int FakeRequestResendCount { get; set; } = 1;
}

/// <summary>
/// Required for Native AOT and assembly trimming support.
/// </summary>
[JsonSerializable(typeof(EngineConfig))]
internal partial class AppConfigContext : JsonSerializerContext
{
}