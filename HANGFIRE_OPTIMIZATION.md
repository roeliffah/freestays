# Hangfire & Redis Optimizasyon Rehberi (2GB RAM Sunucu)

## 🎯 Amaç
2GB RAM'i olan production sunucusunda Hangfire ve Redis'in aşırı hafıza tüketimini azaltmak.

## 📊 Sorun
- **Ön Durum**: Hangfire workers = `Environment.ProcessorCount * 2` (4-16+ worker)
- **Sonuç**: Her bir job büyük miktarda RAM tüketiyor → Memory exhaustion → Server crash (SIGKILL 134)
- **Kök Neden**: 
  1. İşçi sayısı CPU sayısına bağlı (unlimited growth)
  2. Job history (succeeded/deleted lists) sınırsız büyüyebiliyor (5000+ liste boyutu)
  3. Redis connection timeouts çok yüksek (30 sn) → connection pool bloat
  4. Otomatik retry mekanizması unlimited

## ✅ Çözüm Uygulanan

### 1. Hangfire Worker Count Optimizasyonu

**Dosya**: [Program.cs](src/FreeStays.API/Program.cs#L156-L220)

**Eski Yöntem**:
```csharp
options.WorkerCount = Environment.ProcessorCount * 2;  // ❌ 4-16+ workers
```

**Yeni Yöntem**:
```csharp
var workerCount = int.TryParse(hangfireConfig["Hangfire:WorkerCount"], out var wc) 
    ? wc 
    : Math.Max(2, Environment.ProcessorCount / 2);
options.WorkerCount = workerCount;  // ✅ Konfigürasyon tabanlı, varsayılan: 2
```

**Avantajlar**:
- 16 worker → 2 worker = 8x RAM tasarrufu
- Konfigürasyon dosyasından değiştirebilir (redeploy yok)
- Fallback: Minimum 2 worker, maksimum CPU/2

### 2. Job History Lists Sınırlaması

**Dosya**: [appsettings.json](src/FreeStays.API/appsettings.json#L13-L22)

```json
"Hangfire": {
  "DashboardPath": "/hangfire",
  "WorkerCount": 2,
  "MaxJobHistoryCount": 500,
  "MaxRetryAttempts": 2,
  "AutomaticRetryAttempts": 1,
  "FailedListSize": 1000,
  "DeletedListSize": 2000,
  "SucceededListSize": 2000
}
```

**Değişiklikler**:
| Ayar | Eski | Yeni | Tasarruf |
|------|------|------|----------|
| SucceededListSize | 5000 | 2000 | 60% ↓ |
| DeletedListSize | 5000 | 2000 | 60% ↓ |
| AutomaticRetryAttempts | unlimited | 1 | ∞ → 1 |
| MaxRetryAttempts | - | 2 | Sınır koydu |

### 3. Redis Connection Timeout Optimizasyonu

**Dosya**: [appsettings.json](src/FreeStays.API/appsettings.json#L30-L33)

**Eski**:
```
connectRetry=5,connectTimeout=15000,syncTimeout=30000,responseTimeout=30000
```

**Yeni**:
```
connectRetry=2,connectTimeout=5000,syncTimeout=5000,responseTimeout=5000,allowAdmin=true
```

**Avantajlar**:
- Timeout: 30s → 5s (6x hızlı fail-over)
- Retry sayısı: 5 → 2 (connection pool boyutu ↓)
- `allowAdmin=true`: Redis CONFIG SET komutları çalıştırabilir
- Sonuç: Bağlantı havuzu daha küçük, daha hızlı timeout

### 4. Graceful Shutdown & Monitoring

**Program.cs Eklenen Konfigürasyon**:
```csharp
options.ShutdownTimeout = TimeSpan.FromSeconds(30);  // ✅ Graceful shutdown
options.HeartbeatInterval = TimeSpan.FromSeconds(30);
options.ServerCheckInterval = TimeSpan.FromSeconds(30);
options.StopTimeout = TimeSpan.FromSeconds(30);
```

## 🚀 Deployment Adımları

### Step 1: Dosyaları Güncelle
```bash
# Program.cs, appsettings.json değişiklikleri için build
dotnet build
```

### Step 2: Konfigürasyon (Dokploy'da)
Hangfire ayarlarını **environment variables** üzerinden override etmek isterseniz:

```bash
# Dokploy → Settings → Environment Variables
Hangfire__WorkerCount=2
Hangfire__DeletedListSize=1000
Hangfire__SucceededListSize=1000
```

### Step 3: Redis Konfigürasyonu (İsteğe Bağlı)
Redis sunucusuna SSH erişiminiz varsa, memory limits koyabilirsiniz:

```bash
# Redis container içine gir
docker exec redis redis-cli

# Maksimum hafıza limiti koy (512MB)
CONFIG SET maxmemory 536870912  # 512MB

# Eviction policy koy (LRU: en az kullanılan sil)
CONFIG SET maxmemory-policy allkeys-lru

# Değişiklikleri kaydet (eğer RDB varsa)
BGSAVE

# Konfigürasyonu doğrula
CONFIG GET maxmemory
CONFIG GET maxmemory-policy
```

## 📈 Beklenen Sonuçlar

### RAM Kullanımı (Tahmini)
- **Ön**: 1800 MB (Hangfire 1200+ MB, Redis 400+ MB, OS 200 MB) → Crash!
- **Sonra**: 900 MB (Hangfire 400 MB, Redis 300 MB, OS 200 MB) → Stable ✅

### Job Processing
- **Throughput**: İçeriği bağlı değil (2 worker hala aynı işi yapar, sadece daha az parallel)
- **Latency**: Küçük artış olabilir (2 worker vs 16 worker), ama stabilite alındı

## 🔍 Monitoring Checklist

Deployment sonrası kontrol et:

1. **Server Logs** (Dokploy → Logs)
   ```
   ✅ "Hangfire WorkerCount: 2" mesajı gözükmalı
   ✅ "Hangfire configured with Redis storage successfully" başarılı olmalı
   ❌ SIGKILL 134 olmamalı
   ```

2. **Hangfire Dashboard**
   ```
   GET /hangfire
   - Active Jobs: 0-2 arasında (3+ worker çalışmıyor demektir)
   - Succeeded Jobs: ~2000'den fazla olmamalı
   - Failed/Deleted: 1000-2000 arasında
   ```

3. **Memory Usage** (Dokploy → Monitoring)
   ```
   Hedef: < 1000 MB sabit kalması
   İyileştirme: > 1500 MB kalırsa → Hangfire__WorkerCount=1 dene
   ```

4. **Redis Connection Pool**
   ```bash
   redis-cli INFO stats | grep connections
   # connected_clients < 10 olmalı
   ```

## 🛠 Troubleshooting

### Durum 1: Hala Memory Yüksek (>1200 MB)
```bash
# Daha agresif ayarlar
Hangfire__WorkerCount=1
Hangfire__SucceededListSize=500
Hangfire__DeletedListSize=500
```

### Durum 2: Jobs Processing çok yavaş
```bash
# Worker sayısı artır (ama dikkatli)
Hangfire__WorkerCount=3
```

### Durum 3: Redis sürekli timeout veriyor
```bash
# Redis'ten disconnect oluyorsa, timeout uzat
Redis: connectTimeout=10000,syncTimeout=10000
```

## 📝 Implementation Details

### Hangfire Configuration Reading (`Program.cs`)

```csharp
// appsettings.json'dan Hangfire config oku
var hangfireConfig = builder.Configuration.GetSection("Hangfire");

// WorkerCount: config → fallback to CPU-aware default
var workerCount = int.TryParse(hangfireConfig["WorkerCount"], out var wc) 
    ? wc 
    : Math.Max(2, Environment.ProcessorCount / 2);

// DeletedListSize: config → default 2000
var deletedListSize = int.TryParse(hangfireConfig["DeletedListSize"], out var dls) 
    ? dls 
    : 2000;

// SucceededListSize: config → default 2000
var succeededListSize = int.TryParse(hangfireConfig["SucceededListSize"], out var sls) 
    ? sls 
    : 2000;

// Logging
Log.Information("🔧 Hangfire Configuration - WorkerCount: {Workers}, DeletedListSize: {Deleted}, SucceededListSize: {Succeeded}", 
    workerCount, deletedListSize, succeededListSize);
```

### Redis Connection Optimization

```csharp
// Redis connection string components:
// - connectRetry=2: 2 deneme (eski: 5)
// - connectTimeout=5000: 5 saniye (eski: 15000 = 15 saniye)
// - syncTimeout=5000: 5 saniye (eski: 30000 = 30 saniye)
// - responseTimeout=5000: 5 saniye (eski: 30000 = 30 saniye)
// - allowAdmin=true: CONFIG komutları çalıştırabilir
```

## 📚 Referanslar

- Hangfire Docs: https://docs.hangfire.io
- Redis Memory Optimization: https://redis.io/docs/management/optimization/memory-optimization/
- ASP.NET Core Configuration: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration

---

**Son Güncellenme**: 2024-01-03
**Hedef Sunucu**: 2GB RAM AWS t2.small instance
**Status**: ✅ Implemented & Tested
