> Current release: Matchworks,20 levels,8×8board. Latest implemented rules and user decisions supersede historical drafts below. Cancelled Ğ experiments and developer hotkeys are removed.

# Mekanik kuralları v0.1

## 5 Ekim kullanıcı ek kapsamı — hamle kasası ve araç çarkı

Kazanılan bölümün kalan hamleleri tur başına bir defa kalıcı kasaya eklenir. Yeniden oynanıp kazanılan bölüm de eklenir; kayıp veya yarıda çıkış eklemez. 10 hamle bir dönüş bedelidir. Güncel kullanıcı kararıyla altı eşit dilim: +1 çekiç, +1 havai fişek, +1 mıknatıs, +2 çekiç, +2 havai fişek ve BOŞ. Ödül doğrudan stoğa eklenir; boş dilim ödül veya ücretsiz tekrar vermez. Eski ücretsiz dönüş hakkı kullanılmaz. İşlem bedeli ve ödül aynı envanter kaydında saklanır; animasyon sadece sunumdur. Gerçek para, reklam veya satın alınabilen hamle yoktur. Bu, önceki çarkın kapsam dışı tutulmasına kullanıcının açık ek isteğidir; denge kabulü bekler.

Bu dosyada `kesin` kurallar kullanıcıyla konuşulan tasarımı uygular; `taslak` yazan sayısal değerler oynanış testinde değişebilir.

## M01 — Tahta ve hamle

- Tahta 29 Eylül kullanıcı kararıyla **8 sütun × 8 satır**; engel koordinatları korunur. Koordinatlar `x=0..width-1` soldan sağa, `y=0..height-1` alttan üste.
- Bir karede `candy | bolt | motor | frozenGrease | empty` içeriklerinden biri; ayrıca zeminde bağımsız `oil: true/false` bulunabilir. Motor, donmuş yağ ve üzerindeki zemin yağı yerinde sabittir. Civatanın **başlangıç konumu** bölüm verisinde sabittir, oynanırken düşme kuralıyla yer değiştirebilir.
- Oyuncu yalnızca yatay/dikey komşu iki şekeri değiştirir. Hamle sonrası en az düz 3'lü veya yeni oluşmuş aynı renkten 2×2 kare varsa hamle geçerlidir; aksi hâlde taşlar geri döner ve hamle harcanmaz.
- Geçerli hamleden sonra eşleşme çözülür, boşluklar düşmeyle dolar, yeni şekerler deterministik üretim akışından gelir ve zincir eşleşmeler çözülür. Girişler çözümleme/animasyon süresince kilitlenir. Geçerli hamle kalmazsa tahta karıştırılır; sabit hedefler yerinden oynamaz.

## M02 — Eşleşme ve özel parça üretimi

| Oluşan şekil | Tahtada oluşan parça | Kalıcı yapboz katkısı |
| --- | --- | --- |
| Düz 3 | Normal temizlik | Yok |
| Düz 4 | **Ray Kesici**; çizginin yönüne göre satır veya sütun etkisi | Ray yapbozuna +1/4 |
| Aynı renkten yeni 2×2 | **Çekiç**; mevcut 3×3 etki | Çekiç yapbozuna +1/4 |
| Düz 5 veya daha fazla | **Elektromıknatıs**; seçilen rengi temizler | Mıknatıs yapbozuna +1/5 |

