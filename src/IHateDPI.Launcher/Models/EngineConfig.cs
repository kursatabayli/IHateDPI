using System.Text.Json.Serialization;

namespace IHateDPI.Launcher.Models;

/// <summary>
/// Başlatıcı (Launcher) arayüzü tarafından kullanılan ve disk üzerinde saklanan uygulama yapılandırma ayarlarını temsil eder.
/// </summary>
public sealed record EngineConfig
{
    // ==========================================
    // 1. Genel Ağ ve Bağlantı Ayarları
    // ==========================================

    /// <summary>
    /// DNS over HTTPS (DoH) özelliğinin etkin olup olmadığını belirtir.
    /// <para>Varsayılan değer: <c>true</c></para>
    /// </summary>
    [JsonPropertyName("isDoHEnabled")]
    public bool IsDoHEnabled { get; set; } = true;

    /// <summary>
    /// DoH sorgularının gönderileceği sağlayıcının URL adresi.
    /// <para>Örnek: <c>https://1.1.1.1/dns-query</c></para>
    /// </summary>
    [JsonPropertyName("dohProviderUrl")]
    public string DohProviderUrl { get; set; } = "https://1.1.1.1/dns-query";

    /// <summary>
    /// QUIC/HTTP3 protokolünün engellenip engellenmeyeceği.
    /// DPI sistemleri bazen UDP tabanlı QUIC'i inceleyemediği için tamamen bloklar,
    /// bu durumda TCP'ye zorlamak gerekebilir.
    /// </summary>
    [JsonPropertyName("blockQuic")]
    public bool BlockQuic { get; set; } = true;

    /// <summary>
    /// Gönderilecek maksimum veri yükü boyutu. 
    /// Büyük paketlerin DPI tarafından yakalanmasını önlemek için düşürülebilir.
    /// <para>Örnek: <c>1200</c></para>
    /// </summary>
    [JsonPropertyName("maxPayloadSize")]
    public int MaxPayloadSize { get; set; } = 1200;

    // ==========================================
    // 2. Parçalama (Fragmentation) Ayarları
    // ==========================================

    /// <summary>
    /// HTTP istekleri için parçalama boyutu. 
    /// İlk paketin kaç byte olacağını belirler. 0 ise kapalıdır.
    /// <para>Örnek: <c>2</c> (GET isteğinin ilk 2 harfini ayrı gönderir)</para>
    /// </summary>
    [JsonPropertyName("fragmentHttp")]
    public int FragmentHttp { get; set; } = 0;

    /// <summary>
    /// HTTPS (TLS ClientHello) istekleri için parçalama boyutu.
    /// SNI (Server Name Indication) alanını gizlemek için kritiktir.
    /// </summary>
    [JsonPropertyName("fragmentHttps")]
    public int FragmentHttps { get; set; } = 2;

    /// <summary>
    /// Keep-Alive (Kalıcı) HTTP bağlantılarında parçalamanın devam edip etmeyeceği.
    /// </summary>
    [JsonPropertyName("fragmentPersistentHttp")]
    public int FragmentPersistentHttp { get; set; } = 0;

    /// <summary>
    /// İşletim sistemi seviyesinde TCP parçalaması (Native)
    /// </summary>
    [JsonPropertyName("nativeFragmentation")]
    public bool NativeFragmentation { get; set; } = false;

    /// <summary>
    /// Paketleri ters sırada gönderme (Reverse Fragmentation).
    /// Önce 2. parça, sonra 1. parça gönderilir. DPI'ın kafasını karıştırmak için çok etkilidir.
    /// </summary>
    [JsonPropertyName("reverseFragmentation")]
    public bool ReverseFragmentation { get; set; } = true;

    // ==========================================
    // 3. Başlık (Header) Manipülasyonu
    // ==========================================

    /// <summary>
    /// "Host" başlığını "hOsT" gibi karışık büyük/küçük harfle gönderir.
    /// </summary>
    [JsonPropertyName("mixHost")]
    public bool MixHost { get; set; } = true;

    /// <summary>
    /// "Host: google.com" yerine "Host:google.com" (boşluksuz) gönderir.
    /// </summary>
    [JsonPropertyName("hostNoSpace")]
    public bool HostNoSpace { get; set; } = true;

    /// <summary>
    /// Host başlığının sonuna ekstra boşluk veya tab karakteri ekler.
    /// </summary>
    [JsonPropertyName("additionalSpace")]
    public bool AdditionalSpace { get; set; } = false;

    // ==========================================
    // 4. Sahte Paket (Fake Request) Ayarları
    // ==========================================

    /// <summary>
    /// Sahte paketlerin yaşam süresi (TTL).
    /// DPI cihazına ulaşıp sunucuya ulaşmadan ölmesi için düşük tutulur.
    /// </summary>
    [JsonPropertyName("fakePacketTTL")]
    public int FakePacketTTL { get; set; } = 5;

    /// <summary>
    /// Sahte paketin TCP Sequence numarasını bozuk gönderir.
    /// </summary>
    [JsonPropertyName("badSequence")]
    public bool BadSequence { get; set; } = false;

    /// <summary>
    /// Sahte paketin TCP Checksum değerini bozuk gönderir.
    /// </summary>
    [JsonPropertyName("badCheckSum")]
    public bool BadCheckSum { get; set; } = false;

    /// <summary>
    /// Sahte paketin kaç kez tekrar gönderileceği.
    /// </summary>
    [JsonPropertyName("fakeRequestResendCount")]
    public int FakeRequestResendCount { get; set; } = 1;
}