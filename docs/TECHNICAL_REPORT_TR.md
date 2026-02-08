# Derin Paket İnceleme (DPI) Sistemlerinin Anatomisi: TLS Handshake Manipülasyonu Üzerine Bir Vaka Analizi

*(Araştırma Tarihi: Aralık 2025)*

---

## 1. Giriş
`IHateDPI` projesinin geliştirilme sürecinde, farklı İnternet Servis Sağlayıcı (İSS) altyapılarında kullanılan Derin Paket İnceleme (DPI) sistemleri üzerinde kapsamlı bir veri toplama ve analiz çalışması yürütülmüştür. Yapılan yoğun saha testleri ve stres testleri, bu sistemlerin çalışma mantığını ve sahip oldukları yapısal kısıtları (trade-offs) ortaya koymuştur. Bu teknik raporda; hangi protokol manipülasyon yöntemlerinin ağ trafiğinin sürekliliğini sağladığı, hangilerinin başarısız olduğu ve bu sonuçların altında yatan teknik nedenler incelenecektir.

## 2. DPI Sistemlerinin Çalışma Mekaniği
Saha araştırmaları göstermektedir ki; mevcut DPI mekanizmaları, tüm ağ trafiğini analiz etmek yerine, özellikle TLS (Transport Layer Security) el sıkışmasının ilk adımı olan **"Client Hello"** paketini yakalamaya odaklanmıştır. DPI sistemlerinin elindeki yegane ayırt edici veri seti bu pakettir.

### 2.1. "Client Hello" Paketinin Rolü
Basit bir ifadeyle "Client Hello", istemcinin (tarayıcı/cihaz) sunucu ile şifreli bir iletişim başlatma talebidir. Paket içerisinde, DPI sistemleri için kritik öneme sahip olan **SNI (Server Name Indication)** verisi bulunur.

### 2.2. SNI ve Filtreleme Mantığı
SNI alanı, bağlanılmak istenen hedef alan adını (örn: `google.com`) şifresiz (plaintext) olarak barındırır. DPI cihazları, ağ akışı (network flow) içerisinden geçen "Client Hello" paketlerini süzer ve SNI değerini okur. Eğer bu değer, sistemde tanımlı bir kural setiyle (blacklist) eşleşirse, bağlantı **TCP Reset** veya **Packet Drop** yöntemiyle sonlandırılır.

## 3. Modern Web Mimarisi: Merkeziyetçilik Paradoksu
Günümüz web ekosistemi, Cloudflare, Google ve Amazon gibi devasa içerik dağıtıcılar (CDN) üzerinde yüksek oranda merkezileşmiştir. Normal şartlarda internetin bu denli tekelleşmesi bir risk gibi görünse de, DPI tabanlı filtreleme mekanizmaları söz konusu olduğunda bu durum, denetleyici sistemler için **teknik bir çıkmaza** dönüşmektedir.

Geçmiş ağ topolojilerinde IP adresleri **müstakil evler** gibiydi; her adresin arkasında genellikle tek bir sunucu bulunurdu. Ancak modern bulut mimarisinde IP adresleri, binlerce dairenin (web sitesinin) bulunduğu devasa **gökdelenlere** dönüşmüştür.

Bugün tek bir IP adresine erişim engeli getirmek; o IP'yi paylaşan binlerce ilgisiz siteyi, e-ticaret platformunu ve kurumsal sistemi de erişilemez hale getirmek (Collateral Damage) demektir. Bu "Yan Hasar" riski, IP tabanlı engellemeyi imkansız kılmış ve DPI sistemlerini trafiği IP'ye göre değil, **SNI (Server Name Indication)** içeriğine göre filtrelemeye mecbur bırakmıştır.

## 4. Protokol Obfuskasyonu (Gizleme) Yöntemleri
DPI sistemleri, ağ performansını (throughput) düşürmemek adına tüm paketleri derinlemesine inceleyemez. Genellikle belirli bir imza (örn: ilk bayt `0x16` mı?) ararlar. Eşleşme varsa SNI okunur, yoksa akış serbest bırakılır. TLS Handshake tamamlandıktan sonra trafik şifrelendiği için DPI takibi teknik olarak sona erer.

Bu bağlamda tüm strateji; DPI sisteminin denetim mekanizmasını atlatıp, "Client Hello" paketini sunucuya ulaştırmaktır.

### 4.1. Yöntem 1: Sahte (Fake) Client Hello Enjeksiyonu
Saha testlerinde en kararlı (stabil) çalışan yöntem, sisteme gerçek paketten önce yanıltıcı bir paket sunmaktır.

* **Mekanizma:** Gerçek istekten hemen önce, DPI'ın "güvenli" olarak etiketleyeceği bir alan adı (örn: `google.com`) içeren sahte bir "Client Hello" paketi enjekte edilir.
* **Sonuç:**
    1.  DPI sistemi sahte paketi analiz eder ve TCP akışını "Güvenli" olarak işaretler.
    2.  Sistem durumu (state) kaydettiği için, hemen arkasından gelen gerçek "Client Hello" paketi denetime takılmadan geçer.
    3.  Sunucu sahte paketi reddeder, gerçek paketle iletişimi sürdürür.

