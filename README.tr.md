# 💯 100 (Hundred Game)

**🇹🇷 Türkçe** | [🇬🇧 English](README.md)

**Mobil için modern, animasyonlu sayı yerleştirme bulmacası.** 10 × 10'luk tahtayı 1'den 100'e kadar sırayla sayılarla doldur. Kulağa kolay geliyor, ama her sayı belirli bir kareye atlamak zorunda ve tek bir yanlış hamle seni sıkıştırır. Sen kaça kadar gidebilirsin?

![Unity](https://img.shields.io/badge/Unity-6000.6-black?logo=unity) ![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS-green) ![Language](https://img.shields.io/badge/UI-Turkish-red) ![License](https://img.shields.io/badge/License-MIT-yellow)

<p align="center">
  <img src="docs/screenshots/menu.png" width="190" alt="Ana menü">
  <img src="docs/screenshots/oyun.png" width="190" alt="Oyun">
  <img src="docs/screenshots/bolumler.png" width="190" alt="Bölümler">
  <img src="docs/screenshots/sonuc.png" width="190" alt="Sonuç">
</p>

## Nasıl oynanır?

1. **1** sayısını istediğin kareye koy.
2. Sırayla **2**, **3**, **4**... koy. Son sayının bulunduğu kareden, sıradaki sayı **boş** bir kareye şu şekilde gidebilir:
   - **Yatay veya dikey 3 kare ileri** (arada 2 boş kare), ya da
   - **Çapraz 2 kare ileri** (arada 1 boş kare).
3. Gidebileceğin kareler parlar, seçeneklerini hep görürsün.
4. Hamle kalmayınca oyun biter. **Skorun, koyduğun son sayıdır.** 100'e ulaşırsan kazanırsın!

Sıkıştın mı? **Geri Al** ile hamleleri geri al, **İpucu** ile iyi bir hamleyi gör veya **Yeniden** başla.

## Özellikler

- **Tüm bölümler açık** bölüm seçimi; her kartta rekorun, yıldızların ve ilerlemenle dolan mini tahta görünür. Yarım kalan oyunlar kaydedilir ve devam edilir.
- **Skor sistemi:** anlık skor, rekor, kalan seçenek sayısı, ilerleme çubuğu ve bölüm başına 1-3 yıldız.
- **Parlayan geçerli kareler** ve üzerlerinde sıradaki sayı.
- **Geri Al, İpucu, Yeniden** ve her hamlede otomatik kayıt.
- **Akıcı animasyonlar:** beliren kareler, dalga ve parçacık efektleri, her 10 sayıda kutlama yazıları, hatalı dokunuşta sarsıntı, sayılan skor, zafer konfetisi, yüzen sayı karolarıyla animasyonlu arka plan.
- **Ses dosyası yok, hepsi kodla üretilir:** her sayı pentatonik bir nota çalar, saymak melodiye dönüşür. Efektler ve sakin bir fon müziği açılışta sentezlenir. Kendi seslerini `Resources/Audio/<SesAdı>` içine koyarak değiştirebilirsin.
- **Mobile hazır:** dikey ekran, güvenli alan desteği, titreşim, geri tuşu, ayarlar (ses, müzik, titreşim) ve görsel anlatımlı "Nasıl Oynanır" ekranı.
- Her şey kodla üretilir (sprite ve ikonlar dahil), **üçüncü taraf asset gerekmez**.

## Ekran görüntüleri

| Menü | Bölümler | Başlangıç | Oyun |
|---|---|---|---|
| ![](docs/screenshots/menu.png) | ![](docs/screenshots/bolumler.png) | ![](docs/screenshots/oyun-basi.png) | ![](docs/screenshots/oyun.png) |

| Nasıl Oynanır | Ayarlar | Zafer |
|---|---|---|
| ![](docs/screenshots/nasil-oynanir.png) | ![](docs/screenshots/ayarlar.png) | ![](docs/screenshots/sonuc.png) |

## Kurulum

1. **Unity 6000.6** (veya daha yeni 6.x) kur.
2. Depoyu klonla, Unity Hub ile klasörü aç.
3. **HundredGame > Setup Project** menüsünü çalıştır (sahne, oyuncu ayarları ve ikon oluşturulur). Hazır `Assets/_Game/Scenes/Main.unity` zaten depoda var.
4. Play'e bas. Sahnede sadece kamera ve `GameBootstrap` vardır; tüm arayüz çalışma anında kurulur.

Android / iOS derlemesi için **File > Build Profiles** kullan (dikey ekran ayarlı).

## Proje yapısı

```
Assets/_Game/Scripts
├─ Core    GameBootstrap, Anim (tween), editör smoke botu
├─ Data    LevelDef + Levels, SaveData
├─ Game    Board (saf kural mantığı)
├─ UI      UIManager, UIKit, UIFx, MenuPages, GamePage, Popup
├─ Audio   Synth (kodla ses üretimi), AudioManager
└─ Editor  HundredSetup, SmokeLauncher
```

## Yeni bölüm ekleme

Bölümler düz veridir. `Assets/_Game/Scripts/Data/LevelDef.cs` içindeki `Levels.All` listesine ekle:

```csharp
new LevelDef { Name = "Haç", Subtitle = "Delikli 10 × 10", Size = 10,
               Blocked = new[] { 44, 45, 54, 55 }, StarScores = new[] { 30, 60, 96 } },
```

Bölüm seçimi, kayıt, yıldızlar ve sonuç ekranları otomatik algılar. `Blocked`, kullanılamayan kareleri (`y * Size + x`) listeler.

## Test araçları

- **HundredGame > Simulate Board:** hamle kurallarını doğrular, botlarla oyun oynatır.
- **HundredGame > Run Smoke Test:** tüm ekranları gezer, ipucu botuyla tam bir oyun oynar ve `Logs/Smoke` içine ekran görüntüsü alır.

## Lisans

[MIT](LICENSE), (c) Berkay Tonga. Üçüncü taraf asset veya ses kullanılmamıştır.
