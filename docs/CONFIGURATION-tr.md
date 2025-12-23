# IHateDPI Yapılandırma Rehberi

Bu doküman, `IHateDPI` uygulamasının motor ayarlarını (`EngineConfig`) ve bu ayarların DPI (Derin Paket İnceleme) sistemlerini atlatmadaki rollerini açıklar. Bu ayarları değiştirerek kendi internet servis sağlayıcınıza (ISS) ve ağ altyapınıza özel konfigürasyonlar oluşturabilirsiniz.

## ⚙️ Yapılandırma Yöntemleri

Ayarları değiştirmek için kullandığınız sürüme göre aşağıdaki iki yöntemden birini seçebilirsiniz:

### 1. Görsel Arayüz (Launcher) Kullanıcıları
Eğer uygulamayı **Launcher (Arayüz)** ile kullanıyorsanız, JSON dosyasıyla uğraşmanıza gerek yoktur.
* Uygulama içindeki **"Ayarlar"** menüsünü kullanarak tüm yapılandırmayı yapabilirsiniz.
* Yaptığınız değişiklikler, uygulama tarafından otomatik olarak `engineConfig.json` dosyasına kaydedilir.

### 2. Sadece Motor (Console Engine) Kullanıcıları
Eğer arayüz olmadan doğrudan konsol uygulamasını (`IHateDPI.Engine`) kullanıyorsanız:
* Programın bulunduğu klasördeki **`engineConfig.json`** dosyasını bir metin editörüyle (Notepad, VS Code vb.) açın.
* İlgili değerleri bu dokümandaki açıklamalara göre düzenleyip kaydedin ve uygulamayı yeniden başlatın.

---

⚠️ **ÖNEMLİ BAŞLANGIÇ NOTU:**
İnternet Servis Sağlayıcıların (ISS) kullandığı engelleme teknolojileri (DPI) birbirinden çok farklıdır. **Tüm kullanıcılar için çalışan tek bir "Sihirli Ayar" yoktur.**
* Varsayılan ayarlar sadece bir başlangıç noktasıdır.
* Sizin ağınızda çalışan bir ayar, başkasının internetini kesebilir.
* **Çözüm:** Aşağıdaki parametreleri anlayıp, kendi bağlantınız için en doğru kombinasyonu **deneme-yanılma** yoluyla bulmalısınız.

---

## 🌐 Ağ ve Bağlantı Ayarları (Network & Connection)

Bu ayarlar genel bağlantı davranışlarını ve protokol tercihlerini belirler.

### `isDoHEnabled` (DNS over HTTPS)
* **Açıklama:** Güvenli DNS (DoH) özelliğini açıp kapatır. Bu özellik, DNS sorgularınızı HTTPS trafiği içine gizleyerek şifreler.
* **Varsayılan:** `true` (Açık)
* **Nasıl Ayarlanmalı?**
    * **Önce Kapatıp Deneyin:** Eğer bu ayar `false` (kapalı) iken yasaklı sitelere erişebiliyorsanız, kapalı tutmanız önerilir. Bu sayede yerel DNS sunucunuzu kullanacağınız için bağlantı tepki süreniz (ping/latency) biraz daha düşük olabilir.
    * **Ne Zaman Açılmalı?** Eğer ayar kapalıyken siteye hiç ulaşılamıyor, farklı bir "Engellendi" sayfasına yönlendiriliyor veya tarayıcı "IP adresi bulunamadı" hatası veriyorsa; ISS'iniz DNS trafiğinizi (UDP 53) dinliyor ve manipüle ediyor demektir. Bu durumda bu ayarı **mutlaka açmalısınız**.

### `dohProviderUrl`
* **Açıklama:** DNS sorgularının şifreli olarak gönderileceği sunucu adresi.
* **⚠️ ÇOK ÖNEMLİ UYARI:** Bu alana **ASLA** alan adı (domain) içeren bir URL yazmayın! (Örneğin: `https://dns.google/dns-query` YAZMAYIN).
* **Doğru Kullanım:** URL mutlaka **IP adresi** içermelidir.
    * ✅ Doğru: `https://1.1.1.1/dns-query`
    * ❌ Yanlış: `https://cloudflare-dns.com/dns-query`
* **Neden? (Tavuk-Yumurta Problemi):** Eğer buraya bir alan adı yazarsanız, program bu sunucuya bağlanmak için önce onun IP adresini bulmaya çalışır. Ancak DNS sunucusu henüz çalışmadığı için IP çözülemez ve program kilitlenir.
* **Varsayılan:** `https://1.1.1.1/dns-query` (Cloudflare)
* **Kullanabileceğiniz Güvenli IP Adresleri:**
  * Google: `https://8.8.8.8/dns-query`
  * Quad9: `https://9.9.9.9/dns-query`

