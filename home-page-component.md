# Homepage Components Management API - Backend Implementation Guide

## Overview
Bu doküman, homepage'deki component'lerin admin panel üzerinden yönetilmesi için backend API'nin nasıl implement edileceğini açıklar.

## Database Schema

### Table: `HomePageSections`
Homepage'de gösterilecek section'ları saklar.

```sql
CREATE TABLE HomePageSections (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SectionType NVARCHAR(50) NOT NULL, -- 'hero', 'room-types', 'features', 'popular-hotels', 'popular-destinations', 'romantic-tours', 'campaign-banner', 'travel-cta', 'final-cta', 'custom-html'
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL,
    Configuration NVARCHAR(MAX), -- JSON format için component-specific settings
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedBy UNIQUEIDENTIFIER,
    
    CONSTRAINT CK_SectionType CHECK (SectionType IN (
        'hero', 
        'room-types', 
        'features', 
        'popular-hotels', 
        'popular-destinations', 
        'romantic-tours', 
        'campaign-banner', 
        'travel-cta', 
        'final-cta', 
        'custom-html'
    ))
);

CREATE INDEX IX_HomePageSections_DisplayOrder ON HomePageSections(DisplayOrder);
CREATE INDEX IX_HomePageSections_IsActive ON HomePageSections(IsActive);
```

### Table: `HomePageSectionTranslations`
Section başlık ve alt başlıklarının çoklu dil desteği.

```sql
CREATE TABLE HomePageSectionTranslations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SectionId UNIQUEIDENTIFIER NOT NULL,
    Locale NVARCHAR(10) NOT NULL, -- 'tr', 'en', 'de', 'fr', 'es', 'it', 'nl', 'ru', 'el'
    Title NVARCHAR(200),
    Subtitle NVARCHAR(500),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT FK_SectionTranslations_Section FOREIGN KEY (SectionId) 
        REFERENCES HomePageSections(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Section_Locale UNIQUE (SectionId, Locale)
);

CREATE INDEX IX_SectionTranslations_SectionId ON HomePageSectionTranslations(SectionId);
CREATE INDEX IX_SectionTranslations_Locale ON HomePageSectionTranslations(Locale);
```

### Table: `HomePageSectionHotels`
Popular Hotels ve Romantic Tours section'larında gösterilecek oteller.

```sql
CREATE TABLE HomePageSectionHotels (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SectionId UNIQUEIDENTIFIER NOT NULL,
    HotelId NVARCHAR(50) NOT NULL, -- SunHotels API'den gelen hotel ID
    DisplayOrder INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT FK_SectionHotels_Section FOREIGN KEY (SectionId) 
        REFERENCES HomePageSections(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Section_Hotel UNIQUE (SectionId, HotelId)
);

CREATE INDEX IX_SectionHotels_SectionId ON HomePageSectionHotels(SectionId);
CREATE INDEX IX_SectionHotels_DisplayOrder ON HomePageSectionHotels(DisplayOrder);
```

### Table: `HomePageSectionDestinations`
Popular Destinations section'ında gösterilecek destinasyonlar.

```sql
CREATE TABLE HomePageSectionDestinations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SectionId UNIQUEIDENTIFIER NOT NULL,
    DestinationId NVARCHAR(50) NOT NULL, -- SunHotels API'den gelen destination ID
    DisplayOrder INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT FK_SectionDestinations_Section FOREIGN KEY (SectionId) 
        REFERENCES HomePageSections(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Section_Destination UNIQUE (SectionId, DestinationId)
);

CREATE INDEX IX_SectionDestinations_SectionId ON HomePageSectionDestinations(SectionId);
CREATE INDEX IX_SectionDestinations_DisplayOrder ON HomePageSectionDestinations(DisplayOrder);
```

### Configuration JSON Schemas

#### Hero Section
```json
{
  "backgroundImage": "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=1920&q=80",
  "gradient": "from-black/60 via-black/40 to-black/60",
  "height": "600px",
  "showSearchForm": true
}
```

#### Room Types Section
```json
{
  "types": [
    {
      "id": "hotel",
      "icon": "Hotel",
      "color": "blue",
      "translationKey": "roomTypes.hotel"
    },
    {
      "id": "resort",
      "icon": "Umbrella",
      "color": "green",
      "translationKey": "roomTypes.resort"
    },
    {
      "id": "apart",
      "icon": "Plane",
      "color": "purple",
      "translationKey": "roomTypes.apart"
    },
    {
      "id": "villa",
      "icon": "Sparkles",
      "color": "orange",
      "translationKey": "roomTypes.villa"
    }
  ]
}
```