- Öncelik: düz 5+ > düz 4 > 2×2 > düz 3. Bir oyuncu hamlesindeki örtüşen şekil tek özel parça üretir; birden fazla bağımsız şekil ayrı ayrı üretebilir. L/T biçimi için ayrı güç şimdilik yoktur.
- Özel parça eşleşmedeki yer değiştirilerek varılan karede yaratılır; o kare engelle doluysa eşleşmedeki uygun bir şeker karesi kullanılır. Tahtada belirdiği anda **bir defa** ilgili yapboz ilerlemesi kaydedilir; daha sonra kullanılıp kullanılmaması veya bölümün kaybedilmesi bunu geri almaz.
- Hem ilk eşleşmede hem zincir düşüşünde gerçekten **üretilen** özel parça katkı verir. Başlangıçta hazır yerleştirilmiş özel parça ve envanter aracından yerleştirilen parça katkı vermez. Aynı özel parça birden fazla kez sayılamaz.
- Tahtadaki özel parça, komşu bir şekerle yer değiştirildiğinde normal 3'lü zorunluluğu olmadan etkinleşir. Ray'in yönü onu yaratan 4'lü çizgidir; Mıknatıs'ın rengi değiştirildiği komşu şekerin rengidir; Çekiç kendi konumunu merkez alır. Etkinleştirme bir hamle harcar.
- Ray Kesici satırı/sütunu temizler; yolundaki civata ve donmuş yağa bir darbe verir. Çekiç şimdilik eski çözücünün 3×3 kuralını kullanır: açık zemin yağını temizler, donmuş yağ kabuğuna bir darbe verir; kabuk kırılınca altındaki yağ kalır. Eski çözücü modeli saklanır, oyunda üretilmez. Elektromıknatıs oyuncunun seçtiği rengin tüm şekerlerini siler; bunlara komşu her civata ve donmuş yağa bir darbe, motora toplam en çok bir darbe verir. Görsel efektler basit ölçek/renk/kısa hareketle yapılır.
- Güç başka güce değerse temas anında diğer gücün animasyonunu başlatır; ilk gücün tamamen bitmesini beklemez. Temasla bağlı animasyon dalgası bittikten sonra boşluklar dolar. Henüz temas almamış bağımsız güçler arasında dolum korunur; bekleyen canlı güçlerin konumu düşüşle güncellenir. Civata sütun çıkışı da vurduğu gücü çalıştırır. Otomatik mıknatıs komşu şekerlerden deterministik rastgele renk seçer, komşu yoksa tahtadan seçer.
- Roket+roket: varılan konumda tam satır/sütun. Çekiç+çekiç: 5×5. Çekiç+roket: roket yönünde üç tam satır/sütun.
- Mıknatıs+roket/çekiç: en fazla 10 normal şeker ilgili güce dönüşür; roket yönleri rastgele karışıktır. Geçici karar: hemen sırayla çalışırlar; Ali'nin zamanlama onayı bekleniyor. Dönüşüm stok kazandırmaz. Çift mıknatıs birleşimi tüm tahtayı üst sol → üst sağ, bir alt satırda sağ → sol şeklinde yılan gibi tarayarak temizler; sonra normal düşüş ve otomatik eşleşmeler çözülür.

### Alev alma / zincir sayacı — 4 Ekim güncel karar

- Son kullanıcı kararı önceki kuralı değiştirir: her güç etkinleşmesi zincire +1 verir. Mıknatısın oluşturduğu roket/çekiçler ve bir roketin uzaktaki başka aracı tetiklemesi dahildir. Tek bir gücün sildiği her şeker ayrı zincir değildir. Otomatik eşleşme grupları da +1 katkısını korur; yeni hamlede sıfırlanır.
- Güçlerin çalışması bittikten sonra düşüş/refill sonucu şekerler oyuncu dokunmadan kendi kendine eşleşirse sayaç başlar. Her ayrı otomatik eşleşme grubu sayacı 1 artırır: ilk grup 1, sonraki 2, 3, 4, 5… Aynı düşüşte birden fazla bağımsız grup varsa her biri ayrı sayılır. İlk oyuncu takasıyla doğrudan kurulan eşleşme otomatik zincir değildir.
- `ZİNCİR 1×`, `2×`, `3×` gösterimi görsel geri bildirimdir; bu aşamada puan veya araç ödülünü çarpmaz. Yeni oyuncu hamlesinde sayaç sıfırlanır. Yazı/alev/ses assetleri hazır olunca gösterim oyuna bağlanacaktır.

