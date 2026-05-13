# HexWorld — Game Design Document

**Proje:** Graph Theory Project — Phase 3  
**Branch:** feature/hex-world  
**Son Güncelleme:** 2026-05-14

---

## 1. Oyun Özeti

İzometrik hex grid üzerinde kurulu, grafik teorisi tabanlı bir bulmaca oyunu. Oyuncu bir adada köprüleri ve adaletleri yıkarak kurt ile kuzu arasındaki bağlantıyı kesmeye çalışır. Her bölüm, grafdaki **minimum cut** probleminin somutlaştırılmış halidir.

- **Tür:** Single-player puzzle
- **Görsel:** 3D izometrik, ortografik kamera
- **Platform:** PC (Unity)
- **Temel Kavram:** Min-cut / Max-flow (Graf Teorisi)

---

## 2. Temel Mekanik

### Graf Modeli
- **Node (Tile):** Altıgen ada parçaları
- **Edge (Bridge):** Tile'lar arası köprüler
- **Source:** Kurt tarafı (wolf tile) — **yıkılamaz**
- **Destination:** Kuzu tarafı (sheep tile) — **yıkılamaz**

### Kazanma Koşulu
Source ile Destination arasındaki **tüm yollar kesildiğinde** kazanılır (BFS ile kontrol edilir). Köprü kırıldıktan **1 saniye sonra** win ekranı gösterilir.

### Kaybetme Koşulu
Araç kalmamışken Source–Destination hâlâ bağlıysa oyuncu kaybeder.

---

## 3. Araçlar (Tools)

Oyuncunun envanterinde bölüme özgü sayıda araç bulunur. Her kullanımda bir araç harcanır.

| Araç | İkon Rengi | Etki | Hedef |
|------|-----------|------|-------|
| **Balta (Axe)** | Kahverengi | Ahşap köprüyü keser | Edge (Wood) |
| **Kazma (Pickaxe)** | Gri | Taş köprüyü keser | Edge (Stone) |
| **Demir Makası (Iron Shears)** | Gümüş-mavi | Metal köprüyü keser | Edge (Metal) |
| **Bomba (Bomb)** | Turuncu | Tile'ı ve ona bağlı TÜM köprüleri yok eder | Node |

### Bomba Kuralları
- Bir tile'a tıklandığında o tile **ve tüm bağlı köprüleri** kaldırılır (vertex contraction değil, tam silme)
- **Source ve Destination tile'lara kullanılamaz** — tıklanırsa hata efekti gösterilir
- Bu mekanik edge silmekten farklı bir strateji gerektirir: yüksek-degree intermediate node'ları tek hamlede izole etmek mümkün olur

### Yanlış Araç Kullanımı
Seçili araçla kesilemeyecek bir köprüye/tile'a tıklanırsa köprü kırmızı flash yapar, araç harcanmaz.

---

## 4. Köprü Türleri (Edge Types)

| Tür | EdgeType Değeri | Gerekli Araç | Görsel |
|-----|----------------|-------------|--------|
| Wood | 1 | Axe | Ahşap doku, kahverengi |
| Stone | 2 | Pickaxe | Taş doku, gri |
| Metal | 3 | Iron Shears | Metal doku, gümüş |

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

## 6. Level Design Prensipleri

### Mevcut Sorun: Peripheral Cut Triviality
Source veya Destination'ın degree'si ≤ 2 ise oyuncu her zaman o node'u izole ederek 2 hamlede bitirebilir. Bu bir tasarım açığıdır, bulmaca değeri yoktur.

**Kural:** `min(degree(source), degree(destination)) > istenen min-cut değeri` olmalı.  
**Uygulamada:** Source ve Destination'ın her biri en az **4 komşuya** bağlı olmalı. Optimal cut edge'leri grafın ortasında (interior) yer almalı.

### Zorluk Katmanları

| Katman | Araç | Etkisi |
|--------|------|--------|
| Basit | Sadece Axe | Tek tip köprü, az alternatif yol |
| Orta | Axe + Pickaxe | İki tip köprü, birden fazla cut seti |
| Zor | Axe + Pickaxe + Iron Shears | Üç tip, kıt araç sayısı |
| İleri | + Bomb | Node silme stratejisi devreye girer |

### İyi Level Özellikleri
- **Birden fazla geçerli çözüm seti** olmalı ama hepsinin maliyet/fırsat dengesi farklı olmalı
- **Source/Destination'a adjacent köprüler pahalı** (Stone veya Metal) olmalı → ucuz peripheral cut engeli
- **Optimal çözüm interior edge'leri içermeli** — oyuncu grafı okuyarak düşünmeli
- **Bomba bölümleri:** Bombayı kullanmadan çözülmez olmalı ya da bomba kullanmak çok daha verimli bir yol açmalı (degree-4+ intermediate node)

