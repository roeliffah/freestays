# SunHotels PreBook → Payment → Book Akışı

Bu dokümantasyon, FreeStays frontend ve backend arasındaki otel rezervasyon akışını detaylı şekilde açıklar.

## 🏨 Genel Akış

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           KULLANICI ARAYÜZÜ                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  1. Otel Arama → 2. Otel Detay → 3. Oda Seç → 4. Booking Sayfası            │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  BOOKING SAYFASI                                                       │  │
│  │  ├── Misafir Bilgileri (isim, email, telefon)                         │  │
│  │  ├── Yetişkin/Çocuk Bilgileri                                         │  │
│  │  ├── Pass/Kupon Seçimi (opsiyonel)                                    │  │
│  │  └── "Ödemeye Geç" Butonu                                             │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                    │                                         │
│                                    ▼                                         │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  FORM SUBMIT (BookingForm.tsx handleSubmit)                           │  │
│  │                                                                        │  │
│  │  Step 1: POST /api/v1/bookings/hotels/prebook                         │  │
│  │          ├── Misafir bilgileri ile birlikte                           │  │
│  │          └── Fiyat 30 dakika kilitleniyor                             │  │
│  │                                    │                                   │  │
│  │                                    ▼                                   │  │
│  │  Step 2: POST /api/v1/bookings/hotels/checkout-session                │  │
│  │          ├── preBookCode kullanılarak                                 │  │
│  │          └── Stripe Checkout Session oluşturuluyor                    │  │
│  │                                    │                                   │  │
│  │                                    ▼                                   │  │
│  │  Step 3: Stripe Checkout'a yönlendirme                                │  │
│  │          └── stripe.redirectToCheckout({ sessionId })                 │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           STRIPE ÖDEMECİ                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  Kullanıcı Stripe Checkout sayfasında ödeme yapar                           │
│  ├── Başarılı → successUrl'e yönlendirilir                                  │
│  └── İptal → cancelUrl'e yönlendirilir                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           BACKEND WEBHOOK                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│  POST /api/v1/webhooks/stripe                                                │
│  ├── Stripe ödeme başarılı event'i (checkout.session.completed)             │
│  ├── metadata'dan preBookCode alınır                                         │
│  ├── POST /api/v1/bookings/hotels/confirm çağrılır                          │
│  │   └── SunHotels BookV3 API ile gerçek rezervasyon yapılır                │
│  └── Kullanıcıya onay email'i gönderilir                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📋 API Endpoint Gereksinimleri

### 1. PreBook Endpoint
**POST** `/api/v1/bookings/hotels/prebook`

**Request Body:**
```json
{
  "hotelId": 12345,
  "roomId": 67890,
  "roomTypeId": 67890,
  "mealId": 1,
  "checkInDate": "2025-07-01",
  "checkOutDate": "2025-07-05",
  "currency": "EUR",
  "guests": [
    {
      "firstName": "John",
      "lastName": "Doe",
      "type": "adult"
    }
  ],
  "children": [
    {
      "firstName": "Jane",
      "lastName": "Doe",
      "age": 8
    }
  ],
  "contactInfo": {
    "email": "john.doe@example.com",
    "phone": "+1234567890"
  },
  "specialRequests": "Late check-in requested"
}
```

**Response (Success):**
```json
{
  "success": true,
  "preBookCode": "PB-ABC123XYZ",
  "totalPrice": 450.00,
  "currency": "EUR",
  "priceChanged": false,
  "originalPrice": 450.00,
  "taxAmount": 45.00,
  "expiresAt": "2025-06-15T15:30:00Z",
  "hotelConfirmationNumber": null
}
```

**Response (Price Changed):**
```json
{
  "success": true,
  "preBookCode": "PB-ABC123XYZ",
  "totalPrice": 480.00,
  "currency": "EUR",
  "priceChanged": true,
  "originalPrice": 450.00,
  "taxAmount": 48.00,
  "expiresAt": "2025-06-15T15:30:00Z"
}
```

**Backend İşlemleri:**
1. SunHotels PreBook API'yi çağır
2. Fiyatı 30 dakika kilitle
3. preBookCode üret ve veritabanına kaydet
4. Misafir bilgilerini rezervasyon kaydına ekle
5. Fiyat değişikliği varsa `priceChanged: true` döndür

