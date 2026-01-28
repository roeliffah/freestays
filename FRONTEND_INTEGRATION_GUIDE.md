# FreeStays API - Frontend Entegrasyon Rehberi

Bu döküman, Python backend'den .NET API'ye geçiş sürecinde eklenen yeni özellikler ve güncellemeleri içerir.

---

## 📋 İçindekiler

1. [SunHotels Rezervasyon Akışı](#sunhotels-rezervasyon-akışı)
2. [Stripe Test Mode Desteği](#stripe-test-mode-desteği)
3. [After-Sale (Başarısız Ödeme Takibi)](#after-sale-başarısız-ödeme-takibi)
4. [API Endpoint Listesi](#api-endpoint-listesi)
5. [Webhook Events](#webhook-events)

---

## 🏨 SunHotels Rezervasyon Akışı

### Temel Akış

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌─────────────┐
│   Frontend  │────▶│  PreBook API │────▶│ Stripe Checkout │──▶│  BookV3 API │
│  (Next.js)  │     │   (Fiyat)    │     │   (Ödeme)    │     │ (Onay)      │
└─────────────┘     └──────────────┘     └──────────────┘     └─────────────┘
```

### 1. Otel Arama

```http
GET /api/v1/sunhotels/search/hotels/v3
```

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| destination | string | Evet | Şehir adı veya destinasyon ID |
| checkIn | date | Evet | Giriş tarihi (YYYY-MM-DD) |
| checkOut | date | Evet | Çıkış tarihi (YYYY-MM-DD) |
| adults | int | Evet | Yetişkin sayısı |
| children | int | Hayır | Çocuk sayısı (varsayılan: 0) |
| rooms | int | Hayır | Oda sayısı (varsayılan: 1) |
| currency | string | Hayır | Para birimi (varsayılan: EUR) |

**Örnek:**
```javascript
const response = await fetch('/api/v1/sunhotels/search/hotels/v3?' + new URLSearchParams({
  destination: 'Amsterdam',
  checkIn: '2026-02-15',
  checkOut: '2026-02-18',
  adults: 2,
  children: 0,
  rooms: 1,
  currency: 'EUR'
}));
```

### 2. Otel Detay (Oda Resimleri Dahil) 🆕

```http
GET /api/v1/sunhotels/hotels/{hotelId}/details
```

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| checkIn | date | Evet | Giriş tarihi (YYYY-MM-DD) |
| checkOut | date | Evet | Çıkış tarihi (YYYY-MM-DD) |
| adults | int | Hayır | Yetişkin sayısı (varsayılan: 2) |
| children | int | Hayır | Çocuk sayısı (varsayılan: 0) |
| currency | string | Hayır | Para birimi (varsayılan: EUR) |
| destinationId | string | Önerilir | Destinasyon ID (daha iyi sonuçlar için) |
| resortId | string | Hayır | Resort ID |

**Örnek:**
```javascript
const response = await fetch('/api/v1/sunhotels/hotels/12345/details?' + new URLSearchParams({
  checkIn: '2026-02-15',
  checkOut: '2026-02-18',
  adults: 2,
  children: 0,
  currency: 'EUR',
  destinationId: '456'
}));
```

**Response:**
```json
{
  "hotel": {
    "id": 12345,
    "name": "Amsterdam City Hotel",
    "description": "Located in the heart of Amsterdam...",
    "category": 4,
    "stars": 4,
    "contact": {
      "address": "Damrak 123",
      "city": "Amsterdam",
      "country": "Netherlands",
      "countryCode": "NL",
      "phone": "+31 20 123 4567",
      "email": "info@hotel.com",
      "website": "https://hotel.com"
    },
    "location": {
      "latitude": 52.3702,
      "longitude": 4.8952
    },
    "images": [
      "https://sunhotels.net/HotelImages/12345/main.jpg",
      "https://sunhotels.net/HotelImages/12345/lobby.jpg"
    ],
    "features": [
      { "id": 1, "name": "WiFi" },
      { "id": 5, "name": "Restaurant" }
    ],
    "themes": [
      { "id": 10, "name": "Şehir Oteli", "englishName": "City Hotel" }
    ]
  },
  "rooms": [
    {
      "roomId": 67890,
      "roomTypeId": 111,
      "roomTypeName": "Deluxe Double Room",
      "name": "Deluxe Double Room with City View",
      "description": "Spacious room with stunning city views...",
      "images": [
        "https://sunhotels.net/RoomImages/111/room1.jpg",
        "https://sunhotels.net/RoomImages/111/room2.jpg",
        "https://sunhotels.net/RoomImages/111/bathroom.jpg"
      ],
      "mealId": 1,
      "mealName": "Breakfast Included",
      "price": {
        "total": 467.50,
        "perNight": 155.83,
        "currency": "EUR",
        "nights": 3
      },
      "pricing": {
        "originalPrice": 520.00,
        "currentPrice": 467.50,
        "discount": 52.50,
        "discountPercentage": 10.10
      },
      "availability": {
        "availableRooms": 5,
        "isAvailable": true
      },
      "policies": {
        "isRefundable": true,
        "isSuperDeal": false,
        "cancellationPolicies": [
          {
            "fromDate": "2026-02-13",
            "percentage": 100,
            "fixedAmount": null,
            "nightsCharged": 0
          }
        ],
        "earliestFreeCancellation": "2026-02-12"
      },
      "paymentMethods": [1, 2, 3]
    }
  ],
  "pricing": {
    "minPrice": 467.50,
    "currency": "EUR",
    "nights": 3,
    "pricePerNight": 155.83
  },
  "searchParams": {
    "checkIn": "2026-02-15",
    "checkOut": "2026-02-18",
    "adults": 2,
    "children": 0,
    "nights": 3
  }
}
```

#### 📷 Oda Resimleri Kullanımı

Her oda objesi içinde `images` array'i bulunur. Bu resimler statik cache'den alınır ve `roomTypeId`'ye göre eşleştirilir.

```jsx
// React örneği
{rooms.map(room => (
  <div key={room.roomId} className="room-card">
    {/* Oda Galeri */}
    <div className="room-gallery">
      {room.images.length > 0 ? (
        room.images.map((img, idx) => (
          <img 
            key={idx} 
            src={img} 
            alt={`${room.name} - ${idx + 1}`}
            loading="lazy"
          />
        ))
      ) : (
        <div className="no-image">Resim mevcut değil</div>
      )}
    </div>
    
    {/* Oda Bilgileri */}
    <h3>{room.name}</h3>
    <p>{room.description}</p>
    <p className="price">
      {room.price.total} {room.price.currency} 
      <span>({room.price.perNight}/gece)</span>
    </p>
  </div>
))}
```

> **Not:** Bazı odalarda resim olmayabilir. Bu durumda `images` array'i boş döner `[]`. Frontend'de placeholder göstermeniz önerilir.

---

### 3. PreBook (Fiyat Doğrulama)

```http
POST /api/v1/bookings/hotels/prebook
```

**⚠️ Önemli:** Bu endpoint `AllowAnonymous` - Misafir kullanıcılar da kullanabilir.

**Request Body:**
```json
{
  "hotelId": 12345,
  "roomId": 67890,
  "roomTypeId": 111,
  "mealId": 1,
  "checkInDate": "2026-02-15",
  "checkOutDate": "2026-02-18",
  "rooms": 1,
  "adults": 2,
  "children": 0,
  "childrenAges": "",
  "currency": "EUR",
  "language": "tr",
  "searchPrice": 450.00,
  "customerCountry": "NL",
  "isSuperDeal": false,
  "guestName": "John Doe",
  "guestEmail": "john@example.com",
  "guestPhone": "+31612345678"
}
```

**Response:**
```json
{
  "success": true,
  "preBookCode": "PB-ABC123XYZ",
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "totalPrice": 467.50,
  "currency": "EUR",
  "priceBreakdown": {
    "roomPrice": 450.00,
    "taxes": 17.50,
    "fees": 0
  },
  "cancellationPolicy": "Non-refundable",
  "paymentOptions": {
    "clientSecret": "pi_xxx_secret_xxx",
    "checkoutSessionUrl": null
  },
  "expiresAt": "2026-01-23T17:30:00Z"
}
```

### 4. Stripe Checkout Session Oluşturma

```http
POST /api/v1/bookings/hotels/checkout
```

**Request Body:**
```json
{
  "preBookCode": "PB-ABC123XYZ",
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "guestName": "John Doe",
  "guestEmail": "john@example.com",
  "guestPhone": "+31612345678",
  "guestCountry": "NL",
  "specialRequests": "Late check-in please",
  "successUrl": "https://travelar.eu/booking/success?session_id={CHECKOUT_SESSION_ID}",
  "cancelUrl": "https://travelar.eu/booking/cancel"
}
```

**Response:**
```json
{
  "success": true,
  "sessionId": "cs_test_xxx",
  "url": "https://checkout.stripe.com/pay/cs_test_xxx",
  "bookingId": "BK-2026-550E84",
  "expiresAt": "2026-01-23T18:00:00Z"
}
```

### 5. Ödeme Sonrası (Webhook tarafından işlenir)

Stripe ödeme başarılı olunca webhook otomatik olarak:
1. Payment kaydını günceller
2. SunHotels BookV3 çağırır (gerçek rezervasyon)
3. Confirmation email gönderir

---

## 🧪 Stripe Test Mode Desteği

### Test Mode Davranışı

Stripe **test mode**'dayken (admin panelinden `IsLive: false` ayarlı):

- ✅ PreBook API çalışır (gerçek SunHotels çağrısı)
- ✅ Stripe Checkout çalışır (test ödemeleri)
- ⚠️ **BookV3 çağrılmaz** - Gerçek rezervasyon yapılmaz
- ✅ Simüle edilmiş booking numarası döner: `TEST-{bookingId}`

**Test Mode Response:**
```json
{
  "success": true,
  "bookingId": "BK-2026-550E84",
  "sunhotelsBookingCode": "TEST-550E84",
  "status": "test_confirmed",
  "message": "TEST MODE - Rezervasyon simüle edildi. Gerçek SunHotels booking yapılmadı."
}
```

### Test Kartları

| Kart Numarası | Sonuç |
|---------------|-------|
| 4242 4242 4242 4242 | Başarılı ödeme |
| 4000 0000 0000 0002 | Kart reddedildi |
| 4000 0000 0000 9995 | Yetersiz bakiye |

**Test CVC:** Herhangi 3 haneli  
**Test Son Kullanma:** Gelecekteki herhangi bir tarih

---

## 📊 After-Sale (Başarısız Ödeme Takibi)

### Yeni Özellik

Python backend'deki `failed_payments` collection'ın .NET karşılığı.

Stripe checkout session:
- **Süresi dolduğunda** (`checkout.session.expired`)
- **Async ödeme başarısız olduğunda** (`checkout.session.async_payment_failed`)

Otomatik olarak `FailedPayments` tablosuna kaydedilir.

### Admin Panel Endpoint'leri

#### Başarısız Ödemeleri Listele
```http
GET /api/v1/admin/failed-payments?status=pending&page=1&pageSize=20
```

**Response:**
```json
{
  "items": [
    {
      "id": "guid",
      "sessionId": "cs_test_xxx",
      "bookingId": "guid",
      "customerEmail": "john@example.com",
      "customerName": "John Doe",
      "failureType": "expired",
      "amount": 467.50,
      "currency": "EUR",
      "hotelName": "Amsterdam Hotel",
      "checkIn": "2026-02-15",
      "checkOut": "2026-02-18",
      "status": "pending",
      "contactReason": null,
      "contactedAt": null,
      "createdAt": "2026-01-23T15:00:00Z"
    }
  ],
  "total": 15,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

#### İstatistikler
```http
GET /api/v1/admin/failed-payments/stats
```

**Response:**
```json
{
  "byStatus": [
    { "status": "pending", "count": 10 },
    { "status": "contacted", "count": 3 },
    { "status": "resolved", "count": 2 }
  ],
  "byFailureType": [
    { "failureType": "expired", "count": 12 },
    { "failureType": "async_payment_failed", "count": 3 }
  ],
  "pendingTotalAmount": 4500.00,
  "pendingCount": 10,
  "contactedCount": 3,
  "resolvedCount": 2
}
```

#### Follow-up Email Gönder
```http
POST /api/v1/admin/failed-payments/{id}/send-email
```

**Request Body:**
```json
{
  "reason": "no_payment",
  "customMessage": "Özel mesajınız (opsiyonel)",
  "notes": "Admin notları"
}
```

**Reason Seçenekleri:**
| Reason | Açıklama |
|--------|----------|
| `no_payment` | Ödeme tamamlanmadı |
| `stop_payment` | Ödeme durduruldu |
| `not_interested` | İlgilenmiyor |
| `new_offers` | Yeni teklifler |

#### Durum Güncelle
```http
PATCH /api/v1/admin/failed-payments/{id}/status
```

**Request Body:**
```json
{
  "status": "resolved",
  "notes": "Müşteriyle görüşüldü, yeni rezervasyon yapıldı"
}
```

**Status Seçenekleri:**
- `pending` - Beklemede
- `contacted` - İletişim kuruldu
- `resolved` - Çözüldü
- `not_interested` - İlgilenmiyor

### After-Sale Ayarları

#### Ayarları Getir
```http
GET /api/v1/admin/failed-payments/settings
```

**Response:**
```json
{
  "autoSend": false,
  "emailNoPayment": "Ödemenizin tamamlanmadığını fark ettik...",
  "emailStopPayment": "Ödemenizi durdurduğunuzu anlıyoruz...",
  "emailNotInterested": "Gittiğinizi görmek bizi üzdü...",
  "emailNewOffers": "İlginizi çekebilecek harika yeni tekliflerimiz var!"
}
```

#### Ayarları Güncelle
```http
PUT /api/v1/admin/failed-payments/settings
```

**Request Body:**
```json
{
  "autoSend": true,
  "emailNoPayment": "Özel mesaj şablonu...",
  "emailStopPayment": "Özel mesaj şablonu...",
  "emailNotInterested": "Özel mesaj şablonu...",
  "emailNewOffers": "Özel mesaj şablonu..."
}
```

---

## 📡 API Endpoint Listesi

### Public Endpoints (Auth gerektirmez)

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/v1/sunhotels/search/hotels/v3` | Otel arama |
| GET | `/api/v1/sunhotels/hotel/{hotelId}/details` | Otel detayı |
| POST | `/api/v1/bookings/hotels/prebook` | PreBook (misafir de kullanabilir) |
| POST | `/api/v1/bookings/hotels/checkout` | Stripe Checkout Session |
| POST | `/api/v1/webhooks/stripe` | Stripe Webhook |

### Authenticated Endpoints

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/v1/bookings/my` | Kullanıcının rezervasyonları |
| GET | `/api/v1/bookings/{id}` | Rezervasyon detayı |
| GET | `/api/v1/bookings/{id}/voucher` | Voucher PDF indir |

### Admin Endpoints

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/v1/admin/failed-payments` | Başarısız ödemeler |
| GET | `/api/v1/admin/failed-payments/stats` | İstatistikler |
| GET | `/api/v1/admin/failed-payments/{id}` | Detay |
| POST | `/api/v1/admin/failed-payments/{id}/send-email` | Email gönder |
| PATCH | `/api/v1/admin/failed-payments/{id}/status` | Durum güncelle |
| GET | `/api/v1/admin/failed-payments/settings` | After-sale ayarları |
| PUT | `/api/v1/admin/failed-payments/settings` | Ayarları güncelle |

---

## 🔔 Webhook Events

### Stripe Webhook'ları

Webhook URL: `POST /api/v1/webhooks/stripe`

| Event | Açıklama | Aksiyon |
|-------|----------|---------|
| `checkout.session.completed` | Ödeme başarılı | BookV3 çağır, email gönder |
| `payment_intent.succeeded` | PaymentIntent başarılı | Booking güncelle |
| `payment_intent.payment_failed` | Ödeme başarısız | Status: Failed |
| `charge.refunded` | İade yapıldı | Status: Refunded |
| `checkout.session.expired` | Session süresi doldu | FailedPayment kaydet |
| `checkout.session.async_payment_failed` | Async ödeme başarısız | FailedPayment kaydet |

### Webhook Metadata

Stripe Checkout Session oluştururken gönderilen metadata:

```json
{
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "preBookCode": "PB-ABC123XYZ",
  "guestName": "John Doe",
  "guestCountry": "NL",
  "hotelId": "12345",
  "source": "freestays_api"
}
```

---

## 🔧 Frontend Checklist

### Booking Flow

- [ ] Otel arama sayfası
- [ ] Oda seçimi ve PreBook çağrısı
- [ ] Misafir bilgi formu
- [ ] Stripe Checkout'a yönlendirme
- [ ] Success sayfası (`/booking/success`)
- [ ] Cancel sayfası (`/booking/cancel`)

### Admin Panel

- [ ] Failed Payments listesi
- [ ] İstatistik dashboard
- [ ] Email gönderme modal
- [ ] After-sale ayarları sayfası

### Test Mode Banner

Test modunda kullanıcıya banner göster:
```jsx
{isTestMode && (
  <div className="bg-yellow-100 text-yellow-800 p-2 text-center">
    ⚠️ TEST MODE - Gerçek rezervasyon yapılmayacak
  </div>
)}
```

---

## 📞 Destek

Sorularınız için: backend@freestays.com

Son güncelleme: 23 Ocak 2026
