# Hangfire Job Cleanup Guide

## Problem
Hangfire job'ları "Running" state'te takılı kalıyor ve birden fazla instance aynı anda çalışıyor.

## Hızlı Temizleme (Dashboard)

1. **Hangfire Dashboard'a git:**
   - Local: `http://localhost:5000/hangfire`
   - Production: `https://your-domain.com/hangfire`

2. **Recurring Jobs sekmesi:**
   - "sunhotels-static-data-sync" → **Trigger** butonuna BASMAYIN (zaten otomatik çalışıyor)
   - Eğer çok fazla çalışıyorsa: **Delete** → **Yes** → **Add again from code**

3. **Jobs sekmesi:**
   - **Processing** tab'ına git
   - Running job'ları seç → **Delete** buton

4. **Failed sekmesi:**
   - Başarısız job'ları seç → **Delete** veya **Requeue**

## Redis'ten Manuel Temizleme

```bash
# Redis'e bağlan
docker exec -it redis redis-cli -h 3.72.175.63 -p 6379

# Hangfire key'lerini listele
KEYS hangfire:*

# Tüm Hangfire verilerini temizle (DİKKATLİ!)
KEYS hangfire:* | xargs redis-cli -h 3.72.175.63 -p 6379 DEL

# Sadece recurring job'ları temizle
DEL hangfire:recurring-jobs
```

## Kod ile Temizleme

### Tüm Recurring Job'ları Kaldır

```csharp
// Program.cs içinde job tanımlamalarından önce
RecurringJob.RemoveIfExists("sunhotels-static-data-sync");
RecurringJob.RemoveIfExists("sunhotels-basic-data-sync");
```

### Bekleyen/Running Job'ları İptal Et

```csharp
var monitoringApi = JobStorage.Current.GetMonitoringApi();
var processing = monitoringApi.ProcessingJobs(0, int.MaxValue);

foreach (var job in processing)
{
    BackgroundJob.Delete(job.Key);
}
```

## Önlem: DisableConcurrentExecution Attribute

Job'ların aynı anda birden fazla çalışmasını engellemek için:

```csharp
[DisableConcurrentExecution(timeoutInSeconds: 3600)] // 1 saat timeout
public async Task SyncAllStaticDataAsync()
{
    // ...
}
```

## Önlem: AutomaticRetry Devre Dışı

Başarısız job'lar sürekli retry edilmesin:

```csharp
[AutomaticRetry(Attempts = 0)]
public async Task SyncAllStaticDataAsync()
{
    // ...
}
```

## Job Durumlarını Kontrol

```bash
# PostgreSQL'de job history
psql -h 3.72.175.63 -p 4848 -U freestays -d freestays

SELECT job_type, status, start_time, end_time 
FROM job_histories 
WHERE job_type IN ('SyncAllStaticData', 'SyncHotels')
ORDER BY start_time DESC 
LIMIT 20;

# Running job'ları güncelle
UPDATE job_histories 
SET status = 'Failed', 
    end_time = NOW(), 
    error_message = 'Manually cancelled due to duplicate runs'
WHERE status = 'Running' 
  AND start_time < NOW() - INTERVAL '1 hour';
```

## Best Practices

1. **Trigger Now butonunu kullanma** - Job'lar otomatik zamanında çalışıyor
2. **Job süresini izle** - Çok uzun sürüyorsa optimization yap
3. **Timeout ekle** - DisableConcurrentExecution ile
4. **Monitoring** - Job history tablosunu düzenli kontrol et
5. **Graceful shutdown** - Job yarıda kesilirse state'i Failed'a çek

## Sorun Devam Ederse

1. Hangfire server'ı restart et
2. Redis'i restart et
3. Application'ı restart et
4. Job tanımlarını kontrol et (duplicate registration var mı?)