---

### 2. Checkout Session Endpoint
**POST** `/api/v1/bookings/hotels/checkout-session`

**Request Body:**
```json
{
  "preBookCode": "PB-ABC123XYZ",
  "amount": 450.00,
  "currency": "EUR",
  "hotelName": "Grand Hotel",
  "roomType": "Deluxe Room",
  "checkInDate": "2025-07-01",
  "checkOutDate": "2025-07-05",
  "guestName": "John Doe",
  "guestEmail": "john.doe@example.com",
  "successUrl": "https://freestays.com/en/booking/success?session_id={CHECKOUT_SESSION_ID}",
  "cancelUrl": "https://freestays.com/en/booking/cancel",
  "passPurchaseType": "one_time",
  "passCodeValid": false
}
```

**Response:**
```json
{
  "success": true,
  "sessionId": "cs_live_a1b2c3d4e5f6...",
  "url": "https://checkout.stripe.com/pay/cs_live_..."
}
```

**Backend İşlemleri:**
1. preBookCode'un geçerli ve expire olmadığını kontrol et
2. Stripe Checkout Session oluştur
3. metadata'ya preBookCode, hotelId, roomId vs. ekle
4. Pass/kupon varsa indirim uygula
5. Session ID döndür

**Stripe Checkout Session Oluşturma (Backend Örnek):**
```csharp
var options = new SessionCreateOptions
{
    PaymentMethodTypes = new List<string> { "card" },
    LineItems = new List<SessionLineItemOptions>
    {
        new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = request.Currency.ToLower(),
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = $"{request.HotelName} - {request.RoomType}",
                    Description = $"{request.CheckInDate} → {request.CheckOutDate}"
                },
                UnitAmount = (long)(request.Amount * 100) // Cents
            },
            Quantity = 1
        }
    },
    Mode = "payment",
    SuccessUrl = request.SuccessUrl,
    CancelUrl = request.CancelUrl,
    CustomerEmail = request.GuestEmail,
    Metadata = new Dictionary<string, string>
    {
        { "preBookCode", request.PreBookCode },
        { "hotelId", hotelId.ToString() },
        { "roomId", roomId.ToString() },
        { "checkInDate", request.CheckInDate },
        { "checkOutDate", request.CheckOutDate },
        { "guestName", request.GuestName },
        { "passPurchaseType", request.PassPurchaseType ?? "" }
    }
};

var service = new SessionService();
var session = await service.CreateAsync(options);
```

---

### 3. Stripe Webhook Endpoint
**POST** `/api/v1/webhooks/stripe`

**Stripe Event:** `checkout.session.completed`

**Backend İşlemleri:**
1. Webhook signature'ı doğrula
2. Event tipini kontrol et (`checkout.session.completed`)
3. Session metadata'dan bilgileri al
4. `POST /api/v1/bookings/hotels/confirm` çağır
5. Booking kaydını veritabanında güncelle
6. Kullanıcıya onay email'i gönder

**Webhook Handler Örneği:**
```csharp
[HttpPost("stripe")]
public async Task<IActionResult> StripeWebhook()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
    var stripeSignature = Request.Headers["Stripe-Signature"];
    
    try
    {
        var stripeEvent = EventUtility.ConstructEvent(
            json, stripeSignature, _webhookSecret);
        
        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            var preBookCode = session.Metadata["preBookCode"];
            
            // Confirm booking with SunHotels
            await _bookingService.ConfirmBooking(preBookCode);
            
            // Send confirmation email
            await _emailService.SendBookingConfirmation(session.CustomerEmail, ...);
        }
        
        return Ok();
    }
    catch (StripeException e)
    {
        return BadRequest(e.Message);
    }
}
```

---

### 4. Confirm Booking Endpoint
**POST** `/api/v1/bookings/hotels/confirm`

**Request Body:**
```json
{
  "preBookCode": "PB-ABC123XYZ"
}
```

