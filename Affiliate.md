# Backend API Requirements - Affiliate Programs

## 📋 Genel Bakış

Admin panelinde **Affiliate Programs** sekmesi eklendi. Bu özellik için backend API'sine yeni alanlar eklenmelidir.

---

## 🔧 Gerekli Değişiklikler

### 1. **Database Schema Güncellemesi**

`SiteSettings` tablosuna (veya ilgili ayarlar tablosuna) aşağıdaki **9 yeni alan** eklenmelidir:

```sql
-- Excursions / Tours & Activities
excursionsActive BOOLEAN DEFAULT false,
excursionsAffiliateCode NVARCHAR(500) NULL,
excursionsWidgetCode NVARCHAR(MAX) NULL,

-- Car Rental
carRentalActive BOOLEAN DEFAULT false,
carRentalAffiliateCode NVARCHAR(500) NULL,
carRentalWidgetCode NVARCHAR(MAX) NULL,

-- Flight Booking
flightBookingActive BOOLEAN DEFAULT false,
flightBookingAffiliateCode NVARCHAR(500) NULL,
flightBookingWidgetCode NVARCHAR(MAX) NULL
```

**Alan Açıklamaları:**
- `*Active`: Boolean - Servisin aktif/pasif durumu
- `*AffiliateCode`: String - Affiliate partner link URL'i (max 500 karakter)
- `*WidgetCode`: Text - HTML/JavaScript widget embed kodu (sınırsız karakter - NVARCHAR(MAX))

---

### 2. **API Endpoint Güncelleme**

#### **Mevcut Endpoint:** 
```
PUT /api/v1/admin/settings/site
```

#### **Request Body'ye Eklenmesi Gerekenler:**

```json
{
  "excursionsActive": true,
  "excursionsAffiliateCode": "https://getyourguide.com/?partner_id=U00202819",
  "excursionsWidgetCode": "<div data-vi-partner-id=\"U00202819\" data-vi-widget-ref=\"W-46e0b4fc-2d24-4a08-8178-2464b72e88a1\"></div>\n<script async src=\"https://www.viator.com/orion/partner/widget.js\"></script>",
  
  "carRentalActive": false,
  "carRentalAffiliateCode": "",
  "carRentalWidgetCode": "",
  
  "flightBookingActive": true,
  "flightBookingAffiliateCode": "https://skyscanner.com/?associateid=ABC123",
  "flightBookingWidgetCode": "<div id=\"flight-widget\"></div>\n<script src=\"https://widget.skyscanner.com/widget.js\"></script>"
}
```

---

### 3. **DTO (Data Transfer Object) Güncelleme**

C# .NET Core için örnek model:

```csharp
public class UpdateSiteSettingsDto
{
    // ... Mevcut alanlar ...
    
    // Affiliate Programs
    public bool? ExcursionsActive { get; set; }
    public string? ExcursionsAffiliateCode { get; set; }
    public string? ExcursionsWidgetCode { get; set; }
    
    public bool? CarRentalActive { get; set; }
    public string? CarRentalAffiliateCode { get; set; }
    public string? CarRentalWidgetCode { get; set; }
    
    public bool? FlightBookingActive { get; set; }
    public string? FlightBookingAffiliateCode { get; set; }
    public string? FlightBookingWidgetCode { get; set; }
}
```

---

### 4. **GET Endpoint Güncelleme**

#### **Mevcut Endpoint:**
```
GET /api/v1/admin/settings
```

#### **Response Body'ye Eklenmesi Gerekenler:**

```json
{
  "data": {
    "siteName": "FreeStays",
    // ... diğer mevcut alanlar ...
    
    "excursionsActive": true,
    "excursionsAffiliateCode": "https://getyourguide.com/?partner_id=U00202819",
    "excursionsWidgetCode": "<div data-vi-partner-id=\"U00202819\" data-vi-widget-ref=\"W-46e0b4fc-2d24-4a08-8178-2464b72e88a1\"></div>\n<script async src=\"https://www.viator.com/orion/partner/widget.js\"></script>",
    
    "carRentalActive": false,
    "carRentalAffiliateCode": null,
    "carRentalWidgetCode": null,
    
    "flightBookingActive": true,
    "flightBookingAffiliateCode": "https://skyscanner.com/?associateid=ABC123",
    "flightBookingWidgetCode": "<div id=\"flight-widget\"></div>\n<script src=\"https://widget.skyscanner.com/widget.js\"></script>"
  }
}
```

