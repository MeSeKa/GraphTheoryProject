# HexWorld — Game Design Document

**Proje:** Graph Theory Project — Phase 3  
**Branch:** feature/hex-world  
**Son Güncelleme:** 2026-05-14

---

## 1. Oyun Özeti

İzometrik hex grid üzerinde kurulu, grafik teorisi tabanlı bir bulmaca oyunu. Oyuncu bir adada köprüleri ve tile'ları yıkarak kurt ile kuzu arasındaki bağlantıyı kesmeye çalışır. Her bölüm, grafdaki **minimum cut** probleminin somutlaştırılmış halidir.

- **Tür:** Single-player puzzle
- **Görsel:** 3D izometrik, ortografik kamera
- **Platform:** PC (Unity)
- **Temel Kavram:** Min-cut / Max-flow (Graf Teorisi)

---

## 2. Temel Mekanik

### Graf Modeli
- **Node (Tile):** Altıgen ada parçaları
- **Edge (Bridge):** Tile'lar arası köprüler
- **Source:** Kurt tarafı (wolf tile) — **yıkılamaz, bombalanmaz**
- **Destination:** Kuzu tarafı (sheep tile) — **yıkılamaz, bombalanmaz**

### Kazanma Koşulu
Source ile Destination arasındaki **tüm yollar kesildiğinde** kazanılır (BFS ile kontrol edilir). Köprü/tile yıkıldıktan **1 saniye sonra** win ekranı gösterilir.

### Kaybetme Koşulu
Araç ve para kalmamışken Source–Destination hâlâ bağlıysa oyuncu kaybeder.

---

## 3. Araçlar (Tools)

Oyuncunun envanterinde bölüme özgü sayıda **ücretsiz araç** ve harcanabilir **altın** bulunur. Her kullanımda araç harcanır; ekonomi sistemi üzerinden ek araç satın alınabilir.

| Araç | Etki | Hedef |
|------|------|-------|
| **Balta (Axe)** | Ahşap köprüyü keser | Edge (Wood) |
| **Kazma (Pickaxe)** | Taş köprüyü keser | Edge (Stone) |
| **Demir Makası (Iron Shears)** | Metal köprüyü keser | Edge (Metal) |
| **Bomba (Bomb)** | Tile + tüm bağlı köprüleri yok eder | Node |

### Bomba Kuralları
- Bir tile'a tıklandığında o tile **ve tüm bağlı köprüleri** kaldırılır
- **Source ve Destination tile'lara kullanılamaz**
- High-degree interior node'ları tek hamlede izole etmek için güçlü ama pahalı

### Yanlış Araç Kullanımı
Seçili araçla kesilemeyecek bir hedefte köprü kırmızı flash yapar, araç harcanmaz.

---

## 4. Köprü Türleri (Edge Types)

| Tür | EdgeType | Gerekli Araç |
|-----|----------|-------------|
| Wood | 1 | Axe |
| Stone | 2 | Pickaxe |
| Metal | 3 | Iron Shears |

> `Rope (0)` enum'da tutulmuş ama kullanılmıyor.

---

## 5. Tile Türleri (Node Types)

| Tür | HexTileType | Prefab |
|-----|-------------|--------|
| Grass (default) | 1 | GrassHex Variant |
| Stone | 2 | StoneHex Variant |
| Sand | 3 | SandHex Variant |
| Source | — | SourceHex prefab |
| Destination | — | DestinationHex prefab |

`HexLevelData.tileType` → bölüm geneli default.  
`HexTileEntry.tileType = Default` → level default'una düşer.

---

## 6. Ekonomi Sistemi

### Genel Yapı
Her bölüm oyuncuya araç + altın ile başlar. Oyuncu bölüm içi **Shop Panel**'den altınla ek araç satın alabilir. Bölümü **elindeki altınla** bitirmek yıldız skorunu belirler.

### Başlangıç Kaynakları (HexLevelData)
```
axeCount          : 2       // ücretsiz araç
pickaxeCount      : 1
ironShearsCount   : 0
bombCount         : 0
startingGold      : 300     // başlangıç altını
```
Örnek: "2 makas + 300 altın ile başla" → `ironShearsCount=2, startingGold=300`

