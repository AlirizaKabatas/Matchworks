# Workshop Match — Codex çalışma kuralları

Bu paket küçük bir Unity mobil match-3 portfolyo oyununun tasarım kaynağıdır. Çalışma adı **Workshop Match**; son oyun adı henüz seçilmedi. Unity proje köküne bu dosyayı, `Docs/` klasörünü ve içeriklerini kopyala. Bu belgeler proje kökünde durur; `Assets/` içine taşıma.

## Okuma sırası

1. `Docs/GDD.md`: amaç, oyuncu deneyimi ve kapsam.
2. `Docs/Mechanics.md` ve `Docs/Interactions.md`: kesin oynanış kuralları.
3. `Docs/Levels.md`: bölüm verisi, deterministik tekrar ve görsel bölüm editörü.
4. `Docs/Technical.md` ve `Docs/Assets.md`: Unity/Blender uygulaması.
5. `Docs/Tasks.md` ve `Docs/State.md`: sıradaki iş ve gerçek ilerleme.

## Çalışma biçimi

- Her seferinde bir `Tasks.md` maddesi seç; kapsamını büyütmeden uygula, Unity derleme/oynanış kontrolünü yap, sonucu `State.md` ve `Tasks.md` içine yaz.
- Belgelerde **planlanan** yazan bir sistemi yapılmış gibi sunma. Açık bir tasarım kararı gerektiren yerde varsayımı `State.md` altında `Açık kararlar`a kaydet.
- Seviye engellerinin konumu oyuncu tasarımcısı Ali tarafından belirlenecek. Kod veya Codex kendi başına bu konumları nihai karar olarak üretmesin. Öneriler ayrı taslak olabilir.
- Bölümleri ayrı Unity sahneleri olarak kurma. Veriden yüklenen tek oynanış sahnesi ve görsel bölüm editörü kullan.
- Önce sabit, küçük bir Unity blokajı çalıştır; Blender asset'lerini oyun mantığına bağlı olmadan değiştirilebilir prefab olarak kullan.
- İnsan tarafından oynanmış test veya gerçek veri yokken sonuç, retention, A/B testi başarısı ve denge doğrulanmış gibi yazma.
- Zar, reklam, ödeme ve gelir sistemi bu sürümün kapsamı dışında.

İlk Codex görevi: belgeleri oku, `Docs/Tasks.md` T01'i uygula, derleme ve bir oynanış kontrolü yap, `Docs/State.md`yi gerçek sonuçla güncelle.
