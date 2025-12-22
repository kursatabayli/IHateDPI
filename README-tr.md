<div align="center">
  <p>
    <a href="README.md">English Version</a>
  </p>
</div>

# IHateDPI

**IHateDPI**, Windows işletim sistemlerinde ağ trafiği analizi, paket manipülasyonu ve siber güvenlik konularında teknik yetkinlik kazanmak; aynı zamanda modern .NET altyapısında hafıza yönetimi (Memory Management) konularındaki teorik bilgiyi pratiğe dökmek amacıyla geliştirilmiştir.

Bu proje **.NET 9** tabanlı, modern, performans odaklı ve açık kaynaklı bir araçtır.

> **Not:** Bu proje, [GoodbyeDPI](https://github.com/ValdikSS/GoodbyeDPI) projesinden ilham alınarak; modern C# teknikleri, Native AOT ve güncel .NET altyapısı ile sıfırdan geliştirilmiştir.

## Özellikler

* **DoH (DNS over HTTPS):** DNS sorgularınızı şifreleyerek 3. parti bir DNS sunucusuna (varsayılan Cloudflare) göndermenizi sağlar.
* **TCP Window Clamping:** Sunucudan gelen veri akışını manipüle ederek DPI analizini zorlaştırır.
* **HTTPS Fragmentation:** Güvenli bağlantı (TLS) paketlerini parçalayarak SNI (Server Name Indication) analizini atlatır.
* **QUIC Engelleme:** Tarayıcıları, manipüle edilmesi daha kolay olan TCP protokolünü kullanmaya zorlar.
* **Modern Altyapı:** .NET 9 performansı ve Native AOT teknolojisi ile hızlı, hafif ve kurulum gerektirmez.

---

### ⚠️ Önemli Yapılandırma ve Sistem Notları

#### 1. DoH (DNS over HTTPS) Adresleri
Uygulama ayarlarında DoH sunucusu belirtirken **kesinlikle IP adresi** kullanmalısınız. Eğer DoH sunucusu olarak bir alan adı (domain) yazarsanız, program bu alanı çözümleyemez ve "Tavuk-Yumurta" problemi oluşarak tüm internet trafiğiniz kesilebilir.

* ❌ **Yanlış:** `https://cloudflare-dns.com/dns-query`
* ✅ **Doğru:** `https://1.1.1.1/dns-query`

#### 2. Sistem ve Protokol Kısıtlamaları
* **Sadece 64-bit:** Proje yalnızca **64-bit (x64)** Windows işletim sistemlerini desteklemektedir. 32-bit (x86) sistemlerde çalışmaz.
* **IPv6 Desteği:** Şu an için sadece **IPv4** trafiği desteklenmektedir ve işlenmektedir. Eğer sisteminizde IPv6 aktifse, IPv6 üzerinden geçen trafik bu araç tarafından filtrelenmez veya manipüle edilmez (olduğu gibi iletilir). IPv6 desteği **geliştirilme aşamasındadır**.

#### 3. Henüz Aktif Olmayan Özellikler
`EngineConfig` dosyasında veya arayüzde yer alsa da, aşağıdaki özellikler henüz kod tarafında tam olarak implemente edilmemiştir ve şu an için etkisi yoktur:
* `NativeFragmentation`
* `FragmentHttp`
* `FragmentPersistentHttp`

---

## GoodbyeDPI Entegrasyonu

IHateDPI Launcher, dilerseniz motor olarak orijinal **GoodbyeDPI** yazılımını da kullanmanıza olanak tanır. Bu sayede iki motor arasında kolayca geçiş yapabilirsiniz.

**Nasıl Kullanılır?**
1.  Orijinal GoodbyeDPI dosyalarını indirin.
2.  İndirdiğiniz dosyaları (x86_64 klasörü ve .cmd dosyaları dahil) uygulamanın kurulu olduğu dizindeki `Engines/GoodbyeDPI` klasörüne kopyalayın.
3.  IHateDPI Launcher'ı açın, **Ayarlar** menüsüne gidin.
4.  Motor seçim ekranından **GoodbyeDPI**'ı seçin ve listeden çalıştırmak istediğiniz `.cmd` dosyasını belirleyin.

---

## Kurulum ve Kullanım

Bu araç "Taşınabilir (Portable)" olarak tasarlanmıştır. Herhangi bir kurulum gerektirmez.

1.  [Releases](https://github.com/kursatabayli/IHateDPI/releases) sayfasından `IHateDPI-v1.0.0-beta.1-win-x64.zip` (önerilen) veya `IHateDPI-Engine-v1.0.0-beta.1-win-x64.zip` paketlerinden birini indirin.
2.  Zip dosyasını bir klasöre çıkartın.
3.  `IHateDPI Launcher.exe` (önerilen) veya `IHateDPI Engine.exe` dosyasına sağ tıklayıp **"Yönetici Olarak Çalıştır"** seçeneğini kullanın.
4.  Kullanıma Başlama:
    * **Launcher kullanıyorsanız:** Arayüzdeki "Başlat" butonuna tıklayın.
    * **Düz Engine kullanıyorsanız:** Konsol penceresi açıldığında program çalışmaya başlamıştır.

> **Neden Yönetici Yetkisi?**
> Program, ağ paketlerini yakalamak, filtrelemek ve manipüle etmek için **WinDivert** sürücüsünü kullanır. Windows güvenlik mimarisi gereği, bu sürücüyle iletişim kurmak için yönetici hakları zorunludur.

## Geliştirme Durumu ve Geri Bildirim

**Bu proje henüz aktif geliştirme aşamasındadır.**

Temel özellikler kararlı çalışsa da, farklı ağ sağlayıcılarında veya sistem yapılandırmalarında beklenmedik durumlarla karşılaşabilirsiniz.

* **Hata Bildirimi:** Çalışmayan bir özellik veya bir hata (bug) bulursanız **[Issues](https://github.com/kursatabayli/IHateDPI/issues)** sekmesi üzerinden paylaşabilirsiniz.

## Sorumluluk Reddi

Bu yazılım **eğitim ve araştırma amaçlı** geliştirilmiştir. Ağ trafiği analizi, paket manipülasyonu ve siber güvenlik konularında teknik yetkinlik kazandırmayı hedefler.

Yazılımın kullanımından doğabilecek yasal sorumluluklar tamamen kullanıcıya aittir. Geliştirici, bu aracın kötüye kullanımından sorumlu tutulamaz.

## Kaynaklar ve Teşekkür

Bu proje aşağıdaki açık kaynak projelerin üzerine kurulmuştur:

* **[GoodbyeDPI](https://github.com/ValdikSS/GoodbyeDPI) by [ValdikSS](https://github.com/ValdikSS)**
* **[WinDivert](https://github.com/basil00/WinDivert) by [basil00](https://github.com/basil00)**

## Lisans

Bu proje **[GNU Affero General Public License v3.0 (AGPL-3.0)](https://github.com/kursatabayli/IHateDPI/blob/development/LICENSE)** ile lisanslanmıştır.