### Yıldız Sistemi
Bölümü bitirirken **elinde kalan altın miktarı** yıldız skoru verir.

| Yıldız | Koşul |
|--------|-------|
| ⭐⭐⭐ | Başlangıç altınının ≥ %66'sı hâlâ elde |
| ⭐⭐ | Başlangıç altınının ≥ %33'ü elde |
| ⭐ | Bölüm tamamlandı (altın bitti veya az kaldı) |

> Eşikler GDD tasarımı sırasında ayarlanacak, `HexLevelData`'ya field olarak gömülür.

### Zorluk & Altın İlişkisi (Opsiyonel)
Oyuncu başlamadan önce zorluk seçebilir:

| Zorluk | Başlangıç Altını |
|--------|-----------------|
| Kolay | `startingGold × 1.5` |
| Normal | `startingGold` |
| Zor | `startingGold × 0` (altınsız) |

Zorluk sadece altını etkiler, araç sayısını değil.

### Araç Fiyatları

**Baz fiyatlar** ayrı bir `ScriptableObject` (`ToolPriceConfig`) içinde tutulur:
```
axePrice        : 80
pickaxePrice    : 120
ironShearsPrice : 200
bombPrice       : 350
```

**Level override:** `HexLevelData` içinde her araç için yüzdesel indirim tanımlanabilir:
```
axeDiscount        : 0      // % — 0 = indirim yok
pickaxeDiscount    : 25     // % — bu level'da balta %25 indirimli
ironShearsDiscount : 0
bombDiscount       : 0
```

UI'da indirimli fiyat varsa orijinal fiyat üstü çizili gösterilir, indirim yüzdesi küçük etiketle belirtilir (`%25 İNDİRİM`).

### Shop Panel UI
```
ShopPanel (overlay)
├── [Axe]         80g  → [Satın Al]
├── [Pickaxe]    120g  → [Satın Al]
├── [Iron Shears] 200g → [Satın Al]   ← bazı level'larda 150g (%25 indirim)
├── [Bomb]        350g → [Satın Al]
├── GoldText      — "💰 240g"
└── CloseButton
```

Yeterli altın yoksa buton devre dışı kalır. Satın alma anında `startingGold`'dan düşülür, tool envanteri güncellenir.

---

## 7. Level Design Prensipleri

### Peripheral Cut Triviality Sorunu
Source veya Destination'ın degree'si ≤ 2 ise oyuncu her zaman o node'u izole ederek trivial hamlede bitirir.

**Kural:** Source ve Destination'ın her biri en az **4 komşuya** bağlı olmalı. Optimal cut edge'leri grafın **ortasında** yer almalı.

### Level Tasarım Hedefi
Her level birden fazla geçerli cut seti sunmalı, her setin **araç maliyeti farklı** olmalı:
- Set A: 2 Axe + 1 Pickaxe (ucuz, az altın harcar → 3 yıldız potansiyeli)
- Set B: 1 Bomb + 1 Axe (hızlı ama pahalı → 1–2 yıldız)
- Oyuncu hangisini karşılayabileceğine/tercih ettiğine karar verir

### Level & Ekonomi İlişkisi
Level revizesi **ekonomi sistemi tamamlandıktan sonra** yapılacak; fiyatlar, indirimler ve başlangıç altını birlikte değerlendirilerek tasarlanacak.

### Editor Validation (Planlanan)
`HexLevelEditorWindow` — bölüm seçilince:
- Gerçek min-cut sayısını hesapla (max-flow / Edmonds-Karp)
- Tüm minimum cut setlerini listele
- "Trivial peripheral cut var mı?" uyarısı ver
- Her cut setinin araç maliyetini göster (fiyat dahil)
- Optimal cut edge'leri sahnede renklendir

---

## 8. Particle FX Planı

