# Redis ve Hangfire Yapılandırma Rehberi

## 📚 İçindekiler
1. [Hangfire Nedir?](#hangfire-nedir)
2. [Redis Yapılandırması](#redis-yapılandırması)
3. [Adım Adım Kurulum](#adım-adım-kurulum)
4. [Test ve Doğrulama](#test-ve-doğrulama)
5. [Production Önerileri](#production-önerileri)

---

## 🔥 Hangfire Nedir?

**Hangfire** = ASP.NET Core için arka plan iş yönetim sistemi

### Senin Projende Kullanım Alanları:

1. **SunHotels Veri Senkronizasyonu**
   - Günlük otomatik senkronizasyon
   - Otel, oda, destinasyon verilerini güncelleme
   - Job: `SunHotelsStaticDataSyncJob`

2. **Periyodik Görevler**
   - Email gönderimi (kuyruklu)
   - Raporlama işleri
   - Veri temizleme (cleanup)

3. **Dashboard**: `https://your-domain.com/hangfire`
   - İşleri görüntüleme
   - Manuel tetikleme
   - Hata izleme

### Şu Anki Durumun:
```csharp
// Program.cs - Line 117
builder.Services.AddHangfire(config => config.UseInMemoryStorage());
```

⚠️ **Sorun**: InMemory storage, uygulama yeniden başlayınca tüm job geçmişini siler!

---

## 🗄️ Redis Yapılandırması

### Mevcut Redis Bağlantın (appsettings.json):
```json
"Redis": "freestays-cachedb-aucb6o:6379,password=Barneveld2026,ssl=false,abortConnect=false"
```

### Şu An Çalışan:
✅ **Cache Service** - Redis'i kullanıyor (`RedisCacheService`)
- SunHotels API sonuçları cache'leniyor
- Performans artışı sağlıyor

### Eksik Olan:
❌ **Hangfire Storage** - Hala InMemory kullanıyor
- Job geçmişi kaybolabilir
- Multi-instance çalışmaz

---

## 🚀 Adım Adım Kurulum

### **1. NuGet Paketini Yükle**

```bash
cd /Users/halityilmaz/Programlar/Web/freestaysapi/src/FreeStays.API
dotnet add package Hangfire.Redis.StackExchange
```

### **2. Program.cs Güncelle**

**Eski Kod (Line 116-121):**
```csharp
// Hangfire - InMemory Storage (Development) - PostgreSQL opsiyonel
builder.Services.AddHangfire(config =>
{
    config.UseInMemoryStorage();
});
builder.Services.AddHangfireServer();
```

**Yeni Kod:**
```csharp
using StackExchange.Redis;
using Hangfire.Redis.StackExchange;

// Hangfire - Redis Storage (Production Ready)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    try
    {
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        builder.Services.AddHangfire(config =>
        {
            config.UseRedisStorage(redisOptions, new RedisStorageOptions
            {
                Prefix = "hangfire:",
                ExpiryCheckInterval = TimeSpan.FromHours(1),
                DeletedListSize = 5000,
                SucceededListSize = 5000
            });
        });
        Log.Information("Hangfire configured with Redis storage");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis connection failed, falling back to InMemory storage");
        builder.Services.AddHangfire(config => config.UseInMemoryStorage());
    }
}
else
{
    // Development fallback
    builder.Services.AddHangfire(config => config.UseInMemoryStorage());
    Log.Information("Hangfire configured with InMemory storage (Development)");
}

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = Environment.MachineName;
    options.WorkerCount = Environment.ProcessorCount * 2;
});
```

### **3. appsettings.json (Zaten Var)**

Mevcut Redis bağlantın çalışıyor:
```json
{
  "ConnectionStrings": {
    "Redis": "freestays-cachedb-aucb6o:6379,password=Barneveld2026,ssl=false,abortConnect=false"
  },
  "Hangfire": {
    "DashboardPath": "/hangfire",
    "ServerName": "FreeStays-API",
    "WorkerCount": 4
  }
}
```

### **4. Recurring Job Ekle (Opsiyonel)**

SunHotels senkronizasyonunu günlük otomatik çalıştırmak için:

**Program.cs - Line 285 civarına ekle (app.Run()'dan önce):**
```csharp
// Recurring Jobs - Günlük SunHotels senkronizasyonu
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    
    // Her gün saat 03:00'de çalış
    recurringJobManager.AddOrUpdate<SunHotelsStaticDataSyncJob>(
        "sunhotels-daily-sync",
        job => job.SyncAllStaticDataAsync(),
        Cron.Daily(3), // Her gün 03:00
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")
        });
    
    Log.Information("Recurring job scheduled: SunHotels daily sync at 03:00 AM");
}
```

**Cron Örnekleri:**
```csharp
Cron.Daily(3)           // Her gün 03:00
Cron.Hourly()           // Her saat
Cron.Daily()            // Her gün 00:00
Cron.Weekly()           // Pazar günleri 00:00
Cron.Monthly()          // Ayın 1'i 00:00
```

---

## ✅ Test ve Doğrulama

### **1. Redis Bağlantısını Test Et**

```bash
# Terminal'de
redis-cli -h freestays-cachedb-aucb6o -p 6379 -a Barneveld2026

# Redis CLI içinde
> PING
# PONG dönmeli

> KEYS hangfire:*
# Hangfire key'leri görmelisin

> INFO stats
# Redis istatistiklerini görürsün
```

### **2. Hangfire Dashboard**

1. Uygulamayı başlat: `dotnet run`
2. Tarayıcıda aç: `http://localhost:5000/hangfire`
3. Görmem gerekenler:
   - ✅ Recurring Jobs sekmesi
   - ✅ Job geçmişi
   - ✅ Başarılı/Başarısız işler

### **3. Manuel Job Tetikle**

**AdminController.cs içinde zaten var:**
```csharp
[HttpPost("services/sunhotels/sync")]
public IActionResult SyncSunHotelsData()
{
    BackgroundJob.Enqueue<SunHotelsStaticDataSyncJob>(job => job.SyncAllStaticDataAsync());
    return Ok(new { message = "Senkronizasyon başlatıldı." });
}
```

**API çağrısı:**
```bash
curl -X POST http://localhost:5000/api/v1/admin/services/sunhotels/sync
```

### **4. Logları Kontrol Et**

```bash
# Terminal'de canlı log izle
tail -f src/FreeStays.API/logs/log-*.txt

# Hangfire loglarını filtrele
grep "Hangfire" src/FreeStays.API/logs/log-*.txt
```

---

## 🏭 Production Önerileri

### **1. Redis Memory Optimizasyonu**

```csharp
new RedisStorageOptions
{
    Prefix = "hangfire:",
    ExpiryCheckInterval = TimeSpan.FromHours(1),
    DeletedListSize = 5000,          // Max silinen job sayısı
    SucceededListSize = 5000,        // Max başarılı job sayısı
    InvisibilityTimeout = TimeSpan.FromMinutes(30)
}
```

### **2. Hangfire Server Optimizasyonu**

```csharp
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}",
    options.WorkerCount = Environment.ProcessorCount * 2,
    options.Queues = new[] { "default", "critical", "normal" },
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15)
});
```

### **3. Hangfire Dashboard Güvenliği**

**Zaten implementesiz (Program.cs - Line 277):**
```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Custom authorization filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // Sadece Admin/SuperAdmin erişebilir
        return httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("SuperAdmin");
    }
}
```

### **4. Redis Connection Resilience**

```csharp
var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
redisOptions.ConnectRetry = 3;
redisOptions.ConnectTimeout = 5000;
redisOptions.SyncTimeout = 5000;
redisOptions.AbortOnConnectFail = false;
redisOptions.KeepAlive = 60;
```

### **5. Job Retry Stratejisi**

```csharp
// Job'larda automatic retry ekle
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public async Task SyncAllStaticDataAsync()
{
    // Job logic
}
```

### **6. Monitoring ve Alerts**

```csharp
// Job başarısızlıklarında email gönder
GlobalJobFilters.Filters.Add(new JobFailureNotificationAttribute());

public class JobFailureNotificationAttribute : JobFilterAttribute, IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        var failedState = context.CandidateState as FailedState;
        if (failedState != null)
        {
            // Email/Slack/SMS notification gönder
            Log.Error(failedState.Exception, "Hangfire job failed: {JobId}", context.BackgroundJob.Id);
        }
    }
}
```

---

## 📊 Redis vs InMemory Karşılaştırması

| Özellik | InMemory | Redis |
|---------|----------|-------|
| **Performans** | 🟢 Çok Hızlı | 🟡 Hızlı |
| **Kalıcılık** | 🔴 Yok (Restart = Kayıp) | 🟢 Var (Disk'e yazılır) |
| **Multi-Instance** | 🔴 Çalışmaz | 🟢 Çalışır |
| **Bellek Kullanımı** | 🟡 Uygulama RAM'i | 🟢 Ayrı Redis RAM |
| **Production Ready** | 🔴 Hayır | 🟢 Evet |
| **Monitoring** | 🔴 Kısıtlı | 🟢 Gelişmiş |

---

## 🎯 Özet: Ne Yapmalısın?

### ✅ Yapılacaklar Listesi:

1. **Hangfire.Redis.StackExchange paketini yükle**
   ```bash
   dotnet add package Hangfire.Redis.StackExchange
   ```

2. **Program.cs'i güncelle** (yukarıdaki yeni kodu kullan)

3. **Recurring job ekle** (günlük otomatik sync için)

4. **Test et**:
   - Redis bağlantısını kontrol et
   - Hangfire dashboard'u aç (`/hangfire`)
   - Manuel sync tetikle

5. **Production'a deploy et**

### 📌 Hızlı Başlangıç Komutu:

```bash
# Paketi yükle
cd src/FreeStays.API
dotnet add package Hangfire.Redis.StackExchange

# Build et
dotnet build

# Çalıştır
dotnet run

# Dashboard'u aç
open http://localhost:5000/hangfire
```

---

## 🔗 Faydalı Linkler

- [Hangfire Dokümantasyonu](https://docs.hangfire.io/)
- [Hangfire Redis Storage](https://github.com/marcoCasamento/Hangfire.Redis.StackExchange)
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)
- [Cron Expression Generator](https://crontab.guru/)

---

**Hazırlayan**: GitHub Copilot  
**Tarih**: 26 Aralık 2025  
**Proje**: FreeStays API  
**Durum**: ✅ Production Ready
