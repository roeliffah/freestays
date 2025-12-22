# Featured Content Management - Backend API Requirements

## Overview
Admin panelinde ana sayfada gösterilecek öne çıkan otelleri, destinasyonları ve içerikleri yönetmek için bir sistem kurulması gerekiyor. Bu sayfa, mevsimsel değişikliklere göre (yaz/kış), kampanyalara göre veya manuel olarak içerik düzenlemesine olanak sağlayacak.

## Frontend Mevcut Yapı

### 1. Ana Sayfa Kullanımı
- **Dosya**: `app/[locale]/page.tsx`
- **Component**: `components/home/PopularHotels.tsx`
- **API Çağrısı**: `api.hotels.getFeatured()`

### 2. Data Yapısı
Frontend şu anda aşağıdaki endpointten veri çekiyor:
```
GET /Hotels/featured?count=10
```

Mevcut hotel data yapısı (`data/featured-hotels.json`):
```json
{
  "hotelId": "228001",
  "hotelName": "Palace Antalya Resort",
  "hotelCode": "228001",
  "category": 4,
  "categoryName": "4 Star",
  "destinationId": "228",
  "destinationName": "Antalya",
  "regionId": "228",
  "regionName": "Antalya",
  "country": "Turkey",
  "countryCode": "TR",
  "address": "...",
  "location": {
    "latitude": 36.74208034930731,
    "longitude": 30.590228448231215
  },
  "images": [...],
  "facilities": [...],
  "rating": 4.8,
  "priceFrom": 120,
  "currency": "EUR"
}
```

### 3. Featured Destinations
Mevcut destinations data yapısı (`data/featured-destinations.json`):
```json
{
  "countries": [
    {
      "code": "TR",
      "name": "Turkey",
      "flag": "🇹🇷",
      "cities": [
        {
          "id": "228",
          "code": "AYT",
          "name": "Antalya",
          "country": "Turkey",
          "countryCode": "TR"
        }
      ]
    }
  ]
}
```

## Backend API Gereksinimleri

### 1. Featured Hotels Management

#### Admin Endpoints

##### GET /admin/featured-content/hotels
Ana sayfada gösterilecek featured otelleri listele.

**Query Parameters:**
- `page` (int, optional): Sayfa numarası
- `pageSize` (int, optional): Sayfa başına kayıt sayısı
- `status` (string, optional): active, inactive, scheduled
- `season` (string, optional): summer, winter, spring, autumn, all-season
- `category` (string, optional): beach, ski, city, boutique, luxury, budget

**Response:**
```json
{
  "items": [
    {
      "id": "fc-001",
      "hotelId": "228001",
      "hotel": {
        "hotelId": "228001",
        "hotelName": "Palace Antalya Resort",
        "destinationName": "Antalya",
        "country": "Turkey",
        "category": 4,
        "rating": 4.8,
        "images": ["url1", "url2"],
        "priceFrom": 120,
        "currency": "EUR"
      },
      "priority": 1,
      "status": "active",
      "season": "summer",
      "category": "beach",
      "validFrom": "2025-06-01",
      "validUntil": "2025-09-30",
      "campaignName": "Summer 2025",
      "discountPercentage": 20,
      "createdAt": "2025-01-15T10:00:00Z",
      "updatedAt": "2025-01-15T10:00:00Z"
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20
}
```

##### POST /admin/featured-content/hotels
Yeni featured hotel ekle.

**Request Body:**
```json
{
  "hotelId": "228001",
  "priority": 1,
  "status": "active",
  "season": "summer",
  "category": "beach",
  "validFrom": "2025-06-01",
  "validUntil": "2025-09-30",
  "campaignName": "Summer 2025",
  "discountPercentage": 20
}
```

**Response:** 201 Created
```json
{
  "id": "fc-001",
  "message": "Featured hotel added successfully"
}
```

##### PUT /admin/featured-content/hotels/{id}
Featured hotel bilgilerini güncelle.

**Request Body:**
```json
{
  "priority": 2,
  "status": "inactive",
  "validUntil": "2025-10-31"
}
```

**Response:** 200 OK

##### DELETE /admin/featured-content/hotels/{id}
Featured hotel kaydını sil.

**Response:** 204 No Content

##### PATCH /admin/featured-content/hotels/{id}/priority
Priority (sıralama) değiştir.

**Request Body:**
```json
{
  "priority": 1
}
```

**Response:** 200 OK

##### PATCH /admin/featured-content/hotels/bulk-priority
Toplu sıralama değiştir (drag & drop için).

**Request Body:**
```json
{
  "items": [
    { "id": "fc-001", "priority": 1 },
    { "id": "fc-002", "priority": 2 },
    { "id": "fc-003", "priority": 3 }
  ]
}
```

**Response:** 200 OK

### 2. Featured Destinations Management

##### GET /admin/featured-content/destinations
Featured destinasyonları listele.

**Response:**
```json
{
  "items": [
    {
      "id": "fd-001",
      "destinationId": "228",
      "destinationName": "Antalya",
      "countryCode": "TR",
      "country": "Turkey",
      "priority": 1,
      "status": "active",
      "season": "all-season",
      "image": "url",
      "hotelCount": 1250,
      "averagePrice": 85,
      "description": "Mediterranean paradise...",
      "validFrom": "2025-01-01",
      "validUntil": "2025-12-31"
    }
  ]
}
```

##### POST /admin/featured-content/destinations
Yeni featured destination ekle.

##### PUT /admin/featured-content/destinations/{id}
Featured destination güncelle.

##### DELETE /admin/featured-content/destinations/{id}
Featured destination sil.

### 3. Public Endpoints (Frontend için)