### 4.2. Yöntem 2: TCP Segmentasyonu ve "Out-of-Order" Manipülasyonu
Standart TCP parçalama (Fragmentation) yöntemleri, modern DPI cihazlarının "Packet Reassembly" (Paket Birleştirme) yetenekleri nedeniyle etkisini yitirmiştir. Bu araştırmada, DPI tampon belleğini (buffer) hedef alan hibrit bir yöntem geliştirilmiştir.

* **Mekanizma:**
    1.  "Client Hello" paketi parçalanır. İlk parça (Header) sunucuya gönderilir.
    2.  Hemen ardından, ikinci parçanın TCP Sıra Numarasına (Sequence Number) sahip **Anlamsız Veri (Garbage Data)** gönderilir.
    3.  **DPI Davranışı:** DPI, ilk parça ile bu anlamsız veriyi birleştirir. Ortaya çıkan veri anlamsız olduğu için kural setleriyle eşleşmez.
    4.  **Sunucu Davranışı:** Sunucu, TCP protokolü gereği hatalı (checksum failure) veya anlamsız olan bu veriyi yok sayar.
    5.  Gerçek ikinci parça gönderildiğinde, sunucu bunu ilk parçayla birleştirir ve oturum kurulur.

## 5. Vaka Analizi: Tip-B Ağ Altyapısı (Strict Protocol Enforcement)
Saha testlerinde, belirli bir İSS altyapısında (Raporda **"Tip-B"** olarak anılacaktır) yukarıda belirtilen "Çöp Veri" yönteminin çalışmadığı tespit edilmiştir. Bu durum üzerine sistemin davranışını çözümlemek adına derinlemesine bir analiz süreci başlatılmıştır.

### 5.1. Stres Testi ve Anomali Tespiti
Tip-B ağında, standart boyutlu çöp verilerin sistemi etkilemediği görülmüştür. Sistemin tepkisini ölçmek ve olası tampon taşma (buffer overflow) açıklarını tespit etmek adına, enjekte edilen çöp verinin boyutu kademeli olarak artırılmıştır (Payload Inflation).

Beklenti, veri boyutu belirli bir eşiği aştığında sistemin zorlanması ve trafiği kesmesiydi (Fail-Closed). Ancak Tip-B altyapısında şaşırtıcı bir **"Tepkisizlik" (Silent Discard)** durumu gözlemlenmiştir. Veri boyutu ne kadar artırılırsa artırılsın bağlantı kopmamış, ancak manipülasyon da başarılı olmamıştır. Bu durum, sistemin basit bir boyut kontrolü yapmadığını kanıtlamıştır.

### 5.2. Kritik Keşif: Yapısal Bütünlük Analizi (Structural Integrity Check)
Sistemin çalışma mantığını çözmek için sahte paketler üzerinde tersine mühendislik uygulanmış; içi boş paketlerden başlanarak adım adım RFC standartlarına uygun paketler enjekte edilmiştir.

**Bulgular:**
Tip-B DPI sistemi, sadece boyuta veya basit imzalara değil, **İçerik Tutarlılığına (Content Consistency)** bakmaktadır.

TCP yükü parçalanıp araya çöp veri sıkıştırıldığında, DPI sistemi ortaya çıkan verinin **TLS Record Layer** standartlarına uymadığını (Malformed Packet) tespit etmektedir. Sistem bu veriyi bir "manipülasyon girişimi" olarak değil, doğrudan **"Gürültü/Bozuk Veri"** olarak sınıflandırmaktadır. DPI, "bozuk" olarak etiketlediği veriyi işleme almamakta ve durum tablosuna (State Table) kaydetmemektedir.

**Sonuç:** Sistem yalnızca RFC standartlarına birebir uyan ve yapısal bütünlüğü bozulmamış, gerçekçi "Client Hello" paketlerini işleme almaktadır. Bu nedenle, Tip-B ağlarda rastgele veri enjeksiyonu etkisizdir; manipülasyon ancak geçerli protokol yapılarıyla (Valid TLS Structure) mümkündür.

## 6. Sonuç ve Gelecek Projeksiyonu: ECH (Encrypted Client Hello)
Bu araştırma, DPI atlatma yöntemlerinin evrensel olmadığını; ağ cihazlarının konfigürasyonuna (Stateless vs Deep Stateful) göre değişkenlik gösterdiğini kanıtlamıştır.

Mevcut durumda en kararlı yöntem **Sahte (Valid) Client Hello** enjeksiyonudur. Ancak protokol seviyesindeki nihai çözüm, IETF tarafından geliştirilen **Encrypted Client Hello (ECH)** standardıdır. ECH, SNI bilgisini şifreleyerek DPI analizini teknik olarak imkansız kılmayı hedeflemektedir. `IHateDPI` projesi, ECH adaptasyon sürecini izlemeye devam edecektir.

---

### Yasal Uyarı (Disclaimer)
*Bu döküman ve ilgili GitHub deposu, ağ protokollerinin (TCP/IP, TLS) çalışma mantığını anlamak ve siber güvenlik araştırmaları kapsamında analiz yapmak amacıyla hazırlanmıştır. Bilgiler, ağ yöneticilerinin kendi altyapılarını test etmeleri ve protokol davranışlarını anlamaları içindir.*