| Olay | Efekt | Öncelik |
|------|-------|---------|
| Wood köprü kesilmesi | Tahta parçaları, toz bulutu | Yüksek |
| Stone köprü kesilmesi | Taş kırıntısı, gri duman | Yüksek |
| Metal köprü kesilmesi | Kıvılcım, metal parçacıkları | Yüksek |
| Bomba (node patlaması) | Patlama + is bulutu + çakıl sıçraması | Yüksek |
| Yanlış araç kullanımı | Kırmızı "X" halkası | Orta |
| Kazanma (Win) | Konfeti + altın yıldızlar | Orta |
| Kaybetme (Lose) | Kırmızı duman, karanlık patlama | Orta |
| Su kenarı (ambient) | Su sıçraması / dalgacık loop | Düşük |

---

## 9. UI Yapısı

```
Canvas
├── LevelNameText        — "Level 3: The Gauntlet"
├── CutsUsedText         — "Cuts Used: 2"
├── GoldText             — "💰 240g"
├── StatusText           — durum mesajı
├── ToolPanel
│   ├── AxeButton        (sayaç + altın ile satın al)
│   ├── PickaxeButton
│   ├── IronShearsButton
│   └── BombButton
├── ShopButton           — Shop Panel'i açar
├── ShopPanel            — overlay, her araç için fiyat + satın al
├── WinPanel             — 1s delay (DOTween), yıldız skoru göster
│   ├── StarDisplay      — 1–3 yıldız animasyonlu
│   ├── NextLevelButton
│   └── RetryButtonWin
└── LosePanel
    └── RetryButton
```

---

## 10. Sahne Yapısı

**Sahne:** `Assets/Scenes/HexWorldScene.unity`

| GameObject | Bileşen | Not |
|-----------|---------|-----|
| Water | Plane, Y=−0.35 | Blue URP / WaterGraph shader |
| Main Camera | IsometricCamera | pitch=35, yaw=45, padding=3, orthographic |
| HexLevelLoader | HexLevelLoader | tile/bridge prefablar wired, hexSize=4f |
| HexToolManager | HexToolManager | 4 buton: Axe, Pickaxe, IronShears, Bomb |
| HexGameManager | HexGameManager | levels[0..N], materials, UI refs |
| EventSystem | InputSystemUIInputModule | — |

---

## 11. Scriptler (`Assets/Scripts/HexWorld/`)

| Script | Sorumluluk |
|--------|-----------|
| `HexLevelData.cs` | ScriptableObject: tiles, bridges, araç sayıları, startingGold, indirimler |
| `HexGrid.cs` | Static util: AxialToWorld(q, r, hexSize=4f) |
| `HexTile.cs` | MonoBehaviour: q/r, bridges List, SetMaterial() |
| `HexBridge.cs` | MonoBehaviour: Initialize(), AnimateDestroyed() |
| `HexLevelLoader.cs` | Tile/bridge spawn, per-tile tileType resolve |
| `HexToolManager.cs` | Araç seçimi, buton highlight, tüketim, altın takibi |
| `HexGameManager.cs` | Raycast, BFS, OnWin/OnLose, yıldız hesabı |
| `HexShopManager.cs` | Shop Panel aç/kapat, araç satın alma, fiyat override uygula |
| `ToolPriceConfig.cs` | ScriptableObject: baz araç fiyatları |
| `IsometricCamera.cs` | FrameTiles() orthographic size hesabı |
| `HexLevelEditorWindow.cs` | Editor-only: min-cut validation, trivial cut uyarısı |

### Kritik Notlar
- `IsometricCamera.FrameTiles`: `Vector3.back` kullanılırsa kamera toprağa girer
- `HexToolManager` butonlara **sahne instance** atanmalı, prefab asset değil
- `Button Transition = Sprite Swap` kullan, Color Tint kullanma

---

## 12. Sonraki Adımlar (Sıralı)

1. ✅ **Iron Shears + Bomba node-kill** — tamamlandı
2. **Editor Validation Tool** — min-cut hesaplama, trivial uyarı, cut maliyet listesi
3. **Ekonomi sistemi** — ToolPriceConfig, startingGold, ShopPanel, yıldız skoru
4. **Level revizyonu** — ekonomi ve validation tool sonrası, fiyat/indirim dengesiyle
5. **Particle FX** — köprü + node yıkım efektleri
6. **Hint sistemi** — oyuncu takılınca min-cut'ı gösteren buton
7. **Ses efektleri** — araç başına ayrı ses