#### Features Section
```json
{
  "features": [
    {
      "icon": "Sparkles",
      "color": "primary",
      "translationKey": "features.bestPrice"
    },
    {
      "icon": "Shield",
      "color": "secondary",
      "translationKey": "features.secure"
    },
    {
      "icon": "Clock",
      "color": "accent",
      "translationKey": "features.support"
    },
    {
      "icon": "Star",
      "color": "primary",
      "translationKey": "features.hotels"
    }
  ]
}
```

#### Popular Hotels Section
```json
{
  "fetchMode": "auto",
  "layout": "grid-3",
  "autoQuery": {
    "stars": 5,
    "count": 6,
    "orderBy": "rating"
  },
  "manualHotelIds": []
}
```

#### Popular Destinations Section
```json
{
  "fetchMode": "auto",
  "layout": "featured-grid",
  "autoQuery": {
    "count": 5,
    "orderBy": "hotelCount"
  },
  "manualDestinationIds": []
}
```

#### Romantic Tours Section
```json
{
  "fetchMode": "auto",
  "layout": "carousel",
  "autoQuery": {
    "theme": "romantic",
    "stars": 4,
    "count": 8
  },
  "manualHotelIds": []
}
```

#### Campaign Banner Section
```json
{
  "gradient": "from-orange-500 to-red-500",
  "badge": "Special Offer",
  "buttonText": "View Deals",
  "buttonLink": "/search?offers=true"
}
```

#### Travel CTA Cards Section
```json
{
  "cards": [
    {
      "type": "excursions",
      "icon": "MapPin",
      "color": "blue",
      "translationKey": "travel.excursions",
      "affiliateKey": "excursions"
    },
    {
      "type": "car-rental",
      "icon": "Car",
      "color": "green",
      "translationKey": "travel.carRental",
      "affiliateKey": "carRental"
    },
    {
      "type": "flight",
      "icon": "Plane",
      "color": "purple",
      "translationKey": "travel.flights",
      "affiliateKey": "flightBooking"
    }
  ]
}
```

#### Final CTA Section
```json
{
  "gradient": "from-primary to-secondary",
  "buttons": [
    {
      "text": "Search Hotels",
      "link": "/search",
      "variant": "primary"
    },
    {
      "text": "Learn More",
      "link": "/about",
      "variant": "secondary"
    }
  ]
}
```

#### Custom HTML Section
```json
{
  "html": "<div class='py-12 bg-gradient-to-r from-blue-500 to-purple-500 text-white text-center'><h2 class='text-3xl font-bold'>Custom Content</h2></div>",
  "sanitize": true,
  "allowedTags": ["div", "p", "h1", "h2", "h3", "span", "a", "img"],
  "cssClasses": ""
}
```

---

## API Endpoints

### 1. Get All Homepage Sections (Public)
**GET** `/api/v1/public/homepage/sections`

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "sectionType": "hero",
      "title": "Find Your Perfect Stay",
      "subtitle": "Discover amazing hotels...",
      "isActive": true,
      "displayOrder": 1,
      "configuration": {
        "backgroundImage": "...",
        "showSearchForm": true
      }
    },
    {
      "id": "guid",
      "sectionType": "popular-hotels",
      "title": null,
      "subtitle": null,
      "isActive": true,
      "displayOrder": 5,
      "configuration": {
        "fetchMode": "auto",
        "layout": "grid-3",
        "autoQuery": {
          "stars": 5,
          "count": 6
        }
      }
    }
  ]
}
```

### 2. Get All Homepage Sections (Admin)
**GET** `/api/v1/admin/homepage/sections`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "sectionType": "hero",
      "title": "Find Your Perfect Stay",
      "subtitle": "Discover amazing hotels...",
      "isActive": true,
      "displayOrder": 1,
      "configuration": {...},
      "createdAt": "2025-12-27T10:00:00Z",
      "updatedAt": "2025-12-27T10:00:00Z",
      "createdBy": "guid",
      "updatedBy": "guid"
    }
  ]
}
```

### 3. Create Homepage Section
**POST** `/api/v1/admin/homepage/sections`