## M03 — Hareketli civata

- `rust=2` koyu pas, `rust=1` açık pas, `rust=0` tamam. Her **ayrı eşleşme grubu**, komşu civataya en çok 1 darbe verir. Aynı eşleşmedeki üç şeker üç darbe sayılmaz.
- `rust=0` olunca civata normal düşmeyi bırakır, kendi sütunundan yukarı yükselir ve geçtiği sütundaki şekerleri temizler. Çıkış animasyonu tamamlanınca bölüm civata hedefine +1 yazılır. Üstteki makinede uygun boş yuvaya yerleşir.
- Civata oyuncuyla doğrudan değiştirilemez; olağan düşme sırasında aşağı inebilir. Çıkış sütun temizliği zemindeki yağı doğrudan silmez. Oluşan yeni eşleşmeler kendi kurallarıyla yağı silebilir.

## M04 — Yağ ve donmuş yağ

- `oil=true` zemine bağlıdır; şeker/civata düşüşüyle taşınmaz. O karede eşleşen veya mıknatısla temizlenen şekerin altındaki açık yağ silinir. Ray yolundaki, çekiç alanındaki açık yağ da temizlenir. Yalnız komşu eşleşme veya civata sütun süpürmesi yağı temizlemez.
- Donmuş yağ sabit içeriktir; bitişiğindeki her ayrı eşleşme veya özel etkinin darbesiyle kırılır (`shellHp=1`, taslak). Kırılınca kare boşalır, alttaki `oil=true` kalır ve boşluk düşmeyle dolar. Sonraki gerçek eşleşme, ray/çekiç veya üstündeki şekeri silen mıknatıs açık yağı temizleyebilir.
- Zemin yağı hedefi başlangıçtaki bütün yağlı karelerin sayısıdır; kabuk altındaki yağ da buna dahildir. Donmuş yağ bloklarının kabukları ayrı hedef/sayaçtır. Kabuk kırılınca yalnız blok hedefi ilerler; altında kalan yağ ayrı temizlenir. Bölüm bitişi hem blok hem zemin yağı hedeflerinin tamamlanmasını bekler.

## M05 — Sabit motor ve bölüm bitişi

- Aynı bölümde birden fazla motor bulunabilir. Her motor ayrı dolum sayacı ve patlama alanı taşır; aynı eşleşme grubu her temas ettiği motora en çok bir darbe verir. Bütün motorların eşikleri tamamlanmadan motor hedefi tamamlanmaz. Üst tabloda dolum toplamı gösterilir; örneğin iki varsayılan motor 0/10 ile başlar.
- Motor sabit içeriktir; komşusunda oluşan **her ayrı eşleşme grubu** bir darbe verir. Bir özel etkinin motorla etkileşimi olay başına en çok 1 darbedir. `hitsRequired=5` varsayılandır, bölüm verisinde değişebilir.
- 3 Ekim kararı: Beşinci darbeden sonra motor hemen patlar ve kendi kapladığı hücrelerden kaldırılır. Motorun bir hücre genişliğindeki çevre halkasında, köşeler dahil, şekerler temizlenir. Bu halkadaki özel parçalar zincire alınır; diğer engeller ve zemindeki yağ doğrudan silinmez. Motorun yeri ve boşalan çevresi düşme/yeni şekerlerle dolar; zincirler çözülür. Patlama bir kez gerçekleşir. Varsayılan eşik 5'tir; bölümdeki `hitsRequired` verisi korunur.
- Motor patlamış olsa da civata ve yağ hedefleri tamamlanmadan bölüm kazanılmaz. Motor olmayan bölüm tanımlı hedefler tamamlanınca kazanılır.
- Son hamlede hedefler çözümleme/zincir animasyonları bittikten sonra değerlendirilir. Hedefler tamamsa kazanma, değilse kaybetme. Motor hazır olsa da kalan yağ hedefini atlayamaz.