---

## 🔒 Güvenlik Önerileri

### 1. **XSS Koruması**
Widget kodları HTML/JavaScript içerdiği için **XSS saldırılarına** karşı dikkatli olunmalıdır:

```csharp
// Backend'de widget kodunu sanitize etmeyin (kullanıcı kasıtlı olarak script ekliyor)
// Ancak authorization kontrolü yapın
[Authorize(Roles = "Admin")]
public async Task<IActionResult> UpdateSiteSettings([FromBody] UpdateSiteSettingsDto dto)
{
    // Sadece admin kullanıcılar güncelleyebilir
}
```

### 2. **Frontend'de Güvenli Render**
Widget kodları frontend'de `dangerouslySetInnerHTML` ile render edilecek, bu yüzden **sadece admin'den gelen** kodlar kullanılmalıdır.

### 3. **Validation Kuralları**
```csharp
// Widget code boş olabilir (nullable)
// Affiliate code URL formatında olmalı (opsiyonel validasyon)
if (!string.IsNullOrEmpty(dto.ExcursionsAffiliateCode))
{
    if (!Uri.TryCreate(dto.ExcursionsAffiliateCode, UriKind.Absolute, out _))
    {
        return BadRequest("Invalid affiliate URL format");
    }
}
```

---

## 🧪 Test Senaryoları

### Test Case 1: Widget Code Kaydetme
```bash
PUT /api/v1/admin/settings/site
{
  "excursionsActive": true,
  "excursionsWidgetCode": "<div data-vi-partner-id=\"U00202819\"></div>\n<script async src=\"https://www.viator.com/orion/partner/widget.js\"></script>"
}

# Beklenen: 200 OK, widget kodu kaydedilmeli
```

### Test Case 2: Widget Code Okuma
```bash
GET /api/v1/admin/settings

# Beklenen: Response içinde excursionsWidgetCode alanı olmalı
```

### Test Case 3: Widget Code Silme
```bash
PUT /api/v1/admin/settings/site
{
  "excursionsWidgetCode": ""
}

# Beklenen: 200 OK, widget kodu silinmeli (null veya empty string)
```

---

## 📊 Database Migration Örneği (Entity Framework)

```csharp
public partial class AddAffiliatePrograms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ExcursionsActive",
            table: "SiteSettings",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ExcursionsAffiliateCode",
            table: "SiteSettings",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExcursionsWidgetCode",
            table: "SiteSettings",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "CarRentalActive",
            table: "SiteSettings",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "CarRentalAffiliateCode",
            table: "SiteSettings",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CarRentalWidgetCode",
            table: "SiteSettings",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "FlightBookingActive",
            table: "SiteSettings",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "FlightBookingAffiliateCode",
            table: "SiteSettings",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FlightBookingWidgetCode",
            table: "SiteSettings",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ExcursionsActive", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "ExcursionsAffiliateCode", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "ExcursionsWidgetCode", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "CarRentalActive", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "CarRentalAffiliateCode", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "CarRentalWidgetCode", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FlightBookingActive", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FlightBookingAffiliateCode", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FlightBookingWidgetCode", table: "SiteSettings");
    }
}
```

---

## 🎯 Frontend Kullanım Örneği

Widget kodları frontend'de şu şekilde render edilecek:

```tsx
// components/home/TravelWidget.tsx
export function TravelWidget({ widgetCode }: { widgetCode?: string }) {
  if (!widgetCode) return null;
  
  return (
    <div 
      dangerouslySetInnerHTML={{ __html: widgetCode }}
      className="travel-widget-container"
    />
  );
}
```

---

## ✅ Checklist

Backend development ekibi için kontrol listesi:

- [ ] Database'e 9 yeni alan eklendi
- [ ] PUT `/api/v1/admin/settings/site` endpoint'i güncellendi
- [ ] GET `/api/v1/admin/settings` response'una yeni alanlar eklendi
- [ ] DTO modelleri güncellendi
- [ ] Migration dosyası oluşturuldu ve çalıştırıldı
- [ ] Admin authorization kontrolü yapıldı
- [ ] URL validation (opsiyonel) eklendi
- [ ] Test senaryoları çalıştırıldı
- [ ] Swagger/OpenAPI dokümantasyonu güncellendi

---

## 📞 İletişim

Sorularınız için: Frontend Development Team

**Tarih:** 27 Aralık 2025
