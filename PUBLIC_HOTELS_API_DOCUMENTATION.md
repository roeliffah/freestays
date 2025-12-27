# Public Hotels API Endpoints

Frontend ziyaretçilerine gösterilmek üzere hazırlanmış public hotel ve destinasyon endpoint'leri.

## 📋 Endpoint Listesi

### ✅ 1. Featured Hotels - Yıldız Sayısına Göre Popüler Oteller

**Endpoint:** `GET /api/v1/public/featured-hotels`

**Açıklama:** Yıldız sayısına göre popüler otelleri getirir. Oteller, resim sayısı, özellik sayısı ve tema sayısına göre sıralanır.

**Query Parametreleri:**
- `stars` (opsiyonel): Otel yıldız sayısı (3, 4, 5 gibi). Varsayılan: tümü
- `count` (opsiyonel): Kaç otel getirileceği. Varsayılan: 10

**Headers:**
- `Accept-Language`: Dil kodu (tr, en, de, fr). Varsayılan: en

**Örnek Kullanım:**
```http
GET /api/v1/public/featured-hotels?stars=5&count=10
Accept-Language: tr
```

**Örnek Response:**
```json
{
  "language": "tr",
  "stars": 5,
  "count": 10,
  "hotels": [
    {
      "id": 12345,
      "name": "Grand Resort Lagonissi",
      "description": "Lüks sahil oteli...",
      "stars": 5,
      "city": "Athens",
      "country": "Greece",
      "countryCode": "GR",
      "address": "40th km Athens-Sounio Avenue",
      "resort": {
        "id": 789,
        "name": "Athens Coast"
      },
      "location": {
        "latitude": 37.7854,
        "longitude": 23.9478
      },
      "images": [
        "https://...",
        "https://..."
      ],
      "featureIds": ["1", "2", "3"],
      "themeIds": ["5", "8"],
      "contact": {
        "phone": "+30...",
        "email": "info@...",
        "website": "https://..."
      }
    }
  ]
}
```

---

### ✅ 2. Popular Destinations - Ülke Bazlı Popüler Destinasyonlar

**Endpoint:** `GET /api/v1/public/popular-destinations`

**Açıklama:** Ülke bazlı popüler destinasyonları getirir. Her destinasyondaki otel sayısına göre sıralanır.

**Query Parametreleri:**
- `country` (opsiyonel): Ülke kodu (TR, US, GR gibi). Boş bırakılırsa tüm ülkeler
- `count` (opsiyonel): Kaç destinasyon getirileceği. Varsayılan: 10

**Headers:**
- `Accept-Language`: Dil kodu (tr, en, de, fr). Varsayılan: en

**Örnek Kullanım:**
```http
GET /api/v1/public/popular-destinations?country=TR&count=5
Accept-Language: tr
```

**Örnek Response:**
```json
{
  "language": "tr",
  "country": "TR",
  "count": 5,
  "destinations": [
    {
      "id": "ATH",
      "code": "ATH",
      "name": "Athens",
      "country": "Greece",
      "countryCode": "GR",
      "countryId": "83",
      "timeZone": "Europe/Athens",
      "hotelCount": 245
    }
  ]
}
```

---

### ✅ 3. Romantic Hotels - Romantik Turlar İçin Oteller

**Endpoint:** `GET /api/v1/public/romantic-hotels`

**Açıklama:** Romantik turlar ve balayı için uygun otelleri getirir. "Romantic", "Honeymoon", "Balayı" gibi temaları olan oteller filtrelenir.

**Query Parametreleri:**
- `count` (opsiyonel): Kaç otel getirileceği. Varsayılan: 10

**Headers:**
- `Accept-Language`: Dil kodu (tr, en, de, fr). Varsayılan: en

**Örnek Kullanım:**
```http
GET /api/v1/public/romantic-hotels?count=15
Accept-Language: en
```

**Örnek Response:**
```json
{
  "language": "en",
  "count": 15,
  "hotels": [
    {
      "id": 12345,
      "name": "Santorini Romance Suite",
      "description": "Perfect for honeymoon...",
      "stars": 5,
      "city": "Santorini",
      "country": "Greece",
      "countryCode": "GR",
      "address": "Oia Village",
      "resort": {
        "id": 456,
        "name": "Santorini"
      },
      "location": {
        "latitude": 36.4618,
        "longitude": 25.3753
      },
      "images": ["https://..."],
      "featureIds": ["1", "5"],
      "themeIds": ["8", "12"],
      "contact": {
        "phone": "+30...",
        "email": "info@...",
        "website": "https://..."
      }
    }
  ]
}
```