**Response:**
```json
{
  "success": true,
  "bookingId": "BK-2025-001234",
  "sunhotelsBookingCode": "SH-789456",
  "hotelConfirmationNumber": "CONF-ABC123",
  "status": "confirmed",
  "voucher": {
    "voucherNumber": "V-001234",
    "downloadUrl": "/api/v1/bookings/BK-2025-001234/voucher"
  }
}
```

**Backend İşlemleri:**
1. preBookCode ile PreBook kaydını bul
2. SunHotels BookV3 API'yi çağır (gerçek rezervasyon)
3. Booking kaydını oluştur/güncelle
4. Voucher PDF oluştur
5. Onay bilgilerini döndür

---

## 🔐 Güvenlik Gereksinimleri

### PreBook Güvenliği
- PreBook kodu 30 dakika geçerli olmalı
- Aynı preBookCode ile sadece 1 kez ödeme yapılabilmeli
- Frontend'den gelen fiyat yerine backend'deki preBook fiyatı kullanılmalı

### Stripe Webhook Güvenliği
- Webhook signature mutlaka doğrulanmalı
- Event'ler idempotent işlenmeli (aynı event 2 kez gelirse sorun olmamalı)
- Webhook secret environment variable'da tutulmalı

### Genel Güvenlik
- Tüm API endpoint'leri HTTPS üzerinden
- Kullanıcı email'i Stripe'a gönderilmeden önce sanitize edilmeli
- Rate limiting uygulanmalı

---

## 📧 Email Bildirimleri

### Başarılı Rezervasyon Email'i İçeriği:
- Otel adı ve adresi
- Oda tipi ve yemek planı
- Check-in / Check-out tarihleri
- Misafir isimleri
- Toplam ödenen tutar
- Rezervasyon numarası (SunHotels + internal)
- Voucher PDF eki veya download linki
- İptal politikası
- İletişim bilgileri

---

## ⚙️ Environment Variables

### Frontend (.env.local)
```env
NEXT_PUBLIC_API_URL=http://localhost:5240/api/v1
NEXT_PUBLIC_STRIPE_PUBLIC_KEY=pk_test_...
```

### Backend (appsettings.json / environment)
```env
Stripe__SecretKey=sk_test_...
Stripe__WebhookSecret=whsec_...
SunHotels__Username=xxx
SunHotels__Password=xxx
SunHotels__ApiUrl=https://xml.sunhotels.net/15/PostGet/NonStaticXMLAPI.asmx
```

---

## 🧪 Test Senaryoları

### 1. Normal Akış
1. Kullanıcı form doldurur → PreBook başarılı
2. Stripe Checkout'a yönlendirilir
3. Test kart ile ödeme yapar (4242 4242 4242 4242)
4. Success sayfasına yönlendirilir
5. Onay email'i alır

### 2. Fiyat Değişikliği
1. PreBook sırasında fiyat değişirse
2. Kullanıcıya confirm dialog gösterilir
3. Kabul ederse yeni fiyatla devam eder
4. Reddederse işlem iptal olur

### 3. PreBook Expire
1. Kullanıcı 30 dakikadan fazla bekler
2. Ödeme sayfasında hata alır
3. Yeni PreBook gerekir

### 4. Ödeme İptali
1. Kullanıcı Stripe sayfasında cancel'a tıklar
2. cancelUrl'e yönlendirilir
3. Booking oluşturulmaz, PreBook expire olur

---

## 📝 Frontend Dosyaları

| Dosya | Açıklama |
|-------|----------|
| `app/[locale]/booking/page.tsx` | Booking sayfası - misafir bilgileri ve pass seçimi |
| `components/booking/BookingForm.tsx` | Form bileşeni - PreBook ve Checkout logic |
| `app/[locale]/booking/success/page.tsx` | Başarılı ödeme sonrası sayfa |
| `app/[locale]/booking/cancel/page.tsx` | İptal edilen ödeme sayfası |

---

## 🚀 Deployment Checklist

- [ ] Stripe production keys
- [ ] Webhook endpoint'i Stripe Dashboard'a ekle
- [ ] SunHotels production credentials
- [ ] HTTPS sertifikası
- [ ] Email servis konfigürasyonu
- [ ] Rate limiting aktif
- [ ] Error logging/monitoring
- [ ] Database backup stratejisi