##### GET /featured-content/hotels
Ana sayfada gösterilecek aktif featured otelleri getir.

**Query Parameters:**
- `count` (int, optional, default: 10): Kaç otel getirileceği
- `season` (string, optional): Mevsime göre filtrele (otomatik detect edilebilir)
- `category` (string, optional): Kategoriye göre filtrele

**Response:**
```json
{
  "data": [
    {
      "id": "228001",
      "name": "Palace Antalya Resort",
      "city": "Antalya",
      "country": "Turkey",
      "rating": 4.8,
      "stars": 4,
      "priceFrom": 96,
      "originalPrice": 120,
      "discountPercentage": 20,
      "currency": "EUR",
      "images": ["url1", "url2"],
      "category": "beach",
      "campaignName": "Summer Sale"
    }
  ]
}
```

##### GET /featured-content/destinations
Ana sayfada gösterilecek aktif featured destinasyonları getir.

**Response:**
```json
{
  "data": [
    {
      "id": "228",
      "name": "Antalya",
      "country": "Turkey",
      "countryCode": "TR",
      "image": "url",
      "hotelCount": 1250,
      "averagePrice": 85,
      "description": "Mediterranean paradise..."
    }
  ]
}
```

## Database Schema Önerisi

### FeaturedHotels Tablosu
```sql
CREATE TABLE FeaturedHotels (
    Id VARCHAR(50) PRIMARY KEY,
    HotelId VARCHAR(50) NOT NULL,
    Priority INT NOT NULL DEFAULT 999,
    Status VARCHAR(20) NOT NULL, -- active, inactive, scheduled
    Season VARCHAR(20), -- summer, winter, spring, autumn, all-season
    Category VARCHAR(50), -- beach, ski, city, boutique, luxury, budget
    ValidFrom DATETIME,
    ValidUntil DATETIME,
    CampaignName VARCHAR(200),
    DiscountPercentage DECIMAL(5,2),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy VARCHAR(100),
    FOREIGN KEY (HotelId) REFERENCES Hotels(HotelId)
);

CREATE INDEX IX_FeaturedHotels_Priority ON FeaturedHotels(Priority);
CREATE INDEX IX_FeaturedHotels_Status ON FeaturedHotels(Status);
CREATE INDEX IX_FeaturedHotels_Season ON FeaturedHotels(Season);
CREATE INDEX IX_FeaturedHotels_ValidDates ON FeaturedHotels(ValidFrom, ValidUntil);
```

### FeaturedDestinations Tablosu
```sql
CREATE TABLE FeaturedDestinations (
    Id VARCHAR(50) PRIMARY KEY,
    DestinationId VARCHAR(50) NOT NULL,
    DestinationName VARCHAR(200) NOT NULL,
    CountryCode VARCHAR(2) NOT NULL,
    Country VARCHAR(100) NOT NULL,
    Priority INT NOT NULL DEFAULT 999,
    Status VARCHAR(20) NOT NULL,
    Season VARCHAR(20),
    Image VARCHAR(500),
    Description TEXT,
    ValidFrom DATETIME,
    ValidUntil DATETIME,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
```

## Business Logic

### Otomatik Mevsim Algılama
Backend, mevcut tarihe göre otomatik mevsim algılayabilir:
- Aralık-Şubat: winter
- Mart-Mayıs: spring
- Haziran-Ağustos: summer
- Eylül-Kasım: autumn

### Priority Kuralları
- Düşük priority değeri = Daha önde gösterim (1 en önde)
- Aynı priority'ye sahip kayıtlar createdAt'e göre sıralanır
- Bulk update sırasında tüm priority değerleri yeniden hesaplanır

### Status Kuralları
- **active**: Şu anda gösteriliyor
- **inactive**: Gösterilmiyor
- **scheduled**: Gelecek tarihte gösterilecek (validFrom/validUntil kontrolü)

### Validasyon Kuralları
1. Aynı otel birden fazla aktif kampanyada olamaz
2. ValidFrom < ValidUntil olmalı
3. Priority değeri unique olmalı (aynı season/category içinde)
4. DiscountPercentage 0-100 arası olmalı

## Admin Panel UI Gereksinimleri

### Sayfa Özellikleri
1. **Filtreleme**: Status, Season, Category
2. **Arama**: Hotel adı, destination
3. **Sıralama**: Drag & drop ile priority değiştirme
4. **Bulk Actions**: Toplu aktif/pasif yapma, silme
5. **Modal/Drawer**: Yeni ekle/düzenle
6. **Preview**: Değişikliklerin ana sayfada nasıl görüneceğini önizle

### Form Alanları
- Hotel/Destination Seçimi (dropdown/autocomplete)
- Priority (number input)
- Status (select: active/inactive/scheduled)
- Season (multi-select)
- Category (select)
- Valid From/Until (date range picker)
- Campaign Name (text input)
- Discount Percentage (number input, 0-100)

## Öneriler
1. **Caching**: Featured content yüksek trafiğe maruz kalacağı için Redis cache kullanılmalı
2. **CDN**: Image URL'leri CDN üzerinden serve edilmeli
3. **Scheduled Jobs**: Expired kampanyaları otomatik inactive yapan bir job
4. **Analytics**: Hangi featured hotel'lerin daha çok tıklandığını izle
5. **A/B Testing**: Farklı sıralamalar ve kombinasyonlar test edilebilmeli

## Migration Plan
1. Database tablolarını oluştur
2. Admin API endpoint'lerini implement et
3. Public endpoint'leri implement et ve cache ekle
4. Admin panel sayfasını oluştur
5. Mevcut static data'yı database'e migrate et
6. Frontend'i yeni API'ye bağla
7. Test ve production deployment