### Editor Validation (Planlanan)
`HexLevelEditorWindow` — bir bölümü seçince:
- Gerçek min-cut sayısını hesapla (max-flow / BFS)
- Tüm minimum cut setlerini listele
- "Trivial peripheral cut var mı?" uyarısı ver
- Optimal cut edge'leri sahnede renklendir

---

## 7. Particle FX Planı

| Olay | Efekt | Öncelik |
|------|-------|---------|
| Wood köprü kesilmesi | Tahta parçaları, toz bulutu | Yüksek |
| Stone köprü kesilmesi | Taş kırıntısı, gri duman | Yüksek |
| Metal köprü kesilmesi | Kıvılcım, metal parçacıkları | Yüksek |
| Bomba (node patlaması) | Patlama + is bulutu + çakıl sıçraması | Yüksek |
| Yanlış araç kullanımı | Kırmızı "X" halkası veya kıvılcım | Orta |
| Kazanma (Win) | Konfeti + altın yıldızlar (kuzu üzerinden) | Orta |
| Kaybetme (Lose) | Kırmızı duman, karanlık patlama | Orta |
| Su kenarı (ambient) | Su sıçraması / dalgacık loop | Düşük |

---

## 8. UI Yapısı

```
Canvas
├── LevelNameText       — "Level 3: The Gauntlet"
├── CutsUsedText        — "Cuts Used: 2"
├── StatusText          — durum mesajı
├── ToolPanel
│   ├── AxeButton
│   ├── PickaxeButton
│   ├── IronShearsButton  [YENİ]
│   └── BombButton
├── WinPanel            — 1s delay sonrası gösterilir (DOTween)
│   ├── NextLevelButton
│   └── RetryButtonWin
└── LosePanel
    └── RetryButton
```

---

## 9. Sahne Yapısı

**Sahne:** `Assets/Scenes/HexWorldScene.unity`

| GameObject | Bileşen | Not |
|-----------|---------|-----|
| Water | Plane, Y=−0.35 | Blue URP material |
| Main Camera | IsometricCamera | pitch=35, yaw=45, padding=3, orthographic |
| HexLevelLoader | HexLevelLoader | tile/bridge prefablar wired, hexSize=4f |
| HexToolManager | HexToolManager | 4 buton: Axe, Pickaxe, IronShears, Bomb |
| HexGameManager | HexGameManager | levels[0..N], materials, UI refs |
| EventSystem | InputSystemUIInputModule | — |

---

## 10. Scriptler (`Assets/Scripts/HexWorld/`)

| Script | Sorumluluk |
|--------|-----------|
| `HexLevelData.cs` | ScriptableObject: tile[], bridge[], source, dest, araç sayıları |
| `HexGrid.cs` | Static util: AxialToWorld(q, r, hexSize=4f) |
| `HexTile.cs` | MonoBehaviour: q/r, bridges List, SetMaterial() |
| `HexBridge.cs` | MonoBehaviour: Initialize(), AnimateDestroyed() |
| `HexLevelLoader.cs` | Tile/bridge spawn, per-tile tileType resolve |
| `HexToolManager.cs` | Araç seçimi, buton highlight, tüketim |
| `HexGameManager.cs` | Raycast, BFS, OnWin/OnLose, 1s delay |
| `IsometricCamera.cs` | FrameTiles() orthographic size hesabı |

### Kritik Notlar
- `IsometricCamera.FrameTiles`: `Vector3.back` kullanılırsa kamera toprağa girer. Doğrusu: `cam.transform.position = centroid - forward * dist`
- `HexToolManager` butonlara **sahne instance** atanmalı, prefab asset değil
- `Button Transition = Sprite Swap` kullan, Color Tint kullanma (`img.color`'ı override eder)

---

## 11. Level Tool Count Tablosu (Mevcut, Revizyona Açık)

> Bomba artık node patlatıyor, Iron Shears metal kesiyor. Mevcut level'lar Bomb → Iron Shears dönüşümüne göre revize edilecek.

| Level | Axe | Pickaxe | Iron Shears | Bomb |
|-------|-----|---------|-------------|------|
| L1 Tutorial | 2 | 0 | 0 | 0 |
| L2 Crossroads | 1 | 1 | 0 | 0 |
| L3 The Gauntlet | 1 | 1 | 0 | 0 |
| L4–L15 | (revizyonda) | | | |

---

## 12. Sonraki Adımlar

1. **Iron Shears + Bomba node-kill mekaniği** — kod implementasyonu
2. **Mevcut 15 level'ı revize et** — trivial cut sorunu + yeni araç sistemi
3. **Editor Validation Tool** — min-cut hesaplama + trivial uyarı
4. **Particle FX** — köprü + node yıkım efektleri
5. **Hint sistemi** — oyuncu takılınca min-cut'ı gösteren buton
6. **Ses efektleri** — araç başına ayrı ses
