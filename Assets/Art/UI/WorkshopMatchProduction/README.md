# Workshop Match — UI, VFX ve ses kaynak paketi (grup 1–9)

3 Ekim 2026 üretimi. Bu klasör kaynak görsel/font/ses paketidir. 4 Ekim'de `WorkshopMatchUiCatalogBuilder` ve runtime kodu bu kaynaklara bağlandı; Editor'da katalog yeniden oluşturma ve Play doğrulaması henüz bekliyor. Ali'nin görsel/işitsel kabulü ve gerçek telefon yerleşimi ayrıca yapılacaktır.

## Grup 1 — font ve rakamlar

- `Group01_Font/Nunito-Variable.ttf`: Türkçe karakterleri içeren, değişken ağırlıklı gerçek font. Kaynak Google Fonts Nunito; yeniden dağıtım lisansı yanındaki `OFL.txt`.
- `glyphs_dark.png`, `glyphs_cream.png`: şeffaf, aynı 112 px hücre ızgaralı alternatif rakam/işaret/yazı atlasları. Dizin `glyph_manifest.json` içinde; 0–9 ve `/`, `+`, `×` yan yana kurulabilir. 1–50 için ayrı PNG gerekmez.
- `font_style_preview.png`: `HAMLE 21`, `0/5`, `ZİNCİR 3×` ile iki renk yönünün kontrol görseli. Bu önizleme oyuna konacak atlas değildir.
- Kullanım: gerçek fontu dinamik metinlerde öncelikli kullan; atlas, sprite-font gerekirse hazırdır. HUD ve harita sayıları arka plan resmine gömülmemiştir.

## Grup 2 — üst HUD

- `Group02_HUD/hud_full_width_source.png`: 2172×724 RGBA, boş ahşap/pirinç çerçeve. Bir dar hamle alanı ve üç hedef alanı içerir. Metinler ve mevcut somun/vida, yağ, motor ikonları ayrı katmana konmalıdır.
- Bu bir yüksek çözünürlüklü **kaynak** resimdir. Farklı telefon oranları için Unity'de nine-slice/ayrık köşe bölgeleri hazırlanmalı; bütün PNG'yi gelişigüzel esnetmek kabul edilmiş uygulama değildir.

## Grup 3 — araç ilerleme

- `Group03_Progress/segment_empty_source.png` ve `segment_filled_source.png`: tek segmentin boş/dolu kaynakları. Havai fişek ve çekiçte 4, mıknatısta 5 adet tekrarlanır; eşik kodu değiştirilmedi.
- Kaynak PNG'lerin şeffaf boşlukları ve resim içi sınırları birebir aynı değil. Runtime'da ortak segment dikdörtgenine kırpılarak/oturtularak kullanılmalı veya son hizalama yapılmalı. Şu an doğrudan üst üste konmuş piksel mükemmel durum atlası diye sunulmaz.
- Stok rakamı ve araç ikonu görsele gömülmez; mevcut araç ikonları ayrı kalır.

## Grup 4 — ayarlar ve ortak düğmeler

- `Group04_Settings/settings_button_teal.png`: petrol yeşili/mavisi ayarlar düğmesi; mor yok.
- `settings_panel_blank.png`: bütün ayarları taşıyan tek, yazısız panel.
- `slider_rail.png`, `slider_knob.png`: aynı parçaların iki ayrı `SES` ve `MÜZİK` kaydırıcısında kullanılması için.
- `action_button_blank.png`: `ANA MENÜ` ve `YENİDEN BAŞLAT` için tekrar kullanılabilir yazısız düğme; `close_button_teal.png`: panel kapatma.
- Basılı/pasif durumlar için ayrı onaylı bitmap üretilmedi. Basma durumunda mevcut sprite üzerinde kısa ölçek/renk tepkisi kullanılabilir; gerçek ses/müzik slider işlevi ve panel yerleşimi ayrı entegrasyon işidir.

## Grup 5 — harita ve sonuç UI

- `Group05_MapResult/result_panel_blank.png`: başlık, bölüm numarası, sonuç ve düğmelerin ayrı çizileceği boş dikey panel kaynağı. Kazanma/kaybetme ve bölüm başlangıcında aynı çerçeve kullanılabilir.
- Haritadaki 1–10 sayıları Grup 1'in gerçek font/atlasından kurulmalı; onaylı harita zemini ve mevcut açık/seçili/kilitli düğümler yeniden çizilmedi. `ANA MENÜ`/`YENİDEN DENE`/`SONRAKİ` yazıları için Grup 4 ortak boş düğme tekrar kullanılabilir.

## Grup 6 — somun ve vida pası

- `Group06_Rust/nut_rust2_source.png`, `nut_rust1_source.png`, `screw_rust2_source.png`, `screw_rust1_source.png`: yoğun pas ve bir kez temizlenmiş görünüm için dört şeffaf kaynak. Tam temiz durumun mevcut sprite kopyaları aynı klasörde `nut_clean_existing.png` ve `screw_clean_existing.png`; oyundaki asıl `bolt_nut.png`/`bolt_screw.png` dosyaları değiştirilmedi.
- Pas tekdüze kahverengi tint değil, turuncu/umber yamalar ve açıkta kalan metaldir. AI düzenlemelerinde alfa sınırları ve özellikle somun alt kenarı **piksel tamlığıyla aynı kalmadı**; aynı hücre/pivotta hizalanıp küçük boyda okunurluk kontrolü yapılmadan runtime durumu olarak bağlanmamalı. Kaynak görüntüler, kabul edilmiş nihai animasyon atlası değildir.