### `blockQuic` (QUIC/HTTP3 Engelleme)
* **Açıklama:** Tarayıcıların (özellikle Chrome) kullandığı UDP tabanlı QUIC protokolünü engeller.
* **Neden Önemli?** `IHateDPI` paket manipülasyonlarını TCP protokolü üzerinde gerçekleştirir. QUIC protokolü UDP kullandığı için bu manipülasyonlardan kaçabilir. Bu ayar açık olduğunda, tarayıcılar TCP'ye geri dönmeye (fallback) zorlanır ve DPI atlatma teknikleri işe yarar.
* **Varsayılan:** `true` (Açık)

### `maxPayloadSize` (TCP Veri Boyutu)
* **Açıklama:** Gönderilen TCP paketlerinin maksimum veri boyutunu sınırlar (MSS Clamping).
* **Varsayılan:** `1200`
* **⚠️ Öneri:** Bu ayarı **değiştirmeniz önerilmez.**
* **Neden?**
    * `1200` değeri, VPN veya farklı ağ tünelleri kullansanız bile paketlerin sorunsuz iletilmesini sağlayan en güvenli değerdir.
    * **Çok Düşük Yaparsanız:** (Örn: 500) İnternet hızınız ciddi oranda düşer.
    * **Çok Yüksek Yaparsanız:** (Örn: 1500) Paketler yolda kaybolabilir veya DPI sistemlerine daha kolay yakalanabilir.

---

## ✂️ Parçalama Ayarları (Fragmentation)

Paket parçalama, DPI sistemlerini atlatmanın en etkili yollarından biridir. İsteği (Request) birden fazla küçük pakete bölerek DPI cihazının "Bu yasaklı bir siteye giden istek" şeklinde birleştirmesini ve anlamasını zorlaştırır.

### ✅ Aktif Özellikler
Aşağıdaki ayarlar şu anda aktiftir ve motor tarafından işlenmektedir.

### `fragmentHttps` (Kritik Ayar)
* **Açıklama:** HTTPS bağlantılarındaki ilk paketi (ClientHello) belirtilen byte sayısından sonra böler.
* **Varsayılan:** `2`
* **Değer Aralığı:**
  * `0`: **Devre Dışı** (Özelliği kapatır).
  * `1 - 5`: **Önerilen Aralık.** (Bu değerler TLS başlığını böldüğü için DPI sistemlerini şaşırtmakta en etkili aralıktır).