**Headers:**
```
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "sectionType": "popular-hotels",
  "title": null,
  "subtitle": null,
  "isActive": true,
  "displayOrder": 5,
  "configuration": {
    "fetchMode": "auto",
    "layout": "grid-3",
    "autoQuery": {
      "stars": 5,
      "count": 6
    }
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Section created successfully",
  "data": {
    "id": "new-guid",
    "sectionType": "popular-hotels",
    "displayOrder": 5,
    "isActive": true
  }
}
```

### 4. Update Homepage Section
**PUT** `/api/v1/admin/homepage/sections/{id}`

**Headers:**
```
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "title": "Updated Title",
  "subtitle": "Updated Subtitle",
  "isActive": true,
  "displayOrder": 3,
  "configuration": {
    "fetchMode": "manual",
    "manualHotelIds": ["hotel1", "hotel2", "hotel3"]
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Section updated successfully"
}
```

### 5. Delete Homepage Section
**DELETE** `/api/v1/admin/homepage/sections/{id}`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Section deleted successfully"
}
```

### 6. Reorder Sections
**PATCH** `/api/v1/admin/homepage/sections/reorder`

**Headers:**
```
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "sectionOrders": [
    { "id": "guid1", "displayOrder": 1 },
    { "id": "guid2", "displayOrder": 2 },
    { "id": "guid3", "displayOrder": 3 }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Sections reordered successfully"
}
```

### 7. Toggle Section Active Status
**PATCH** `/api/v1/admin/homepage/sections/{id}/toggle`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "message": "Section status updated",
  "data": {
    "id": "guid",
    "isActive": false
  }
}
```

### 8. Get Available Section Types
**GET** `/api/v1/admin/homepage/section-types`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "type": "hero",
      "name": "Hero Section",
      "description": "Main banner with search form",
      "icon": "Banner",
      "configSchema": {...}
    },
    {
      "type": "popular-hotels",
      "name": "Popular Hotels",
      "description": "Display featured hotels",
      "icon": "Hotel",
      "configSchema": {...}
    }
  ]
}
```

---

## Multi-Language Translation Endpoints

### 9. Get Section Translations
**GET** `/api/v1/admin/homepage/sections/{sectionId}/translations`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "tr": {
      "locale": "tr",
      "title": "Popüler Oteller",
      "subtitle": "En çok tercih edilen 5 yıldızlı oteller"
    },
    "en": {
      "locale": "en",
      "title": "Popular Hotels",
      "subtitle": "Most preferred 5-star hotels"
    },
    "de": {
      "locale": "de",
      "title": "Beliebte Hotels",
      "subtitle": "Die beliebtesten 5-Sterne-Hotels"
    }
  }
}
```

### 10. Update Section Translations
**POST** `/api/v1/admin/homepage/sections/{sectionId}/translations`

**Headers:**
```
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "translations": {
    "tr": {
      "title": "Popüler Oteller",
      "subtitle": "En çok tercih edilen 5 yıldızlı oteller"
    },
    "en": {
      "title": "Popular Hotels",
      "subtitle": "Most preferred 5-star hotels"
    },
    "de": {
      "title": "Beliebte Hotels",
      "subtitle": "Die beliebtesten 5-Sterne-Hotels"
    }
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Translations updated successfully"
}
```

---

## Hotel/Destination Selection Endpoints

### 11. Get Available Hotels (For Selection)
**GET** `/api/v1/admin/homepage/available-hotels`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Query Parameters:**
- `search` (optional): Hotel name search
- `stars` (optional): Filter by stars (1-5)
- `country` (optional): Filter by country code
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50)

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "hotelId": "12345",
        "hotelName": "Rixos Premium Belek",
        "destinationName": "Belek",
        "country": "Turkey",
        "stars": 5,
        "rating": 9.2,
        "image": "https://...",
        "priceFrom": 150,
        "currency": "EUR"
      }
    ],
    "totalCount": 1250,
    "page": 1,
    "pageSize": 50,
    "totalPages": 25
  }
}
```

### 12. Get Available Destinations (For Selection)
**GET** `/api/v1/admin/homepage/available-destinations`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Query Parameters:**
- `search` (optional): Destination name search
- `country` (optional): Filter by country code
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 50)

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "destinationId": "123",
        "destinationName": "Antalya",
        "country": "Turkey",
        "countryCode": "TR",
        "hotelCount": 542,
        "averagePrice": 120,
        "image": "https://..."
      }
    ],
    "totalCount": 250,
    "page": 1,
    "pageSize": 50,
    "totalPages": 5
  }
}
```