## Grup 7 — temel efekt parçaları

- `Group07_VFX/`: `candy_pop_source.png`, `rocket_sparks_source.png`, `hammer_impact_source.png`, `magnet_glint_source.png`, `motor_burst_source.png`, `oil_splash_source.png`, `frozen_oil_shards_source.png`.
- Bunlar tek karelik şeffaf parçalar. Ölçek/dönüş/alfa/zamanlama kodla verilir; sıralı patlama, motor halkası ve mıknatıs çizgileri mevcut oyun kurallarıyla ayrı bağlanmalıdır. `bolt_ping` görseli için küçük `candy_pop` yeniden renklendirilerek kullanılabilir; her olaya ayrı büyük PNG çizilmedi.

## Grup 8 — zincir/alev alma

- `Group08_Chain/chain_label_blank.png`: dinamik `ZİNCİR N×` metni için yazısız etiket. `flame_tongue.png`: köşelere çoğaltılabilen alev parçası. Küçük belirme ışıltısında Grup 7 kıvılcımı yeniden kullanılabilir.
- Güç patlamalarının zincir sayılmaması ve yalnız sonrasındaki kendiliğinden eşleşmelerin sayılması oyun kodu görevidir; bu pakette mekanik uygulanmadı.

## Grup 9 — ses taslakları

- `Group09_Audio/` altında 19 özgün mono PCM WAV: şeker, roketin ateş/uçuş/patlama aşamaları, çekiç, mıknatıs çekim/kapanış, yağ/donmuş yağ, civata, motor çalışma/kırılma, çark, UI/harita/ayar, kazanma/kaybetme ve zincir adımı.
- Olay eşlemesi: şeker `candy_pop`; roket `rocket_ignite`, `rocket_flight`, `rocket_burst`; çekiç `hammer_hit`; mıknatıs `magnet_pull`, `magnet_close`; yağ `oil_squish`, donmuş yağ `frozen_oil_crack`; civata `bolt_ping`; motor `motor_run_loop_draft`, `motor_break`; çark `gear_spin_loop_draft`; genel UI `ui_click`, harita `map_select`, ayarlar `settings_toggle`; sonuç `level_win`, `level_lose`; otomatik eşleşme zinciri `chain_step`.
- Üretim kaynağı `Art/build_ui_sound_pack.py`; standart Python dalga sentezi, rastgele tohum sabit, üçüncü taraf sample kullanılmadı. Unity ses üretim servisi Editor bağlantısı olmadığı için çalışmadı; bu yüzden dosyalar **prosedürel taslaktır**, final ses tasarımı/işitsel miks kabulü değildir. `*_loop_draft.wav` dosyalarının kesintisiz döngü noktası ayrıca temizlenmeli. WAV'lar oyuna bağlanmadı.

## Görsel üretim yönü

Mevcut Workshop Match UI'ı referans alınarak built-in ImageGen ile şeffaf raster parçalar üretildi: sade toon atölye, sıcak ahşap, krem, pirinç/altın kenar; ayarlarda petrol teal; ikon/metin/sayı gömülmeyen boş çerçeveler. Prompt kümesi: (5) mevcut panel üslubunda boş sonuç paneli; (6) mevcut somun/vida çizimini koruyarak turuncu/umber yoğun ve kısmi pas; (7) ayrı küçük şeker pufu, roket kıvılcımı, çekiç darbesi, mıknatıs çekim ışıltısı, motor patlaması, yağ sıçraması, donmuş yağ kırığı; (8) yazısız zincir etiketi ve tek alev dili. Dış kaynak font haricindeki çizimler bu üretim turunda oluşturuldu. 1. gruptaki atlas ise gerçek fonttan deterministik `Art/build_ui_font_atlas.py` betiğiyle türetildi; büyük/küçük Türkçe glifler kontrol edildi.

## Teknik kontrol ve açık işler

4 Ekim bağlantı notu: Aşağıdaki üretim anı kayıtları tarihsel durumdur. Kaynaklar artık Editor katalog oluşturucusunda ve runtime çağrılarında referanslıdır; katalog asseti yenilenmeden gerçek oyunda görünmezler. Müzik kaynağı bulunmadığı için müzik slider'ı yalnız ayarı saklar. Gerçek Unity derlemesi ve görsel/ses kabulü beklenmektedir.

- PNG dosyalarının RGBA boyutu/alpha sınırları, WAV başlığı/uzunluk/tepe/RMS verileri ve font önizlemesi kontrol edildi. Bu, ekran görünümü veya ses duyumu kabulü değildir.
- Unity import, C# derleme, Game View, gerçek cihaz ve oynanış testi **yapılmadı**; kullanıcı geçmişte pahalı görsel testleri kendisinin yapacağını belirtti.
- HUD nine-slice, araç segment hizalaması ve pas durumlarının pivot/ölçek hizası bitmeden bu kaynaklar son oyun içi asset kabul edilmemeli. Ayarlar panelinin buton/slider işlevi, VFX animasyon zamanlaması, zincir kodu ve ses miks/bağlantısı ayrı T12 işleri.
