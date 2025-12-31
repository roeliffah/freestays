# Frontend API Parameters - Hotel Search

## Endpoint
```
POST /api/v1/SunHotels/search/unified
```

## Zorunlu Parametreler

### Dinamik Arama için (Tarihli - Canlı Fiyatlar)
```typescript
{
  checkInDate: string,        // Format: "YYYY-MM-DD" (örn: "2026-01-15")
  checkOutDate: string,       // Format: "YYYY-MM-DD" (örn: "2026-01-20")
  adults: number              // Minimum: 1, Maximum: 9
}
```

### Statik Arama için (Tarihsiz - Filtre Bazlı)
En az **birini** göndermelisiniz:
- `destinationIds` (string[])
- `countryCodes` (string[])
- `themeIds` (number[])
- `featureIds` (number[])
- `searchTerm` (string)

## Zorunlu Olmayan Parametreler

### Genel Parametreler
```typescript
{
  language?: string,          // Default: "en" | Seçenekler: "en", "tr", "de", "fr"
  currency?: string,          // Default: "EUR" | Seçenekler: "EUR", "USD", "TRY"
  page?: number,              // Default: 1 | Minimum: 1
  pageSize?: number           // Default: 20 | Minimum: 1, Maximum: 100
}
```

### Dinamik Arama Parametreleri
```typescript
{
  children?: number,          // Default: 0 | Maximum: 9
  numberOfRooms?: number      // Default: 1 | Minimum: 1, Maximum: 9
}
```

### Filtre Parametreleri (Array Tipleri)
```typescript
{
  // Destinasyon filtreleri
  destinationIds?: string[],  // ["10025", "10026"]
  resortIds?: number[],       // [1234, 5678]
  
  // Ülke filtreleri  
  countryCodes?: string[],    // ["TR", "GR", "ES"]
  countryNames?: string[],    // ["Turkey", "Greece", "Spain"]
  
  // Tema/Özellik filtreleri
  themeIds?: number[],        // [1, 5, 12] (Beach, City, Mountain)
  featureIds?: number[],      // [10, 25, 30] (Pool, Spa, WiFi)
  mealIds?: number[],         // [1, 3, 5] (Breakfast, Half Board, All Inclusive)
  
  // Yıldız filtreleri
  minStars?: number,          // 1-5
  maxStars?: number,          // 1-5
  
  // Metin arama
  searchTerm?: string         // "Rixos" | "Beach Hotel" | "Antalya"
}
```

## Kullanım Örnekleri

### 1. Dinamik Arama (Tarihli - Canlı Fiyatlar)
```typescript
const dynamicSearchRequest = {
  checkInDate: "2026-06-15",
  checkOutDate: "2026-06-20",
  adults: 2,
  children: 1,
  numberOfRooms: 1,
  currency: "EUR",
  language: "en",
  destinationIds: ["10025"],  // İsteğe bağlı destinasyon filtresi
  page: 1,
  pageSize: 20
};
```

### 2. Statik Arama (Tarihsiz - Filtre Bazlı)
```typescript
const staticSearchRequest = {
  language: "tr",
  destinationIds: ["10025", "10026", "10027"],
  themeIds: [1, 5],           // Beach + City
  featureIds: [10, 25],       // Pool + Spa
  minStars: 4,
  maxStars: 5,
  page: 1,
  pageSize: 10
};
```

### 3. Ülke Bazlı Arama
```typescript
const countrySearchRequest = {
  countryCodes: ["TR", "GR"],
  themeIds: [1],              // Beach
  mealIds: [5],               // All Inclusive
  page: 1,
  pageSize: 50
};
```

### 4. Metin Bazlı Arama
```typescript
const textSearchRequest = {
  searchTerm: "Rixos",
  language: "en",
  currency: "USD",
  page: 1,
  pageSize: 20
};
```

## Response Formatı
```typescript
{
  hotels: HotelDto[],         // Otel listesi
  totalCount: number,         // Toplam otel sayısı
  totalPages: number,         // Toplam sayfa sayısı
  currentPage: number,        // Mevcut sayfa
  pageSize: number,           // Sayfa boyutu
  searchType: "dynamic" | "static",  // Arama tipi
  hasPricing: boolean,        // Fiyat bilgisi var mı?
  priceMessage?: string       // Fiyat mesajı (tarih seç vs.)
}
```

## Önemli Notlar

1. **Tarih Formatı**: Mutlaka `YYYY-MM-DD` formatında gönderin
2. **Gelecek Tarihler**: `checkInDate` geçmiş tarih olamaz
3. **Array Parametreler**: Boş array `[]` göndermeyin, hiç göndermeyin veya değerli array gönderin
4. **Dinamik vs Statik**: 
   - Tarih varsa → Dinamik arama (SunHotels NonStatic API)
   - Tarih yoksa → Statik arama (Cache tabloları)
5. **Pagination**: `page` 1'den başlar, `pageSize` maksimum 100
6. **Currency**: Dinamik aramada fiyatlar bu para biriminde dönecek
7. **Language**: Otel açıklamaları ve mesajlar bu dilde gelecek