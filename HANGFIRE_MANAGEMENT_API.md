# Hangfire Management API Documentation

Admin panelinden Hangfire job'larını yönetmek için RESTful API endpoint'leri.

## Base URL
```
/api/v1/admin/hangfire
```

**Authentication:** Bearer Token (Admin role gerekli)

---

## 📋 Recurring Jobs

### 1. Tüm Recurring Job'ları Listele
```http
GET /recurring-jobs
```

**Response:**
```json
[
  {
    "id": "sunhotels-static-data-sync",
    "cron": "0 3 * * *",
    "nextExecution": "2025-12-28T03:00:00Z",
    "lastExecution": "2025-12-27T03:00:00Z",
    "lastJobState": "Succeeded",
    "job": "SyncAllStaticDataAsync"
  }
]
```

### 2. Job'ı Manuel Tetikle
```http
POST /recurring-jobs/{jobId}/trigger
```

**Örnek:**
```bash
curl -X POST https://api.freestays.com/api/v1/admin/hangfire/recurring-jobs/sunhotels-static-data-sync/trigger \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response:**
```json
{
  "message": "Job triggered successfully",
  "jobId": "sunhotels-static-data-sync"
}
```

### 3. Job'ı Sil (Durdur)
```http
DELETE /recurring-jobs/{jobId}
```

**Response:**
```json
{
  "message": "Job removed successfully",
  "jobId": "sunhotels-static-data-sync"
}
```

### 4. Job Zamanlamasını Güncelle
```http
PUT /recurring-jobs/{jobId}/schedule
```

**Request Body:**
```json
{
  "cronExpression": "0 */6 * * *",
  "timeZone": "Europe/Istanbul"
}
```

**Response:**
```json
{
  "message": "Schedule updated successfully",
  "jobId": "sunhotels-static-data-sync",
  "cron": "0 */6 * * *"
}
```

---

## 🔄 Processing Jobs (Çalışan Job'lar)

### 5. Çalışan Job'ları Listele
```http
GET /processing-jobs
```

**Response:**
```json
[
  {
    "jobId": "123456",
    "serverId": "server-1",
    "startedAt": "2025-12-27T02:24:00Z",
    "job": "SyncAllStaticDataAsync"
  }
]
```

### 6. Belirli Bir Job'ı İptal Et
```http
DELETE /jobs/{jobId}
```

**Response:**
```json
{
  "message": "Job deleted successfully",
  "jobId": "123456"
}
```

---

## 📊 Queue Yönetimi

### 7. Queue İstatistikleri
```http
GET /queue/stats
```

**Response:**
```json
{
  "enqueued": 5,
  "failed": 2,
  "processing": 1,
  "scheduled": 0,
  "succeeded": 150,
  "deleted": 10,
  "recurring": 2,
  "servers": 1,
  "queues": [
    {
      "name": "default",
      "length": 5
    }
  ]
}
```

### 8. Başarısız Job'ları Temizle
```http
DELETE /queue/failed
```

**Response:**
```json
{
  "message": "Cleared 5 failed jobs",
  "count": 5
}
```

### 9. Çalışan Job'ları İptal Et
```http
DELETE /queue/processing
```

**Response:**
```json
{
  "message": "Cleared 2 processing jobs",
  "count": 2
}
```

---

## 📜 Job History

### 10. Job History Listele
```http
GET /history?page=1&pageSize=20&jobType=SyncAllStaticData&status=Failed
```

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `jobType` (string, optional)
- `status` (string, optional: Running, Completed, Failed)

**Response:**
```json
{
  "total": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8,
  "jobs": [
    {
      "id": 1,
      "jobType": "SyncAllStaticData",
      "status": "Failed",
      "startTime": "2025-12-27T02:24:00Z",
      "endTime": "2025-12-27T02:25:00Z",
      "errorMessage": "Connection timeout",
      "duration": "00:01:00"
    }
  ]
}
```

### 11. Stuck Job'ları Temizle
```http
POST /history/cleanup-stuck?olderThanMinutes=30
```

**Query Parameters:**
- `olderThanMinutes` (int, default: 30) - Bu süreden uzun Running olan job'ları temizle

**Response:**
```json
{
  "message": "Cleaned 10 stuck jobs",
  "count": 10,
  "jobs": [
    {
      "id": 1,
      "jobType": "SyncHotels",
      "startTime": "2025-12-27T01:00:00Z"
    }
  ]
}
```

---

## 🕐 Cron Presets

### 12. Hazır Cron Expression'ları Al
```http
GET /cron-presets
```

**Response:**
```json
[
  { "name": "Her 5 dakikada", "cron": "*/5 * * * *" },
  { "name": "Her 15 dakikada", "cron": "*/15 * * * *" },
  { "name": "Her 30 dakikada", "cron": "*/30 * * * *" },
  { "name": "Her saat başı", "cron": "0 * * * *" },
  { "name": "Her 6 saatte", "cron": "0 */6 * * *" },
  { "name": "Günde 1 kez (gece 00:00)", "cron": "0 0 * * *" },
  { "name": "Günde 1 kez (sabah 03:00)", "cron": "0 3 * * *" }
]
```

---

## 🖥️ Server Bilgileri

### 13. Hangfire Server'ları Listele
```http
GET /servers
```

**Response:**
```json
[
  {
    "name": "server-1:12345:abcd1234",
    "workersCount": 20,
    "queues": ["default"],
    "startedAt": "2025-12-27T00:00:00Z",
    "heartbeat": "2025-12-27T02:30:00Z"
  }
]
```

---

## 💡 Frontend Kullanım Örnekleri

### React/Next.js Component

```typescript
// Recurring job'ları listele
const { data: jobs } = await fetch('/api/v1/admin/hangfire/recurring-jobs', {
  headers: { Authorization: `Bearer ${token}` }
});