---

### ✅ 4. Accommodation Types - Konaklama Tipleri

**Endpoint:** `GET /api/v1/public/accommodation-types`

**Açıklama:** Themes ve features bazlı konaklama tiplerini getirir. Her tip için mevcut otel sayısı ile birlikte döner.

**Headers:**
- `Accept-Language`: Dil kodu (tr, en, de, fr). Varsayılan: en

**Örnek Kullanım:**
```http
GET /api/v1/public/accommodation-types
Accept-Language: tr
```

**Örnek Response:**
```json
{
  "language": "tr",
  "themes": [
    {
      "id": 5,
      "name": "Spa & Wellness",
      "englishName": "Spa & Wellness",
      "type": "theme",
      "hotelCount": 523
    },
    {
      "id": 8,
      "name": "Romantic",
      "englishName": "Romantic",
      "type": "theme",
      "hotelCount": 312
    }
  ],
  "features": [
    {
      "id": 1,
      "name": "Havuz",
      "englishName": null,
      "type": "feature",
      "hotelCount": 1245
    },
    {
      "id": 2,
      "name": "Wi-Fi",
      "englishName": null,
      "type": "feature",
      "hotelCount": 2103
    }
  ]
}
```

---

## 🌍 Desteklenen Diller

Tüm endpoint'ler `Accept-Language` header'ı ile çok dilli çalışır:

- `tr` - Türkçe
- `en` - İngilizce (varsayılan)
- `de` - Almanca
- `fr` - Fransızca

**Örnek:**
```http
Accept-Language: tr
```
veya
```http
Accept-Language: tr-TR
```
veya
```http
Accept-Language: tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7
```

---

## 🔑 Özellikler

- ✅ **Tamamen Public:** Tüm endpoint'ler authentication gerektirmez (`[AllowAnonymous]`)
- ✅ **Dil Desteği:** Accept-Language header ile otomatik dil filtrelemesi
- ✅ **Cache Tabanlı:** SunHotels cache servisinden çok hızlı veri çeker
- ✅ **Filtreleme:** Yıldız, ülke, tema bazlı filtreleme
- ✅ **Sıralama:** Popülerlik, otel sayısı, resim/özellik zenginliğine göre sıralama
- ✅ **Pagination:** Count parametresi ile sayfalama desteği

---

## 🧪 Test Etme

Test endpoint'lerini kullanmak için `test-public-hotels.http` dosyasını kullanabilirsiniz:

```bash
# Visual Studio Code'da REST Client extension ile
# test-public-hotels.http dosyasını açın ve "Send Request" butonuna tıklayın
```

veya curl ile:

```bash
# Featured hotels
curl -H "Accept-Language: tr" "https://localhost:7001/api/v1/public/featured-hotels?stars=5&count=10"

# Popular destinations
curl -H "Accept-Language: en" "https://localhost:7001/api/v1/public/popular-destinations?country=TR"

# Romantic hotels
curl -H "Accept-Language: de" "https://localhost:7001/api/v1/public/romantic-hotels?count=20"

# Accommodation types
curl -H "Accept-Language: fr" "https://localhost:7001/api/v1/public/accommodation-types"
```

---

## 📝 Notlar

1. **Cache Dependency:** Bu endpoint'ler SunHotels cache verilerini kullanır. Cache'in dolu olması gerekir.
2. **Sync Job:** Cache verisi `SunHotelsStaticDataSyncJob` background job'u tarafından düzenli olarak güncellenir.
3. **Performance:** Tüm veriler cache'den geldiği için çok hızlıdır.
4. **Language Fallback:** Belirtilen dilde veri bulunamazsa varsayılan olarak İngilizce döner.

---

## 🚀 Deployment

Bu endpoint'ler production'a deploy edildiğinde:

1. CORS ayarlarını kontrol edin (frontend domain'i whitelist'e ekleyin)
2. Rate limiting ekleyin (DDoS koruması için)
3. Response caching ekleyin (performance için)
4. CDN kullanın (global erişim için)

---

## 📞 İletişim

Sorularınız için lütfen development ekibi ile iletişime geçin.
