> Current release: Matchworks,20 levels,8×8board. Latest implemented rules and user decisions supersede historical drafts below. Cancelled Ğ experiments and developer hotkeys are removed.

# Sistem etkileşimleri ve çözüm sırası

Tek eşleşmenin kaç darbeye dönüştüğü ve eş zamanlı animasyonlar burada kesinleşir. Koda geçerken bu olay sırasını koru.

## I01 — Geçerli oyuncu hamlesi

1. Girdi kilitlenir; iki şekeri değiştir.
2. Değişen hücre çevresinde düz 3+/yeni 2×2 ara; yoksa geri al ve kilidi aç.
3. Geçerliyse hamle hakkını bir kez azalt. Örtüşen şekilleri `Mechanics.md` önceliğiyle seç, özel parça üret ve yapboz sayacını o anda kaydet.
4. Eşleşen şekeri temizlerken yalnızca şeklin **bulunduğu karedeki** yağı temizle. Her eşleşme grubunun komşu olduğu her civata/donmuş yağ/motora **en fazla bir** darbe uygula.
5. Kırılan donmuş yağları kaldır, zemin yağlarını bırak. Pası biten civata varsa önce yerçekimi/dolum uygula: civata da boşluğa düşer, üzerindeki sütun yeni şekerlerle dolar. Ardından civatayı yeni konumundan yukarı çıkarıp dolmuş sütunun şekerlerini sil; vurulan güçleri sıraya al.
   Motor eşiğe ulaştıysa motoru kaldır ve bir hücrelik çevre halkasının şekerlerini temizle; halkadaki güçleri sıraya al. Temasla birbirini tetikleyen güçler aynı animasyon dalgasında çalışır; dolum bu dalganın sonunda yapılır. Bağımsız güç/patlama adımları arasında düşme/refill yap. Bekleyen canlı özel parçaların konumlarını düşüşleriyle güncelle; önceden vurulan güç kayıtlı vurulma konumunda çalışır.
6. Önce sabit içeriklerin ayırdığı sütun bölgelerinde dikey düşüşü uygula. Üstü sabit engelle kapanan boşluğa açık köşeden sağ/sol üst şeker kayabilir; dikey kaynak önceliklidir, civata çapraza kaymaz. Üstten bağlı sütunlara yeni şeker üretip dikey/çapraz akışı kararlı olana kadar sürdür. Erişilemeyen kapalı cepler için yerel dolum korunur. Üretim deterministiktir; yeni akış spawn sırasını değiştirebilir. Zincir eşleşmeleri çöz, olay tekrarı için üst sınır/güvenlik kontrolü koy.
7. Tahtayı kararlı duruma getir, hedefleri ve hamleyi değerlendir; gerekiyorsa yalnız şekerleri karıştır; girdi kilidini aç veya sonuç ekranına geç.

## I02 — Özel parça ve engel matrisi

| Olay | Civata | Motor | Donmuş yağ | Açık zemin yağı |
| --- | --- | --- | --- | --- |
| Komşu normal eşleşme | Grup başına 1 pas | Grup başına 1 vuruş | Grup başına 1 kabuk | Yalnız üstündeki eşleşen kare temizlenir |
| Ray Kesici | Yolunda 1 pas | Yanından/yolundan etki alırsa en çok 1 | Yolunda 1 kabuk | Yolundaki açık yağı temizler; yeni açılan yağ kalır |
| Çekiç (3×3; önceki çözücü kuralı) | Kapsıyorsa 1 pas | Kapsıyorsa en çok 1 | Kapsıyorsa 1 kabuk | Kapsanan açık yağ silinir; yeni açılan yağ kalır |
| Elektromıknatıs | Silinen renklerin komşuluğunda bir defa 1 pas | Tüm olayda en çok 1 | Silinen renklerin komşuluğunda bir defa 1 kabuk | Sildiği şekerlerin altındaki açık yağ temizlenir; yeni açılan kabuk altı yağ kalır |
| Civatanın sütun çıkışı | Diğer civataya darbe vermez | Darbe vermez | Darbe vermez | Temizlemez |

Bir özel etkinin aynı hedefe beş şekerle temas etmesi beş vuruş değildir. Her hedef/olay için benzersiz kimlik üzerinden tek etki uygula. 3 Ekim kararıyla motor patlaması beşinci vuruşta gerçekleşen gerçek bir tahta olayıdır; motor ayak izi boşalır, çevre şekerleri temizlenir ve refill olur. Motorun patlaması diğer hedefleri otomatik tamamlamaz.

## I03 — Kararlılık ve sınır durumları

- Yalnız şekerler yer değiştirir. Sabit motor/donmuş yağ düşmeyi ve swap'i engeller; civata düşebilir ama swap edilemez.
- Civatanın yükselişine iki başka olay sebep olursa yalnız bir kez sayılır ve bir kez makineye gider.
- Son hamlede oluşan zincir ve civata çıkışları tamamen çözülmeden kaybetme gösterilmez.
- Başlangıç tahtasında hazır otomatik eşleşme olmamalıdır; geçerli oyuncu hamlesi olmalıdır. Şekil olarak 2×2 başlangıçta varsa da otomatik tetiklenmez, editör bunu uyarı olarak gösterir.
- Kayıt; yapboz artışı, stok kazanımı ve geçerli araç kullanımında tutulur. Yeniden yükleme aynı olaydan ikinci kez ödül üretmemelidir.
- Geçerli hareket kalmayınca motor, civata, yağ ve donmuş yağ aynen kalır; yalnız sıradan şekerler yeniden karılır. Çözülebilir durum bulunamazsa bölüm doğrulama hatası verilir.

## I04 — Araç arayüzü

Yarı saydam ikon 0/N ile parçalı dolum gösterir. N/N olduğunda küçük bir dolum animasyonu oynar, stok +1 ve sayacın kalanı gösterilir. Stok 0 iken araç etkinleşmez. Stok >0 iken seçime basılır, geçerli hedef vurgulanır, iptal mümkündür. Geçerli uygulamada stok hemen kaydedilir. Hedefsiz dokunma harcama yapmaz.

Tahtadaki özel şekli komşu şekerle değiştirmek bir hamle harcar. Stok aracını normal şekere yerleştirmek yalnız bir stok harcar; yeni özel parça hücrede bekler. İptalde veya mevcut özel parçaya/engele dokunmada hamle ve stok harcanmaz. Düşme/zincir çözümü yerleştirmede değil sonraki etkinleştirmede çalışır.

## Sonra değerlendirilecek, bu sürümde uygulanmayacak

Zar, yeni üretim şekerleri ve gerçek ekonomi kapsam dışıdır. Çift mıknatıs, `Mechanics.md` M02'deki satır satır tam-tahta taramasıdır. Diğer özel+özel birleşimleri de M02'ye göre uygulanır. Her temas dalgasından sonra düşme/refill olur; tüm bağımsız güç kuyruğu beklenmez. Normal eşleşmeler güç kuyruğu bittikten sonra çözülür.

9 Ekim: Motor çevre halkasında şekerleri temizler, özel araçları zincire alır, vida/somun pasını bir kat azaltır ve donmuş yağ kabuğuna bir hasar verir. Motorun altındaki yağ temizlenir; çevredeki açık yağ ve kırılan kabuğun altındaki yağ kalır.
