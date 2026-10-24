> Current release: Matchworks,20 levels,8×8board. Latest implemented rules and user decisions supersede historical drafts below. Cancelled Ğ experiments and developer hotkeys are removed.

# 8 Ekim kapsam güncellemesi

Aktif bölüm sayısı 20. 11–20 mevcut 1–10 verilerinden türetilmiş düzenlenebilir taslaklardır; nihai engel konumları ve denge Ali tarafından belirlenecek. Aşağıdaki eski 10 bölüm hedefi önceki kapsamı anlatır.

# Bölüm verisi, editör ve 10 bölümlük içerik planı

## Sahiplik ve tekrar kuralı

**Nihai yerleşimi Ali yapar:** her bölümün başlangıçtaki civata, motor, yağ ve donmuş yağ koordinatları. Codex yalnız taslak önerebilir; Ali onaylamadan içerik olarak kaydedemez. Aynı bölümün her yeniden girişinde aynı başlangıç yerleşimi yüklenir. Şekerlerin başlangıç düzeni ve üretim dizisi de bölüm verisindeki seed/diziden tekrarlanabilir olur. Oyuncunun farklı hamleleri farklı sonuç doğurabilir; 'aynı oyun' vaadi aynı ilk durum ve aynı kurallardır.

## Önerilen veri alanları

`id`, `width`, `height`, `moves`, `colors`, `initialCandyLayout`, `spawnSeedOrSequence`, `fixedObjects[]`, `floorOilCells[]`, `boltRust[]`, `motorHitsRequired`, `chapterMachineId`, `displayTitle`. `fixedObjects[]` içindeki nesnelerin koordinatı ve türü kaydedilir; zemin yağı koordinatı ayrı tutulur. Nihai fiziksel veri biçimi JSON veya eşdeğer dış veri dosyası olabilir; Unity Inspector'da elle 100+ hücre doldurulmaz.

Yalnız tasarım için örnek: `width=8`, `height=9`, `moves=18`, `colors=[red,blue,green,yellow,purple]`, `fixedObjects=[{type:motor,x:4,y:5},{type:frozenGrease,x:2,y:3}]`, `floorOilCells=[{x:2,y:3},...]`. **Bu örnek bir bölüm yerleşimi değildir.** JSON'a dönüştürürken şema doğrulaması ve kararlı sırayı uygula.

## Görsel bölüm editörü gereksinimi

Unity içinde editör penceresi veya oyun içi geliştirme modu: genişlik/yükseklik ve hamle ayarı; ızgarada fırça seçerek `bolt rust=2`, `motor`, `frozenGrease`, `oil`, başlangıç şekeri yerleştirme/silme; koordinat, sayaç ve hedef özeti; kaydet/yükle; `Play This Level` ile hemen deneme. Sabit nesneyi dolu kareye koyma, iki motor çakışması, erişilemeyen hedef, başlangıç otomatik eşleşmesi ve hamlesiz tahta doğrulama uyarısı. Her JSON sürüm kontrollü dosya; tasarımcı değişikliği kaydedince oynanışa yansır.

## Öğretim sırası (konumlar boş; Ali dolduracak)

| Bölüm | Önerilen yeni öğrenme | Nihai yerleşim |
| --- | --- | --- |
| 01 | Eşleşme + bir civatanın iki pas aşaması | Ali belirleyecek |
| 02 | İki civata, çıkış sütunu seçimi | Ali belirleyecek |
| 03 | Açık zemin yağı | Ali belirleyecek |
| 04 | Civata + yağ + düz 4 veya 2×2 özel parça | Ali belirleyecek |
| 05 | Sabit motorun 5 darbesi; ilk makine çalıştırma | Ali belirleyecek |
| 06 | Donmuş yağın iki aşaması | Ali belirleyecek |
| 07 | Donmuş yağ + civata etkileşimi | Ali belirleyecek |
| 08 | Beşli mıknatıs ve yaygın renk dağılımı | Ali belirleyecek |
| 09 | Az hamleyle çoklu hedef önceliği | Ali belirleyecek |
| 10 | Civata + yağ + donmuş yağ + motor finali | Ali belirleyecek |

## Dengeleme notu

Codex ilk renk dizilimi, spawn seed'i ve hamle sayısını **önerir**, fakat otomatik 'kolay/zor' kararı kesin değildir. Doğrulayıcı kurallar, mümkünse çoklu seed/ajan denemesi ve gerçek oyuncu testleri gerekir. Sabit başlangıçla oynanırken kaç oyuncunun bitirdiği, kaç hamle kaldığı ve nerede takıldığı kaydedilir. Denge ayarı Ali'nin bölüm tasarımını izinsiz değiştirmez; önerilen revizyon ayrı gösterilir.

9 Ekim: Bir bölümde birden fazla motor ankrajına izin verilir. Motor alanları birbirine değebilir, çakışamaz. Her ankraj kendi ObstacleState dolum eşiğini kullanır; her motor kendi MotorOrientation yönünü kullanır (1 yatay, 2 dikey). Eski kayıtlardaki 0 değeri bölümün MotorRotated yönünü kullanır. Engel konumlarını Ali belirler.

13 Ekim — çapraz dolum sonrası kullanıcı denemesine göre onaylı hamle ayarları: [MoveTuning_20261013.md](MoveTuning_20261013.md). SpawnSeed ve Cells korunur; kullanıcı tüm 20 bölümü oynadığını bildirdi, bu kayıt tek oynanış geri bildirimidir.
