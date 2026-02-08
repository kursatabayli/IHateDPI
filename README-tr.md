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

## Teknik Mimari ve Vaka Analizi

Projenin geliştirilme sürecinde elde edilen teknik veriler, DPI sistemlerinin çalışma prensipleri ve TLS el sıkışması (handshake) manipülasyonları üzerine detaylı bir rapor hazırlanmıştır.

İSS'lerin davranış analizlerini ve protokol seviyesindeki çözümlemeleri içeren bu teknik yazıya aşağıdan ulaşabilirsiniz:

* 📄 **[Teknik Analiz Raporunu Oku](docs/TECHNICAL_REPORT_TR.md)**

---

## Yapılandırma ve Sorun Giderme

Tüm motor ayarlarının, parametrelerin, paket manipülasyon stratejilerinin ve bağlantı sorunlarını giderme adımlarının detaylı açıklaması için lütfen ilgili rehbere göz atın:

👉 **[Yapılandırma ve Sorun Giderme Rehberi](docs/CONFIGURATION-tr.md)**

> **İpucu:** Eğer bağlantı sorunu yaşıyorsanız veya internet servis sağlayıcınıza (ISS) özel ince ayar yapmak istiyorsanız, bu rehber kritik bilgiler (DoH ayarı, parçalama stratejileri ve özel çözümler) içerir.

---

### Sistem Gereksinimleri ve Kısıtlamalar

* **Sadece 64-bit:** Proje yalnızca **64-bit (x64)** Windows işletim sistemlerini desteklemektedir. 32-bit (x86) sistemlerde çalışmaz.
* **IPv6 Desteği:** Şu an için sadece **IPv4** trafiği desteklenmektedir ve işlenmektedir. Eğer sisteminizde IPv6 aktifse, IPv6 üzerinden geçen trafik bu araç tarafından filtrelenmez veya manipüle edilmez (olduğu gibi iletilir). IPv6 desteği **geliştirilme aşamasındadır**.

---

## Harici Motor Desteği (External Engine)

IHateDPI Launcher, artık **evrensel bir başlatıcı (Universal Launcher)** yeteneğine sahiptir. Sadece kendi motorunu değil, parametre ile çalışabilen diğer popüler DPI atlatma araçlarını (GoodbyeDPI, Zapret, Byedpi vb.) da bu arayüz üzerinden yönetebilirsiniz.

Bu özellik sayesinde, farklı araçlar için ayrı ayrı siyah ekranlarla (CMD) uğraşmak yerine, hepsini tek bir modern arayüzden yönetebilirsiniz.

### Nasıl Yapılandırılır?

Ayarlar menüsünden **"Harici Motor (External)"** sekmesine giderek şu yapılandırmaları yapabilirsiniz:

1.  **Motor Seçimi (`.exe`):** Çalıştırmak istediğiniz herhangi bir CLI uygulamasını (örneğin `goodbyedpi.exe`) seçin.
2.  **Script/Config Desteği (`.cmd`, `.bat`):** Eğer elinizde hazır ayarların olduğu bir script dosyası varsa (örneğin `1_russia_blacklist_dnsredir.cmd`), bunu doğrudan seçebilirsiniz. Launcher, ilgili scripti otomatik olarak ayrıştırır ve çalıştırır.
3.  **Manuel Parametreler:** Script dosyası kullanmak istemiyorsanız, çalıştırma parametrelerini (örneğin `-9 --dns-addr 1.1.1.1`) doğrudan arayüzdeki kutucuğa girebilirsiniz.

> **GoodbyeDPI Kullanıcıları İçin:**
> Orijinal GoodbyeDPI klasörünü bilgisayarınızda herhangi bir yere indirin. IHateDPI ayarlarından `x86_64/goodbyedpi.exe` dosyasını seçin ve dilediğiniz `.cmd` dosyasını "Script Yolu" olarak gösterin. Hepsi bu kadar!

---

## Kurulum ve Kullanım

Bu araç "Taşınabilir (Portable)" olarak tasarlanmıştır. Herhangi bir kurulum gerektirmez.

1.  [Releases](https://github.com/kursatabayli/IHateDPI/releases) sayfasından en son sürümü bulun ve `IHateDPI-vx.x.x-win-x64.zip` (önerilen) veya `IHateDPI-Engine-vx.x.x-win-x64.zip` paketlerinden birini indirin.
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