// Job'ı tetikle
await fetch(`/api/v1/admin/hangfire/recurring-jobs/${jobId}/trigger`, {
  method: 'POST',
  headers: { Authorization: `Bearer ${token}` }
});

// Zamanlamayı güncelle
await fetch(`/api/v1/admin/hangfire/recurring-jobs/${jobId}/schedule`, {
  method: 'PUT',
  headers: { 
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    cronExpression: '0 */6 * * *',
    timeZone: 'Europe/Istanbul'
  })
});

// Stuck job'ları temizle
await fetch('/api/v1/admin/hangfire/history/cleanup-stuck?olderThanMinutes=30', {
  method: 'POST',
  headers: { Authorization: `Bearer ${token}` }
});

// Queue stats
const { data: stats } = await fetch('/api/v1/admin/hangfire/queue/stats', {
  headers: { Authorization: `Bearer ${token}` }
});
```

### UI Component Önerisi

```tsx
// components/admin/HangfireManager.tsx
import { useState } from 'react';

export function HangfireManager() {
  const [jobs, setJobs] = useState([]);
  
  const handleTrigger = async (jobId: string) => {
    await api.post(`/hangfire/recurring-jobs/${jobId}/trigger`);
    toast.success('Job başlatıldı');
  };
  
  const handleUpdateSchedule = async (jobId: string, cron: string) => {
    await api.put(`/hangfire/recurring-jobs/${jobId}/schedule`, {
      cronExpression: cron
    });
    toast.success('Zamanlama güncellendi');
  };
  
  return (
    <div>
      {jobs.map(job => (
        <div key={job.id}>
          <h3>{job.id}</h3>
          <p>Cron: {job.cron}</p>
          <button onClick={() => handleTrigger(job.id)}>Başlat</button>
          <select onChange={e => handleUpdateSchedule(job.id, e.target.value)}>
            <option value="0 3 * * *">Günde 1 (03:00)</option>
            <option value="0 */6 * * *">Her 6 saat</option>
            <option value="0 */12 * * *">Her 12 saat</option>
          </select>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔒 Güvenlik

- Tüm endpoint'ler `[Authorize(Roles = "Admin")]` ile korunuyor
- Sadece Admin rolüne sahip kullanıcılar erişebilir
- Bearer token authentication gerekli

---

## 🚀 Kullanım Senaryoları

### 1. Stuck Job Temizleme
```bash
# 30 dakikadan uzun Running olan job'ları temizle
POST /history/cleanup-stuck?olderThanMinutes=30
```

### 2. Job Zamanlamasını Değiştirme
```bash
# Static sync'i günde 1'den 2'ye çıkar
PUT /recurring-jobs/sunhotels-static-data-sync/schedule
{
  "cronExpression": "0 3,15 * * *"  # Saat 03:00 ve 15:00'te
}
```

### 3. Manuel Sync Tetikleme
```bash
# Hemen sync başlat
POST /recurring-jobs/sunhotels-static-data-sync/trigger
```

### 4. Tüm Kuyruğu Temizleme
```bash
# Failed job'ları temizle
DELETE /queue/failed

# Processing job'ları iptal et
DELETE /queue/processing
```