### 13. Get Section Hotels
**GET** `/api/v1/admin/homepage/sections/{sectionId}/hotels`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "hotelId": "12345",
      "displayOrder": 1,
      "hotelDetails": {
        "hotelName": "Rixos Premium Belek",
        "destinationName": "Belek",
        "country": "Turkey",
        "stars": 5,
        "rating": 9.2,
        "image": "https://...",
        "priceFrom": 150,
        "currency": "EUR"
      }
    }
  ]
}
```

### 14. Update Section Hotels
**POST** `/api/v1/admin/homepage/sections/{sectionId}/hotels`

**Headers:**
```
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "hotels": [
    { "hotelId": "12345", "displayOrder": 1 },
    { "hotelId": "67890", "displayOrder": 2 },
    { "hotelId": "11111", "displayOrder": 3 }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Section hotels updated successfully"
}
```

### 15. Get Section Destinations
**GET** `/api/v1/admin/homepage/sections/{sectionId}/destinations`

**Headers:**
```
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "destinationId": "123",
      "displayOrder": 1,
      "destinationDetails": {
        "destinationName": "Antalya",
        "country": "Turkey",
        "hotelCount": 542,
        "averagePrice": 120,
        "image": "https://..."
      }
    }
  ]
}
```

### 16. Update Section Destinations
**POST** `/api/v1/admin/homepage/sections/{sectionId}/destinations`

**Headers:**
```
Authorization: Bearer {admin_token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "destinations": [
    { "destinationId": "123", "displayOrder": 1 },
    { "destinationId": "456", "displayOrder": 2 },
    { "destinationId": "789", "displayOrder": 3 }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Section destinations updated successfully"
}
```
    }
  ]
}
```

---

## C# Controller Implementation Example

### HomePageSectionsController.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FreeStays.API.Controllers.Admin
{
    [ApiController]
    [Route("api/v1/admin/homepage")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class HomePageSectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomePageSectionsController> _logger;

        public HomePageSectionsController(
            ApplicationDbContext context,
            ILogger<HomePageSectionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("sections")]
        public async Task<IActionResult> GetAllSections()
        {
            try
            {
                var sections = await _context.HomePageSections
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new
                    {
                        s.Id,
                        s.SectionType,
                        s.Title,
                        s.Subtitle,
                        s.IsActive,
                        s.DisplayOrder,
                        Configuration = JsonSerializer.Deserialize<JsonElement>(s.Configuration),
                        s.CreatedAt,
                        s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = sections });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching homepage sections");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("sections")]
        public async Task<IActionResult> CreateSection([FromBody] CreateSectionRequest request)
        {
            try
            {
                // Validate section type
                var validTypes = new[] { "hero", "room-types", "features", "popular-hotels", 
                    "popular-destinations", "romantic-tours", "campaign-banner", 
                    "travel-cta", "final-cta", "custom-html" };
                
                if (!validTypes.Contains(request.SectionType))
                {
                    return BadRequest(new { success = false, message = "Invalid section type" });
                }

                var section = new HomePageSection
                {
                    Id = Guid.NewGuid(),
                    SectionType = request.SectionType,
                    Title = request.Title,
                    Subtitle = request.Subtitle,
                    IsActive = request.IsActive,
                    DisplayOrder = request.DisplayOrder,
                    Configuration = JsonSerializer.Serialize(request.Configuration),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = GetCurrentUserId(),
                    UpdatedBy = GetCurrentUserId()
                };

                _context.HomePageSections.Add(section);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Section created successfully",
                    data = new { section.Id, section.SectionType, section.DisplayOrder }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating homepage section");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPut("sections/{id}")]
        public async Task<IActionResult> UpdateSection(Guid id, [FromBody] UpdateSectionRequest request)
        {
            try
            {
                var section = await _context.HomePageSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound(new { success = false, message = "Section not found" });
                }

                section.Title = request.Title;
                section.Subtitle = request.Subtitle;
                section.IsActive = request.IsActive;
                section.DisplayOrder = request.DisplayOrder;
                section.Configuration = JsonSerializer.Serialize(request.Configuration);
                section.UpdatedAt = DateTime.UtcNow;
                section.UpdatedBy = GetCurrentUserId();

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Section updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating homepage section");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpDelete("sections/{id}")]
        public async Task<IActionResult> DeleteSection(Guid id)
        {
            try
            {
                var section = await _context.HomePageSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound(new { success = false, message = "Section not found" });
                }

                _context.HomePageSections.Remove(section);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Section deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting homepage section");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPatch("sections/reorder")]
        public async Task<IActionResult> ReorderSections([FromBody] ReorderRequest request)
        {
            try
            {
                foreach (var item in request.SectionOrders)
                {
                    var section = await _context.HomePageSections.FindAsync(item.Id);
                    if (section != null)
                    {
                        section.DisplayOrder = item.DisplayOrder;
                        section.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Sections reordered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering sections");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPatch("sections/{id}/toggle")]
        public async Task<IActionResult> ToggleSection(Guid id)
        {
            try
            {
                var section = await _context.HomePageSections.FindAsync(id);
                if (section == null)
                {
                    return NotFound(new { success = false, message = "Section not found" });
                }

                section.IsActive = !section.IsActive;
                section.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Section status updated",
                    data = new { section.Id, section.IsActive }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling section");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }

    // Request DTOs
    public class CreateSectionRequest
    {
        public string SectionType { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public object Configuration { get; set; }
    }

    public class UpdateSectionRequest
    {
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public object Configuration { get; set; }
    }

    public class ReorderRequest
    {
        public List<SectionOrder> SectionOrders { get; set; }
    }

    public class SectionOrder
    {
        public Guid Id { get; set; }
        public int DisplayOrder { get; set; }
    }
}
```

### Public Controller (Frontend Data)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FreeStays.API.Controllers.Public
{
    [ApiController]
    [Route("api/v1/public/homepage")]
    public class PublicHomePageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PublicHomePageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("sections")]
        public async Task<IActionResult> GetActiveSections()
        {
            try
            {
                var sections = await _context.HomePageSections
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new
                    {
                        s.Id,
                        s.SectionType,
                        s.Title,
                        s.Subtitle,
                        s.DisplayOrder,
                        Configuration = JsonSerializer.Deserialize<JsonElement>(s.Configuration)
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = sections });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}
```

---

## Entity Model

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeStays.API.Models
{
    [Table("HomePageSections")]
    public class HomePageSection
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SectionType { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(500)]
        public string? Subtitle { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public int DisplayOrder { get; set; }

        [Required]
        public string Configuration { get; set; } // JSON string

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}
```

---

## Migration Script

```sql
-- Create HomePageSections table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HomePageSections')
BEGIN
    CREATE TABLE HomePageSections (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SectionType NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        DisplayOrder INT NOT NULL,
        Configuration NVARCHAR(MAX),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CreatedBy UNIQUEIDENTIFIER,
        UpdatedBy UNIQUEIDENTIFIER,
        
        CONSTRAINT CK_SectionType CHECK (SectionType IN (
            'hero', 
            'room-types', 
            'features', 
            'popular-hotels', 
            'popular-destinations', 
            'romantic-tours', 
            'campaign-banner', 
            'travel-cta', 
            'final-cta', 
            'custom-html'
        ))
    );

    CREATE INDEX IX_HomePageSections_DisplayOrder ON HomePageSections(DisplayOrder);
    CREATE INDEX IX_HomePageSections_IsActive ON HomePageSections(IsActive);
END
GO

-- Create HomePageSectionTranslations table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HomePageSectionTranslations')
BEGIN
    CREATE TABLE HomePageSectionTranslations (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SectionId UNIQUEIDENTIFIER NOT NULL,
        Locale NVARCHAR(10) NOT NULL,
        Title NVARCHAR(200),
        Subtitle NVARCHAR(500),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_SectionTranslations_Section FOREIGN KEY (SectionId) 
            REFERENCES HomePageSections(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_Section_Locale UNIQUE (SectionId, Locale)
    );

    CREATE INDEX IX_SectionTranslations_SectionId ON HomePageSectionTranslations(SectionId);
    CREATE INDEX IX_SectionTranslations_Locale ON HomePageSectionTranslations(Locale);
END
GO

-- Create HomePageSectionHotels table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HomePageSectionHotels')
BEGIN
    CREATE TABLE HomePageSectionHotels (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SectionId UNIQUEIDENTIFIER NOT NULL,
        HotelId NVARCHAR(50) NOT NULL,
        DisplayOrder INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_SectionHotels_Section FOREIGN KEY (SectionId) 
            REFERENCES HomePageSections(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_Section_Hotel UNIQUE (SectionId, HotelId)
    );

    CREATE INDEX IX_SectionHotels_SectionId ON HomePageSectionHotels(SectionId);
    CREATE INDEX IX_SectionHotels_DisplayOrder ON HomePageSectionHotels(DisplayOrder);
END
GO

-- Create HomePageSectionDestinations table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HomePageSectionDestinations')
BEGIN
    CREATE TABLE HomePageSectionDestinations (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SectionId UNIQUEIDENTIFIER NOT NULL,
        DestinationId NVARCHAR(50) NOT NULL,
        DisplayOrder INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_SectionDestinations_Section FOREIGN KEY (SectionId) 
            REFERENCES HomePageSections(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_Section_Destination UNIQUE (SectionId, DestinationId)
    );

    CREATE INDEX IX_SectionDestinations_SectionId ON HomePageSectionDestinations(SectionId);
    CREATE INDEX IX_SectionDestinations_DisplayOrder ON HomePageSectionDestinations(DisplayOrder);
END
GO

-- Insert default sections
DECLARE @heroId UNIQUEIDENTIFIER = NEWID();
DECLARE @roomTypesId UNIQUEIDENTIFIER = NEWID();
DECLARE @featuresId UNIQUEIDENTIFIER = NEWID();
DECLARE @hotelsId UNIQUEIDENTIFIER = NEWID();
DECLARE @destinationsId UNIQUEIDENTIFIER = NEWID();
DECLARE @romanticId UNIQUEIDENTIFIER = NEWID();
DECLARE @campaignId UNIQUEIDENTIFIER = NEWID();
DECLARE @travelCtaId UNIQUEIDENTIFIER = NEWID();
DECLARE @finalCtaId UNIQUEIDENTIFIER = NEWID();

-- Insert Sections
INSERT INTO HomePageSections (Id, SectionType, IsActive, DisplayOrder, Configuration)
VALUES
    (@heroId, 'hero', 1, 1, 
     '{"backgroundImage":"https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=1920&q=80","gradient":"from-black/60 via-black/40 to-black/60","height":"600px","showSearchForm":true}'),
    
    (@roomTypesId, 'room-types', 1, 2, 
     '{"types":[{"id":"hotel","icon":"Hotel","color":"blue","translationKey":"roomTypes.hotel"},{"id":"resort","icon":"Umbrella","color":"green","translationKey":"roomTypes.resort"},{"id":"apart","icon":"Plane","color":"purple","translationKey":"roomTypes.apart"},{"id":"villa","icon":"Sparkles","color":"orange","translationKey":"roomTypes.villa"}]}'),
    
    (@featuresId, 'features', 1, 3, 
     '{"features":[{"icon":"Sparkles","color":"primary","translationKey":"features.bestPrice"},{"icon":"Shield","color":"secondary","translationKey":"features.secure"},{"icon":"Clock","color":"accent","translationKey":"features.support"},{"icon":"Star","color":"primary","translationKey":"features.hotels"}]}'),
    
    (@hotelsId, 'popular-hotels', 1, 4, 
     '{"layout":"grid-3"}'),
    
    (@destinationsId, 'popular-destinations', 1, 5, 
     '{"layout":"featured-grid"}'),
    
    (@romanticId, 'romantic-tours', 1, 6, 
     '{"layout":"carousel"}'),
    
    (@campaignId, 'campaign-banner', 1, 7, 
     '{"gradient":"from-orange-500 to-red-500"}'),
    
    (@travelCtaId, 'travel-cta', 1, 8, 
     '{"cards":[{"type":"excursions","icon":"MapPin","color":"blue","translationKey":"travel.excursions","affiliateKey":"excursions"},{"type":"car-rental","icon":"Car","color":"green","translationKey":"travel.carRental","affiliateKey":"carRental"},{"type":"flight","icon":"Plane","color":"purple","translationKey":"travel.flights","affiliateKey":"flightBooking"}]}'),
    
    (@finalCtaId, 'final-cta', 1, 9, 
     '{"gradient":"from-primary to-secondary","buttons":[{"text":"Search Hotels","link":"/search","variant":"primary"},{"text":"Learn More","link":"/about","variant":"secondary"}]}');

-- Insert Translations (Turkish, English, German as examples)
INSERT INTO HomePageSectionTranslations (SectionId, Locale, Title, Subtitle)
VALUES
    -- Hero Section
    (@heroId, 'tr', 'Mükemmel Konaklama Yerinizi Bulun', 'Harika otelleri keşfedin ve unutulmaz bir tatil geçirin'),
    (@heroId, 'en', 'Find Your Perfect Stay', 'Discover amazing hotels and have an unforgettable vacation'),
    (@heroId, 'de', 'Finden Sie Ihre perfekte Unterkunft', 'Entdecken Sie erstaunliche Hotels und verbringen Sie einen unvergesslichen Urlaub'),
    
    -- Room Types
    (@roomTypesId, 'tr', 'Konaklama Türleri', 'Size uygun oteli bulun'),
    (@roomTypesId, 'en', 'Room Types', 'Find the right hotel for you'),
    (@roomTypesId, 'de', 'Zimmertypen', 'Finden Sie das richtige Hotel für Sie'),
    
    -- Features
    (@featuresId, 'tr', 'Neden Bizi Seçmelisiniz?', 'Hizmetlerimizin avantajları'),
    (@featuresId, 'en', 'Why Choose Us?', 'Benefits of our services'),
    (@featuresId, 'de', 'Warum uns wählen?', 'Vorteile unserer Dienstleistungen'),
    
    -- Popular Hotels
    (@hotelsId, 'tr', 'Popüler Oteller', 'En çok tercih edilen 5 yıldızlı oteller'),
    (@hotelsId, 'en', 'Popular Hotels', 'Most preferred 5-star hotels'),
    (@hotelsId, 'de', 'Beliebte Hotels', 'Die beliebtesten 5-Sterne-Hotels'),
    
    -- Popular Destinations
    (@destinationsId, 'tr', 'Popüler Destinasyonlar', 'En çok ziyaret edilen tatil bölgeleri'),
    (@destinationsId, 'en', 'Popular Destinations', 'Most visited holiday regions'),
    (@destinationsId, 'de', 'Beliebte Reiseziele', 'Meist besuchte Urlaubsregionen'),
    
    -- Romantic Tours
    (@romanticId, 'tr', 'Romantik Turlar', 'Sevdiklerinizle unutulmaz anlar'),
    (@romanticId, 'en', 'Romantic Tours', 'Unforgettable moments with your loved ones'),
    (@romanticId, 'de', 'Romantische Touren', 'Unvergessliche Momente mit Ihren Liebsten'),
    
    -- Campaign Banner
    (@campaignId, 'tr', 'Özel Fırsatlar', 'Kaçırılmayacak kampanyalar'),
    (@campaignId, 'en', 'Special Offers', 'Don''t miss out on campaigns'),
    (@campaignId, 'de', 'Sonderangebote', 'Verpassen Sie keine Kampagnen'),
    
    -- Travel CTA
    (@travelCtaId, 'tr', 'Tatilinizi Tamamlayın', 'Transfer, turlar ve daha fazlası'),
    (@travelCtaId, 'en', 'Complete Your Vacation', 'Transfer, tours and more'),
    (@travelCtaId, 'de', 'Vervollständigen Sie Ihren Urlaub', 'Transfer, Touren und mehr'),
    
    -- Final CTA
    (@finalCtaId, 'tr', 'Hemen Rezervasyon Yapın', 'Hayalinizdeki tatile bugün başlayın'),
    (@finalCtaId, 'en', 'Book Now', 'Start your dream vacation today'),
    (@finalCtaId, 'de', 'Jetzt buchen', 'Beginnen Sie noch heute Ihren Traumurlaub');
GO
```

---

## Testing Checklist

- [ ] Create HomePageSections table
- [ ] Insert default sections
- [ ] Test GET /api/v1/public/homepage/sections
- [ ] Test GET /api/v1/admin/homepage/sections (with auth)
- [ ] Test POST section creation
- [ ] Test PUT section update
- [ ] Test DELETE section
- [ ] Test PATCH reorder
- [ ] Test PATCH toggle
- [ ] Verify JSON configuration parsing
- [ ] Test with invalid section types
- [ ] Test authorization (admin only)

---

## Notes for Frontend Integration

1. **Environment Variable**: Frontend will call `NEXT_PUBLIC_API_URL/public/homepage/sections`
2. **Caching**: Consider caching this data with SWR or React Query (revalidate every 5 minutes)
3. **Dynamic Import**: Use dynamic imports for section components to reduce bundle size
4. **Fallback**: If API fails, show default sections from static data
5. **Localization**: Section titles/subtitles can be localized using i18n translation keys
