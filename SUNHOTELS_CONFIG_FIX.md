# SunHotels Configuration - Database'den Credential Yükleme

## 🔧 Sorun

Logda sürekli 403 (Forbidden) hatası:
```
Could not find login credentials
userName=&password=&
```

**Sebep:** SunHotels API credentials'ları database'de seed ediliyor ama `SunHotelsService` bunları kullanmıyor, boş string olarak kalıyordu.

## ✅ Çözüm

Database'deki `external_service_configs` tablosundan SunHotels credentials'larını otomatik olarak yükleme implementasyonu eklendi.

## 📝 Yapılan Değişiklikler

### 1. Repository Eklendi

**IExternalServiceConfigRepository.cs**
```csharp
public interface IExternalServiceConfigRepository : IRepository<ExternalServiceConfig>
{
    Task<ExternalServiceConfig?> GetByServiceNameAsync(string serviceName, CancellationToken cancellationToken = default);
}
```

**ExternalServiceConfigRepository.cs**
```csharp
public class ExternalServiceConfigRepository : Repository<ExternalServiceConfig>, IExternalServiceConfigRepository
{
    public async Task<ExternalServiceConfig?> GetByServiceNameAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        return await _context.ExternalServiceConfigs
            .FirstOrDefaultAsync(x => x.ServiceName == serviceName && x.IsActive, cancellationToken);
    }
}
```

### 2. DependencyInjection.cs Güncellendi

```csharp
// Repository eklendi
services.AddScoped<IExternalServiceConfigRepository, ExternalServiceConfigRepository>();
```

### 3. SunHotelsService.cs Güncellendi

**Constructor'da repository injection:**
```csharp
private readonly IExternalServiceConfigRepository _serviceConfigRepository;
private bool _configLoaded = false;

public SunHotelsService(
    HttpClient httpClient,
    IOptions<SunHotelsConfig> config,
    ILogger<SunHotelsService> logger,
    IExternalServiceConfigRepository serviceConfigRepository)
{
    _httpClient = httpClient;
    _config = config.Value;
    _logger = logger;
    _serviceConfigRepository = serviceConfigRepository;
    
    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
}
```

**Config loading metodu:**
```csharp
private async Task EnsureConfigLoadedAsync(CancellationToken cancellationToken = default)
{
    if (_configLoaded) return;

    try
    {
        var dbConfig = await _serviceConfigRepository.GetByServiceNameAsync("SunHotels", cancellationToken);
        
        if (dbConfig != null && dbConfig.IsActive)
        {
            _config.Username = dbConfig.Username ?? string.Empty;
            _config.Password = dbConfig.Password ?? string.Empty;
            _config.BaseUrl = dbConfig.BaseUrl;
            _config.AffiliateCode = dbConfig.AffiliateCode;
            
            _logger.LogInformation("SunHotels configuration loaded from database for service: {ServiceName}", dbConfig.ServiceName);
        }
        else
        {
            _logger.LogWarning("SunHotels configuration not found in database, using default config");
        }

        _configLoaded = true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load SunHotels configuration from database, using default config");
        _configLoaded = true;
    }
}
```

**Tüm public metodlara eklendi:**
```csharp
public async Task<List<SunHotelsDestination>> GetDestinationsAsync(...)
{
    await EnsureConfigLoadedAsync(cancellationToken);
    // ... rest of the method
}
```

### 4. SunHotelsConfig Model Güncellendi

```csharp
public class SunHotelsConfig
{
    public string BaseUrl { get; set; } = "http://xml.sunhotels.net/15/PostGet/NonStaticXMLAPI.asmx";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? AffiliateCode { get; set; }  // ← YENİ
}
```

## 🎯 Nasıl Çalışır

1. **İlk API Çağrısı:** `SunHotelsService` üzerinden herhangi bir metod çağrıldığında
2. **Config Loading:** `EnsureConfigLoadedAsync()` database'den credentials'ları çeker
3. **Caching:** Config bir kere yüklendikten sonra memory'de kalır (`_configLoaded` flag'i)
4. **Fallback:** Database'den yüklenemezse, default config kullanılır (boş strings)

## 🗄️ Database Seed

DatabaseSeeder.cs zaten credentials'ları seed ediyor:

```csharp
var sunHotelsConfig = new ExternalServiceConfig
{
    Id = Guid.NewGuid(),
    ServiceName = "SunHotels",
    BaseUrl = "http://xml.sunhotels.net/15/PostGet/NonStaticXMLAPI.asmx",
    Username = "your_username_here",
    Password = "your_password_here",
    IsActive = true,
    IntegrationMode = ServiceIntegrationMode.Api,
    CreatedAt = DateTime.UtcNow
};
```

## ⚠️ ÖNEMLİ

Database'de SunHotels credentials'larını doğru değerlerle güncellemelisin:

```sql
UPDATE external_service_configs 
SET 
    username = 'GERÇEK_USERNAME',
    password = 'GERÇEK_PASSWORD'
WHERE service_name = 'SunHotels';
```

Veya Admin Panel üzerinden güncelleyebilirsin (External Service Config yönetimi geliştirilmeli).

## 🚀 Deployment

1. **Build:** ✅ Başarılı
2. **Database:** SunHotels credentials'ları güncelle
3. **Deploy:** Dokploy'a push et
4. **Test:** Background job loglarını kontrol et

## 📊 Test Senaryosu

Uygulama başladığında Hangfire job çalışacak:

```
[02:05:19 INF] SunHotels configuration loaded from database for service: SunHotels
[02:05:19 INF] Sending static request to SunHotels: GetStaticHotelsAndRooms - URL: http://xml.sunhotels.net/15/PostGet/StaticXMLAPI.asmx/GetStaticHotelsAndRooms?userName=ACTUAL_USERNAME&password=ACTUAL_PASSWORD&...
```

Artık `userName=&password=&` yerine gerçek credentials göreceksin!

## 🔍 Debugging

Credentials doğru yüklendi mi kontrol et:

```bash
# Container'a bağlan
docker exec -it [container-id] bash

# Database'i kontrol et
psql -h 3.72.175.63 -p 4848 -U usrarvas -d freestays

# Credentials'ları kontrol et
SELECT service_name, username, is_active FROM external_service_configs WHERE service_name = 'SunHotels';
```

## 🎉 Sonuç

Artık SunHotels credentials'ları:
- ✅ Database'den otomatik yükleniyor
- ✅ Runtime'da güncellenebilir (database'i değiştir, container restart)
- ✅ Güvenli (appsettings.json'da hardcode yok)
- ✅ Merkezi yönetim (tüm external services için tek yer)
