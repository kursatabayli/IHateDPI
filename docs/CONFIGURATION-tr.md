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

### `isDoHEnabled` (DNS over HTTPS)
* **Açıklama:** DNS sorgularını HTTPS trafiği içine gizler (Şifreli DNS).
* **Varsayılan:** `true`
* **Öneri:** Yasaklı sitelere IP seviyesinde değil de DNS seviyesinde engelleme yapılıyorsa bu ayar mutlaka açık olmalıdır.

### `dohProviderUrl`
* **Açıklama:** Güvenli DNS sunucusu adresi.
* **⚠️ Önemli:** Buraya alan adı (domain) yazmayın, **IP adresi** içeren URL kullanın.
* **Varsayılan:** `https://1.1.1.1/dns-query`
* **Alternatifler:**
  * Google: `https://8.8.8.8/dns-query`
  * Quad9: `https://9.9.9.9/dns-query`

### `blockQuic` (QUIC Engelleme)
* **Açıklama:** Tarayıcıların UDP tabanlı QUIC/HTTP3 protokolünü kullanmasını engeller ve onları TCP kullanmaya zorlar.
* **Varsayılan:** `false`
* **Neden Açılmalı?** IHateDPI, paket manipülasyonlarını TCP üzerinde yapar. Eğer tarayıcınız YouTube veya Google servislerine girerken QUIC (UDP) kullanırsa, DPI motoru bu paketleri manipüle edemez. Bu durumda bu ayarı `true` yapmalısınız.

### `maxPayloadSize` (TCP Window Clamping)
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

### `autoSplitSni` (Akıllı SNI Bölme)
* **Açıklama:** Motor, HTTPS paketinin içindeki SNI (gidilen sitenin adı) bilgisini otomatik tespit eder ve paketi tam ortasından ikiye böler.
* **Varsayılan:** `false`
* **Avantajı:** `fragmentHttps` ile manuel byte saymak yerine, motorun paketi en kritik noktadan bölmesini sağlar.

### `fragmentHttps` (Manuel HTTPS Bölme)
* **Açıklama:** HTTPS (TLS ClientHello) paketini belirtilen byte sayısından sonra böler.
* **Varsayılan:** `0` (Kapalı)
* **Kullanımı:** Eğer `autoSplitSni` kapalıysa veya işe yaramıyorsa, buraya `1` ile `5` arasında bir değer girerek manuel bölme yapabilirsiniz.

### `fragmentHttp` (HTTP Bölme)
* **Açıklama:** Şifresiz HTTP isteklerini belirtilen byte sayısından sonra böler.
* **Varsayılan:** `0` (Kapalı)

### `reverseFragmentation` (Ters Sırada Gönderme)
* **Açıklama:** Bölünen paketleri ters sırada gönderir (Önce 2. parça, sonra 1. parça).
* **Varsayılan:** `false`
* **Not:** Durum denetimli (Stateful) DPI sistemlerinde çok etkilidir ancak bazı modemler/routerlar bunu sevmez.

---

## ☠️ Tampon Zehirleme (Buffer Poisoning) 🔥 *YENİ*

DPI cihazının belleğini (buffer) "çöp" veriyle doldurarak, gerçek paketi incelemesini engellemeyi amaçlar.

### ⚠️ KRİTİK ÇALIŞMA ŞARTI
**Bu özelliğin çalışabilmesi için paketlerin bölünmüş olması ZORUNLUDUR.**
Yani Buffer Poisoning'i açacaksanız aşağıdakilerden **en az biri** yapılmış olmalıdır:
1.  ✅ `autoSplitSni`: **true** OLMALI
2.  ✅ VEYA `fragmentHttps`: **0'dan büyük** OLMALI

Eğer paket bölünmezse, araya zehirli (junk) paket enjekte edilecek bir "aralık" oluşmaz.

### `bufferPoisoning`
* **Açıklama:** Parçalanmış gerçek paketlerin arasına sahte "Junk" (Çöp) paketler sıkıştırır.
* **Varsayılan:** `false`

### Zehirli Paket Ayarları:
* **`junkPacketSize`**: Çöp paketin boyutu (byte). (Varsayılan: `1`)
* **`junkPacketCount`**: Kaç adet çöp paket gönderileceği. (Varsayılan: `1`)
* **`junkPacketTTL`**: Çöp paketin yaşam süresi. (Varsayılan: `5`)
    * *Mantık:* Bu paket DPI'dan geçmeli ama gerçek sunucuya varmadan ölmelidir.
* **`junkPacketBadChecksum`**: Çöp paketin doğrulama kodunu bozar. (Varsayılan: `false`)
* **`junkPacketBadSequence`**: Çöp paketin sıra numarasını bozar. (Varsayılan: `false`)

---

## 🛠️ Başlık Manipülasyonu (Sadece HTTP)

Şifresiz HTTP sitelerinde "Host" başlığını değiştirerek filtreleri atlatır.
* **`mixHost`**: `hOsT: example.com` yapar. (Varsayılan: `false`)
* **`hostNoSpace`**: `Host:example.com` yapar (boşluğu siler). (Varsayılan: `false`)
* **`additionalSpace`**: Metot ile URI arasına fazladan boşluk ekler. (Varsayılan: `false`)

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