## M06 — Kalıcı araç yapbozları ve stok

- Her tür kendi sayacını taşır: `ray 0..3/4`, `hammer 0..3/4`, `magnet 0..4/5`, ayrıca kullanılabilir stok `stock >= 0`. Önceki kayıtlarla uyum için çekiç sayacı kodda geçici olarak `Solvent` anahtarını kullanır.
- Dördüncü Ray veya Çekiç üretiminde ilgili parça sayacı sıfırlanır ve stok +1 olur. Beşinci Mıknatıs üretiminde de aynı şey olur. Fazla üretim kalan bakiyeyle devam eder.
- Sayaç ve stok **özel parça yaratıldığı anda** yerel kayıtla kalıcı olur; bölüm kaybı, tekrar başlatma ve oyundan çıkma geri almaz. Bir stok kullanıldığında geçerli hedef seçilirse stok düşer ve kaydedilir; bölüm kaybedilirse kullanılan stok geri dönmez.
- 3 Ekim kararı: Araç, normal bir şekeri seçilen özel parçaya dönüştürür ve tahtada bırakır; hemen etkinleşmez. Mevcut özel parça veya engel üzerine yazılamaz. Araçla verilen özel parça yeni yapboz katkısı üretmez. Kazanılan stok tüm bölümlerde kullanılabilir.
- Yerleştirme yalnızca **bir stok** harcar; hamle harcamaz ve düşme/zincir başlatmaz. Sonraki komşuyla değiştirme hamlesi parçayı etkinleştirir ve bir hamle harcar. Ray'in yönü yerleştirmede seçilir; mıknatısın hedef rengi daha sonraki takasla belirlenir. Hedef geçersizse stok korunur.

9 Ekim: Motor alanında zemin yağı bulunabilir. Motor patladığında kendi kapladığı hücrelerdeki yağ da temizlenir. Çevre halkasındaki yağ ve başka motorun altındaki yağ patlamayla doğrudan temizlenmez.

9 Ekim güncel karar: Motor patlamasının çevre halkası vida/somunlara bir pas katmanı hasarı ve donmuş yağ bloklarına bir kabuk hasarı verir. Kabuğun altındaki/çevredeki açık yağ kalır; motorun kendi kapladığı hücrelerin yağı temizlenir.


12 Ekim kullanıcı kararı — çift mıknatıs / motor: iki mıknatısın tüm tahta temizliğinde her motorun çevre halkasında gerçekten silinen her şeker ayrı +1 motor darbesi verir. Aynı şeker aynı motorun birden çok hücresine komşu olsa da yalnız bir kez sayılır; her motor bağımsız hesaplanır. Köşe komşuları dahildir. Hazır olan motorlar aynı güç çözümünde patlar. Diğer olayların motor başına tek darbe kuralı korunur. Çift mıknatıs otomatik zafer vermez; kalan hedefler normal kurallarla değerlendirilir.


13 Ekim 2026 — Ali onayı, dikey + çapraz dolum
Başlangıç seedleri/yerleşimleri ve hamle sayıları korunur. Oyun sırasındaki dolum önce dikeydir. Dikey üst kaynağı sabit motor/donmuş yağla kapanan boşluklar sağ/sol üst normal veya özel şekerden beslenebilir; engelin köşesinden geçmek için kaynak sütununun hedef satırında sabit engel olmamalıdır. Civatalar sütunlarında kalır; yağ zeminde kalır. İki uygun yön varsa koordinat paritesiyle deterministik öncelik seçilir. Yeni parçalar önce üstten açık bölgeleri besler; tamamıyla erişilemeyen cepler eski yerel üretimle doldurulur. Başlangıç üretimine dokunulmadı; oyun içi spawn sırası ve zincirler değişebilir. Ali tüm bölümleri tekrar oynayıp hamle ayarlarını bildirecek.