* **Neden Önemli?** HTTPS şifreli olsa da, bağlantı kurulurken gidilen sitenin adı açık metin olarak gönderilir. Bu paketi en başından (örneğin 2. byte'tan) bölmek, DPI'ın bu başlığı okuyamamasını sağlar.

### `reverseFragmentation` (Ters Sırada Gönderme)
* **Açıklama:** Parçalanmış paketleri ters sırada gönderir (Önce 2. parça, sonra 1. parça).
* **Mantık:** TCP protokolü paketleri hedefte tekrar birleştirir, yani veri bozulmaz. Ancak aradaki DPI cihazı paketleri sırasız gördüğünde kafası karışır ve içeriği birleştiremeyip geçişine izin verebilir.
* **Varsayılan:** `true`

### 🚧 Geliştirme Aşamasındaki Özellikler (Henüz Aktif Değil)
*Aşağıdaki parametreler yapılandırma dosyasında yer almaktadır ancak kod entegrasyonu henüz tamamlanmamıştır. Bu değerleri değiştirmeniz şu an için motorun çalışmasını etkilemez.*

#### `fragmentHttp`
* **Durum:** 🛠️ *Geliştiriliyor*
* **Tanım:** Şifresiz HTTP istekleri için ilk paketin kaçıncı byte'tan bölüneceğini belirler (Örn: "GET" kelimesini bölmek için).

#### `fragmentPersistentHttp`
* **Durum:** 🛠️ *Geliştiriliyor*
* **Tanım:** Sürekli açık kalan (Keep-Alive) HTTP bağlantılarında sonraki isteklerin parçalanma boyutunu ayarlar.

#### `nativeFragmentation`
* **Durum:** 🛠️ *Geliştiriliyor*
* **Tanım:** Paketleri uygulama katmanı yerine işletim sistemi seviyesinde (Native TCP Fragmentation) bölmeyi hedefler.

---

## 🛠️ Başlık Manipülasyonu (Header Manipulation)

Bu ayarlar, HTTP isteğindeki metin formatını değiştirerek DPI imzalarını (signatures) bozmayı hedefler. Sadece düz HTTP (şifresiz) sitelerde etkilidir.

### `mixHost`
* **Açıklama:** "Host" başlığını rastgele büyük/küçük harf yapar.
* **Örnek:** `Host: example.com` -> `hOsT: example.com`
* **Mantık:** Sunucular bunu anlar ama DPI cihazları bazen sadece "Host" kelimesine duyarlıdır.
* **Varsayılan:** `true`

### `hostNoSpace`
* **Açıklama:** Başlıktaki iki nokta üst üsteden sonraki boşluğu siler.
* **Örnek:** `Host: example.com` -> `Host:example.com`
* **Varsayılan:** `true`

### `additionalSpace`
* **Açıklama:** HTTP Metodu ile URI arasına fazladan boşluk veya tab karakteri ekler.
* **Varsayılan:** `false`

---

## 🎭 Sahte Paket Ayarları (Fake Packet)

Bu ayarlar, DPI sistemini kandırmak için araya "Fake" (Sahte) veri paketleri sıkıştırır. Bu bölümdeki ayarlar **ileri düzeydir**; yanlış yapılandırma internet bağlantınızı tamamen kesebilir.

### `fakePacketTTL` (Yaşam Süresi ve Oto-Takip)
* **Açıklama:** Sahte paketin ağ üzerinde kaç sekme (hop) gideceğini belirler.
* **Akıllı Mesafe Takibi (Auto-Learning):** Uygulama içinde gömülü bir **TtlTracker** (Mesafe Takipçisi) bulunur. Bu sistem, hedef sunucu ile aranızdaki mesafeyi otomatik olarak hesaplar. Buraya girdiğiniz değer, sistem henüz hesaplama yapamadığında kullanılan bir **"Fallback" (Yedek/Başlangıç)** değeridir.
* **Varsayılan:** `5`
* **Kapatmak İçin:** Bu değeri `0` yaparsanız sahte paket gönderimi tamamen **devre dışı** kalır.
* **Mantık:** Paket DPI cihazından geçmeli ancak gerçek sunucuya ulaşmadan yok olmalıdır.

### `badSequence` (Bozuk Sıra Numarası)
* **Açıklama:** Sahte paketin TCP sıra numarasını (Sequence Number) kasten yanlış gönderir.
* **Risk:** Bazı internet servis sağlayıcılarında DPI atlatmayı sağlarken, bazılarında **internet bağlantısını tamamen bozabilir.**
* **Öneri:** Varsayılan olarak kapalı tutun. Eğer diğer yöntemler çalışmazsa açıp deneyin; bağlantınız koparsa tekrar kapatın.
* **Varsayılan:** `false`

### `badCheckSum` (Bozuk Doğrulama Kodu)
* **Açıklama:** Sahte paketin doğrulama kodunu (Checksum) bozuk gönderir.
* **Mantık:** DPI sistemleri performans kazanmak için genelde bu kontrolü yapmaz ve paketi yer; ancak gerçek sunucular paketi reddeder (ki istediğimiz de budur).
* **Risk:** Tıpkı `badSequence` gibi, bazı ağ donanımları (modemler, routerlar) checksum'ı bozuk paketleri otomatik engelleyebilir ve bağlantı sorununa yol açabilir. Deneme-yanılma gerektirir.
* **Varsayılan:** `false`

### `fakeRequestResendCount`
* **Açıklama:** Sahte paketin kaç kez art arda gönderileceğini belirler.
* **Varsayılan:** `1`
* **Önerilen Aralık:** `1 - 3`
* **Uyarı:** Bu sayıyı çok artırmak (örneğin 10 yapmak), ağ trafiğinizi gereksiz yere şişirir ve modeminizin kilitlenmesine neden olabilir. Genelde `1` veya en fazla `2` yeterlidir.

---

## 🧪 Nasıl Deneme Yapmalısınız? (Strateji Rehberi)

Eğer bağlantı sorunu yaşıyorsanız, lütfen önce aşağıdaki çözüm yollarını deneyin:

1.  **Adım 1 (Temel):** Sadece `fragmentHttps` değerini değiştirin (`1`, `2`, `3`).
2.  **Adım 2 (Sıralama):** `reverseFragmentation` ayarını `false` yapın. Bazı ağlar ters paket sevmez.
3.  **Adım 3 (Riskli Bölge):** Hala girmiyorsa `badCheckSum` veya `badSequence` ayarlarını `true` yapın. **Dikkat:** Bu interneti keserse hemen geri kapatın.
4.  **Adım 4 (DoH):** Yasaklı siteye hiç ping atamıyorsanız veya IP adresi bulunamıyorsa `isDoHEnabled` mutlaka `true` olmalıdır.
