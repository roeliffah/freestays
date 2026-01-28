# Admin Bookings API Documentation

Bu dokümantasyon, admin panelden rezervasyon yönetimi için kullanılacak API endpoint'lerini açıklar.

## Base URL
```
/api/v1/admin/bookings
```

## Authentication
Tüm endpoint'ler **Admin** veya **SuperAdmin** rolü gerektirir.

```
Authorization: Bearer <jwt_token>
```

---

## 📋 Endpoint Listesi

| Endpoint | Method | Açıklama |
|----------|--------|----------|
| `/` | GET | Tüm rezervasyonları listeler |
| `/failed-confirmations` | GET | Ödeme alınmış ama SunHotels başarısız olanları listeler |
| `/{id}` | GET | Rezervasyon detayı |
| `/{id}/retry-sunhotels` | POST | SunHotels'e tekrar rezervasyon gönderir |
| `/{id}/refund` | POST | Stripe üzerinden iade yapar |
| `/{id}/refund-status` | GET | İade durumunu kontrol eder |
| `/{id}/cancel-sunhotels` | POST | SunHotels rezervasyonunu iptal eder |
| `/stats` | GET | Rezervasyon istatistikleri |

---

## 🔍 1. Rezervasyonları Listele

### `GET /api/v1/admin/bookings`

Tüm rezervasyonları sayfalama ve filtreleme ile listeler.

#### Query Parameters

| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| `status` | enum | Hayır | `Pending`, `Confirmed`, `Cancelled`, `Completed`, `Failed`, `Refunded`, `ConfirmationFailed` |
| `type` | enum | Hayır | `Hotel`, `Flight`, `Car` |
| `fromDate` | datetime | Hayır | Başlangıç tarihi (ISO 8601) |
| `toDate` | datetime | Hayır | Bitiş tarihi (ISO 8601) |
| `search` | string | Hayır | Misafir adı, email veya konfirmasyon kodu arar |
| `page` | int | Hayır | Sayfa numarası (varsayılan: 1) |
| `pageSize` | int | Hayır | Sayfa boyutu (varsayılan: 20) |

#### Örnek İstek
```bash
GET /api/v1/admin/bookings?status=ConfirmationFailed&page=1&pageSize=10
```

#### Örnek Yanıt
```json
{
  "items": [
    {
      "id": "5e453589-74f5-4822-b8ef-8a0b7f3646f4",
      "type": "Hotel",
      "status": "ConfirmationFailed",
      "totalPrice": 1450.00,
      "currency": "EUR",
      "guestName": "John Doe",
      "guestEmail": "john@example.com",
      "hotelName": "Twin/Double room - Economy",
      "checkIn": "2026-02-01T00:00:00",
      "checkOut": "2026-02-07T00:00:00",
      "confirmationCode": null,
      "paymentStatus": "Completed",
      "stripePaymentIntentId": "pi_xxx",
      "createdAt": "2026-01-27T22:30:00"
    }
  ],
  "total": 5,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

---

## ⚠️ 2. Başarısız Konfirmasyonları Listele

### `GET /api/v1/admin/bookings/failed-confirmations`

Ödeme alınmış ama SunHotels rezervasyonu başarısız olmuş rezervasyonları listeler.

> **Önemli:** Bu liste `status=ConfirmationFailed` VE `paymentStatus=Completed` olan rezervasyonları döner.

#### Örnek Yanıt
```json
{
  "items": [
    {
      "bookingId": "5e453589-74f5-4822-b8ef-8a0b7f3646f4",
      "guestName": "John Doe",
      "guestEmail": "john@example.com",
      "guestPhone": "+90 555 123 4567",
      "externalHotelId": 577824,
      "roomId": 12345,
      "roomTypeName": "Twin/Double room - Economy",
      "mealId": 1,
      "checkIn": "2026-02-01T00:00:00",
      "checkOut": "2026-02-07T00:00:00",
      "adults": 2,
      "children": 0,
      "preBookCode": "PB-xxx-xxx",
      "isSuperDeal": false,
      "specialRequests": "Late check-in requested",
      "totalPrice": 1450.00,
      "currency": "EUR",
      "paymentAmount": 1450.00,
      "paymentStatus": "Completed",
      "stripePaymentIntentId": "pi_xxx",
      "paidAt": "2026-01-27T22:35:00",
      "notes": "[Auto-Confirm Error] PreBook failed...",
      "createdAt": "2026-01-27T22:30:00",
      "canRetry": true,
      "canRefund": true,
      "isRefundable": false,
      "freeCancellationDeadline": null,
      "cancellationPercentage": 100,
      "maxRefundableAmount": 0,
      "cancellationPolicyText": "Non-refundable: 100% cancellation fee applies from 01 Jan 2026"
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

#### Yanıt Açıklamaları

| Alan | Açıklama |
|------|----------|
| `canRetry` | `true` ise SunHotels'e tekrar gönderilebilir (PreBookCode mevcut) |
| `canRefund` | `true` ise Stripe iadesi yapılabilir (StripePaymentIntentId mevcut) |
| `isRefundable` | 🆕 `false` ise oda iade edilemez (non-refundable) |
| `freeCancellationDeadline` | 🆕 Ücretsiz iptal son tarihi (ISO 8601). Bu tarihten sonra iptal ücreti uygulanır |
| `cancellationPercentage` | 🆕 İptal ücreti yüzdesi (0-100). 100 = tam iptal ücreti |
| `maxRefundableAmount` | 🆕 Politikaya göre maksimum iade edilebilir tutar |
| `cancellationPolicyText` | 🆕 İnsan okunabilir iptal politikası açıklaması |

---

## 📄 3. Rezervasyon Detayı

### `GET /api/v1/admin/bookings/{id}`

Tek bir rezervasyonun tüm detaylarını döner.

#### Örnek Yanıt
```json
{
  "id": "5e453589-74f5-4822-b8ef-8a0b7f3646f4",
  "userId": "abc123",
  "type": "Hotel",
  "status": "Confirmed",
  "totalPrice": 1450.00,
  "commission": 145.00,
  "currency": "EUR",
  "couponDiscount": 0,
  "notes": null,
  "createdAt": "2026-01-27T22:30:00",
  "updatedAt": "2026-01-27T22:35:00",
  "hotelBooking": {
    "hotelName": "Grand Hotel Istanbul",
    "roomTypeName": "Twin/Double room - Economy",
    "boardTypeName": "All Inclusive",
    "rooms": 1,
    "checkIn": "2026-02-01T00:00:00",
    "checkOut": "2026-02-07T00:00:00",
    "adults": 2,
    "children": 0,
    "guestName": "John Doe",
    "guestEmail": "john@example.com",
    "guestPhone": "+90 555 123 4567",
    "specialRequests": "Late check-in",
    "sunhotelsBookingCode": "SH28161157",
    "hotelConfirmationNumber": null,
    "totalPrice": 1450.00,
    "currency": "EUR",
    "taxAmount": null,
    "id": "xxx",
    "externalHotelId": 577824,
    "roomId": 12345,
    "roomTypeId": 6789,
    "mealId": 1,
    "mealName": "All Inclusive",
    "preBookCode": "PB-xxx",
    "confirmationCode": "SH28161157",
    "voucher": "https://voucher.travel/?id=xxx",
    "invoiceRef": null,
    "hotelAddress": "CIHANGIR MAH...",
    "hotelPhone": "+90 552 156 02 02",
    "hotelNotes": "Check-in 15:00...",
    "cancellationPolicies": "[\"Non-refundable...\"]",
    "isSuperDeal": false,
    "sunHotelsBookingDate": "2026-01-27T23:10:20",
    "confirmationEmailSent": true,
    "confirmationEmailSentAt": "2026-01-27T23:11:00",
    "isRefundable": true,
    "freeCancellationDeadline": "2026-01-30T23:59:59",
    "cancellationPercentage": 0,
    "maxRefundableAmount": 1450.00,
    "cancellationPolicyText": "Free cancellation until 30 Jan 2026. After: 50% fee until 01 Feb 2026"
  },
  "payment": {
    "status": "Completed",
    "paidAt": "2026-01-27T22:35:00",
    "stripeSessionId": null,
    "stripePaymentIntentId": "pi_xxx",
    "amount": 1450.00,
    "currency": "EUR",
    "id": "xxx",
    "stripePaymentId": "ch_xxx",
    "failureReason": null
  }
}
```

#### hotelBooking Alanları

| Alan | Tip | Açıklama |
|------|-----|----------|
| `hotelName` | string | Otel adı |
| `roomTypeName` | string | Oda tipi adı |
| `boardTypeName` | string | Yemek planı (All Inclusive, Bed & Breakfast vb.) |
| `rooms` | int | Oda sayısı |
| `sunhotelsBookingCode` | string | SunHotels rezervasyon kodu |
| `hotelConfirmationNumber` | string? | Otel onay numarası (opsiyonel) |
| `totalPrice` | decimal | Oda toplam fiyatı |
| `taxAmount` | decimal? | Vergi tutarı (opsiyonel) |

#### payment Alanları

| Alan | Tip | Açıklama |
|------|-----|----------|
| `status` | string | Ödeme durumu: `Pending`, `Completed`, `Failed`, `Refunded` |
| `paidAt` | datetime? | Ödeme tarihi |
| `stripeSessionId` | string? | Stripe checkout session ID (opsiyonel) |
| `stripePaymentIntentId` | string | Stripe payment intent ID (refund için gerekli) |
| `amount` | decimal | Ödenen tutar |

---

## 🔄 4. SunHotels'e Tekrar Gönder (Retry)

### `POST /api/v1/admin/bookings/{id}/retry-sunhotels`

Başarısız bir SunHotels rezervasyonunu tekrar gönderir.

> **Ne zaman kullanılır:** `status=ConfirmationFailed` ve `canRetry=true` ise

#### Request Body
```json
{
  "customerCountry": "TR",
  "sendConfirmationEmail": true
}
```

| Parametre | Tip | Zorunlu | Varsayılan | Açıklama |
|-----------|-----|---------|------------|----------|
| `customerCountry` | string | Hayır | "TR" | ISO ülke kodu |
| `sendConfirmationEmail` | bool | Hayır | true | Başarılı olursa onay emaili gönder |

#### Başarılı Yanıt (200)
```json
{
  "success": true,
  "message": "SunHotels booking confirmed successfully",
  "confirmationCode": "SH28161157",
  "voucher": "https://voucher.travel/?id=xxx",
  "bookingStatus": "Confirmed"
}
```

#### Hata Yanıtı (400)
```json
{
  "success": false,
  "message": "SunHotels booking failed",
  "error": "PreBook code expired"
}
```

---

## 💰 5. Stripe İade Yap

### `POST /api/v1/admin/bookings/{id}/refund`

Stripe üzerinden tam veya kısmi iade yapar.

> **🆕 Önemli:** Bu endpoint artık iptal politikasını kontrol eder ve non-refundable rezervasyonlarda uyarı verir.

#### Request Body
```json
{
  "amount": 100.00,
  "reason": "requested_by_customer",
  "adminNote": "Müşteri talebi ile iptal",
  "sendRefundEmail": true,
  "forceRefund": false
}
```

| Parametre | Tip | Zorunlu | Varsayılan | Açıklama |
|-----------|-----|---------|------------|----------|
| `amount` | decimal | Hayır | null | İade tutarı. `null` = tam iade |
| `reason` | string | Hayır | "requested_by_customer" | `duplicate`, `fraudulent`, `requested_by_customer` |
| `adminNote` | string | Hayır | null | Admin notu (metadata) |
| `sendRefundEmail` | bool | Hayır | true | Müşteriye email gönder |
| `forceRefund` | bool | Hayır | false | 🆕 Non-refundable olsa bile iadeyi zorla yap |

#### Non-Refundable Uyarısı (400)

Non-refundable bir rezervasyonda `forceRefund: false` ise:

```json
{
  "success": false,
  "message": "Booking is non-refundable. Add 'forceRefund: true' to process anyway.",
  "warning": "⚠️ WARNING: This booking is NON-REFUNDABLE. SunHotels will charge 100% cancellation fee. Policy: Non-refundable: 100% cancellation fee applies from 01 Jan 2026",
  "recommendedRefundAmount": 0,
  "cancellationPolicy": "Non-refundable: 100% cancellation fee applies from 01 Jan 2026"
}
```

#### Ücretsiz İptal Süresi Geçmiş Uyarısı (200 with warning)

```json
{
  "success": true,
  "message": "Partial refund processed successfully",
  "refundId": "re_xxx",
  "refundAmount": 725.00,
  "currency": "EUR",
  "refundStatus": "succeeded",
  "bookingStatus": "Confirmed",
  "policyInfo": {
    "warning": "⚠️ WARNING: Free cancellation deadline has passed (25 Jan 2026). Cancellation fee: 50% = 725.00 EUR. Recommended refund: 725.00 EUR",
    "recommendedRefundAmount": 725.00
  }
}
```

#### Başarılı Yanıt (200)
```json
{
  "success": true,
  "message": "Full refund processed successfully",
  "refundId": "re_xxx",
  "refundAmount": 1450.00,
  "currency": "EUR",
  "refundStatus": "succeeded",
  "bookingStatus": "Refunded",
  "policyInfo": null
}
```

#### Hata Yanıtı (400)
```json
{
  "success": false,
  "message": "Stripe refund failed",
  "error": "Charge has already been refunded",
  "code": "charge_already_refunded"
}
```

---

## 📊 6. İade Durumu Kontrol

### `GET /api/v1/admin/bookings/{id}/refund-status`

Bir rezervasyonun Stripe iade geçmişini getirir.

#### Örnek Yanıt
```json
{
  "bookingId": "5e453589-74f5-4822-b8ef-8a0b7f3646f4",
  "paymentIntentId": "pi_xxx",
  "originalAmount": 1450.00,
  "totalRefunded": 500.00,
  "remainingAmount": 950.00,
  "refunds": [
    {
      "refundId": "re_xxx",
      "amount": 500.00,
      "currency": "EUR",
      "status": "succeeded",
      "reason": "requested_by_customer",
      "createdAt": "2026-01-28T10:00:00"
    }
  ]
}
```

---

## ❌ 7. SunHotels Rezervasyonu İptal Et

### `POST /api/v1/admin/bookings/{id}/cancel-sunhotels`

SunHotels'teki rezervasyonu iptal eder.

> **Önemli:** Bu işlem sadece SunHotels tarafını iptal eder. Stripe iadesi için `processRefund: true` gönderin.

#### Request Body
```json
{
  "processRefund": true
}
```

| Parametre | Tip | Zorunlu | Varsayılan | Açıklama |
|-----------|-----|---------|------------|----------|
| `processRefund` | bool | Hayır | false | İptal sonrası otomatik Stripe iadesi yap |

#### Başarılı Yanıt (200)
```json
{
  "success": true,
  "message": "SunHotels booking cancelled successfully",
  "cancellationFee": 150.00,
  "currency": "EUR",
  "paymentMethods": [
    {
      "id": 1,
      "name": "Invoice",
      "cancellationFees": [
        { "amount": 150.00, "currency": "EUR" }
      ],
      "cancellations": [
        {
          "type": "active",
          "policyText": "Cancellation fee applies..."
        }
      ]
    }
  ],
  "bookingStatus": "Cancelled"
}
```

#### Hata Yanıtı (400)
```json
{
  "success": false,
  "message": "SunHotels cancellation failed",
  "error": "Booking not found or already cancelled"
}
```

---

## 📈 8. İstatistikler

### `GET /api/v1/admin/bookings/stats`

Genel rezervasyon istatistiklerini döner.

#### Örnek Yanıt
```json
{
  "byStatus": [
    { "status": "Pending", "count": 5 },
    { "status": "Confirmed", "count": 120 },
    { "status": "Cancelled", "count": 15 },
    { "status": "ConfirmationFailed", "count": 3 },
    { "status": "Refunded", "count": 8 }
  ],
  "totalRevenue": 175000.00,
  "totalRefunded": 12000.00,
  "failedConfirmations": 3,
  "needsAttention": 3
}
```

---

## 🎯 Kullanım Senaryoları

### Senaryo 1: Başarısız Rezervasyonu Yeniden Gönderme

```
1. GET /api/v1/admin/bookings/failed-confirmations
   → canRetry: true olanları bul

2. POST /api/v1/admin/bookings/{id}/retry-sunhotels
   → { "sendConfirmationEmail": true }
   
3. Başarılı ise müşteriye email gider, başarısız ise error döner
```

### Senaryo 2: Müşteri İptal İstedi (Tam İade)

```
1. POST /api/v1/admin/bookings/{id}/cancel-sunhotels
   → { "processRefund": true }
   
2. Hem SunHotels iptal edilir hem Stripe iadesi yapılır
   (İptal ücreti varsa düşülür)
```

### Senaryo 3: Kısmi İade

```
1. POST /api/v1/admin/bookings/{id}/refund
   → { "amount": 500.00, "reason": "requested_by_customer" }
   
2. GET /api/v1/admin/bookings/{id}/refund-status
   → Toplam iade tutarını kontrol et
```

### Senaryo 4: Dashboard için Dikkat Gerektiren Rezervasyonlar

```
1. GET /api/v1/admin/bookings/stats
   → needsAttention: 3 (ConfirmationFailed sayısı)

2. GET /api/v1/admin/bookings/failed-confirmations
   → Liste halinde detayları göster
```

---

## 📝 Status Değerleri

| Status | Kod | Açıklama |
|--------|-----|----------|
| `Pending` | 0 | Ödeme bekleniyor |
| `Confirmed` | 1 | Rezervasyon onaylı |
| `Cancelled` | 2 | İptal edildi |
| `Completed` | 3 | Konaklama tamamlandı |
| `Failed` | 4 | Başarısız |
| `Refunded` | 5 | İade edildi |
| `ConfirmationFailed` | 6 | ⚠️ Ödeme alındı ama SunHotels başarısız |

---

## ⚠️ Önemli Notlar

1. **ConfirmationFailed** durumu özeldir: Ödeme Stripe'dan alınmış ama SunHotels rezervasyonu yapılamamıştır. Bu durumdaki rezervasyonlar için:
   - Ya `retry-sunhotels` ile tekrar deneyin
   - Ya da `refund` ile müşteriye iade yapın

2. **PreBookCode süresi dolabilir:** SunHotels PreBook kodları genellikle 15-30 dakika geçerlidir. Retry başarısız olursa müşterinin yeniden rezervasyon yapması gerekebilir.

3. **İptal Ücretleri:** SunHotels iptallerinde otel politikasına göre iptal ücreti kesilir. Bu miktar Stripe iadesinden otomatik düşülür (`processRefund: true` ise).

4. **Kısmi İade:** Stripe birden fazla kısmi iade yapmanıza izin verir. `refund-status` endpoint'i ile toplam iade miktarını takip edin.

5. **🆕 Non-Refundable Rezervasyonlar:** 
   - `isRefundable: false` olan rezervasyonlar iade edilemez
   - Refund endpoint'i bu durumda uyarı verir ve `forceRefund: true` gerektirir
   - `forceRefund: true` ile iade yaparsanız **şirket zarar eder** (SunHotels %100 kesinti yapar)

6. **🆕 Ücretsiz İptal Süresi:**
   - `freeCancellationDeadline` tarihinden önce iptal ücretsizdir
   - Bu tarihten sonra `cancellationPercentage` kadar kesinti uygulanır
   - Admin panelde bu bilgileri göstererek doğru iade kararı verin

---

## 🔒 Güvenlik

- Tüm endpoint'ler JWT authentication gerektirir
- Sadece `Admin` ve `SuperAdmin` rolleri erişebilir
- Tüm işlemler `booking.Notes` alanına loglanır
- Stripe işlemleri `metadata` ile izlenebilir

---

## 🆕 İptal Politikası Alanları

Rezervasyon oluşturulurken SunHotels PreBook yanıtından iptal politikası bilgileri otomatik olarak kaydedilir.

### Alanlar

| Alan | Tip | Açıklama |
|------|-----|----------|
| `isRefundable` | bool | `false` = Non-refundable oda, iade yapılamaz |
| `freeCancellationDeadline` | DateTime? | Ücretsiz iptal son tarihi. Bu tarihten önce %0 kesinti |
| `cancellationPercentage` | decimal | İptal ücreti yüzdesi (0-100). Check-in tarihine yaklaştıkça artar |
| `maxRefundableAmount` | decimal? | Politikaya göre maksimum iade edilebilir tutar |
| `cancellationPolicyText` | string? | İnsan okunabilir politika açıklaması |

### Örnek Senaryolar

#### Senaryo A: Tamamen İade Edilebilir
```json
{
  "isRefundable": true,
  "freeCancellationDeadline": "2026-02-01T00:00:00",
  "cancellationPercentage": 0,
  "maxRefundableAmount": 1450.00,
  "cancellationPolicyText": "Free cancellation until 01 Feb 2026"
}
```
→ 1 Şubat'a kadar tam iade yapılabilir

#### Senaryo B: Non-Refundable
```json
{
  "isRefundable": false,
  "freeCancellationDeadline": null,
  "cancellationPercentage": 100,
  "maxRefundableAmount": 0,
  "cancellationPolicyText": "Non-refundable: 100% cancellation fee applies from 01 Jan 2026"
}
```
→ ⚠️ İade yapılamaz. `forceRefund: true` ile zorlanırsa şirket zarar eder.

#### Senaryo C: Kademeli İptal Ücreti
```json
{
  "isRefundable": true,
  "freeCancellationDeadline": "2026-01-25T00:00:00",
  "cancellationPercentage": 50,
  "maxRefundableAmount": 725.00,
  "cancellationPolicyText": "Free cancellation until 25 Jan. 50% fee from 25 Jan to 01 Feb. 100% fee after 01 Feb."
}
```
→ 25 Ocak'tan sonra %50 kesinti uygulanır

### Admin Panel UI Önerisi

```
┌─────────────────────────────────────────────────────────┐
│ 💰 İade İşlemi                                         │
├─────────────────────────────────────────────────────────┤
│ Rezervasyon: #SH28161157                               │
│ Toplam Tutar: 1,450.00 EUR                             │
│                                                         │
│ ⚠️ İPTAL POLİTİKASI                                    │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ 🔴 NON-REFUNDABLE                                   │ │
│ │ Bu rezervasyon iade edilemez.                       │ │
│ │ İade yaparsanız SunHotels %100 kesinti uygular.     │ │
│ │                                                     │ │
│ │ Önerilen İade: 0.00 EUR                             │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ [  ] Bu riski kabul ediyorum (forceRefund)             │
│                                                         │
│ [ İptal ]                    [ ⚠️ Yine de İade Yap ]   │
└─────────────────────────────────────────────────────────┘
```
