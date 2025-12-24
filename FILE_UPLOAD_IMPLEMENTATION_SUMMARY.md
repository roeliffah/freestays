# File Upload Implementasyonu - Özet

## ✅ Yapılan Değişiklikler

### 1. Dockerfile Güncellemesi
**Dosya:** `/Dockerfile`

```dockerfile
# wwwroot/uploads klasörü oluşturuldu ve izinler verildi
RUN mkdir -p /app/wwwroot/uploads && chmod 777 /app/wwwroot/uploads
```

**Amaç:** Docker container içinde upload klasörünün oluşturulması ve yazma izinlerinin verilmesi.

---

### 2. Appsettings.json Güncellemeleri

#### Production (`appsettings.json`)
```json
{
  "FileUpload": {
    "BasePath": "wwwroot/uploads",
    "MaxFileSizeInMB": 5,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
    "BaseUrl": "/uploads"
  }
}
```

#### Development (`appsettings.Development.json`)
```json
{
  "FileUpload": {
    "BasePath": "wwwroot/uploads",
    "MaxFileSizeInMB": 10,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"],
    "BaseUrl": "/uploads"
  }
}
```

**Fark:** Development ortamında dosya boyutu limiti daha yüksek (10 MB) ve .svg formatına izin var.

---

### 3. Yeni Servis Sınıfları

#### a) FileUploadSettings.cs
**Yol:** `src/FreeStays.API/Services/FileUploadSettings.cs`

Configuration binding için settings class.

#### b) IFileUploadService.cs
**Yol:** `src/FreeStays.API/Services/IFileUploadService.cs`

Interface tanımlamaları:
- `UploadFileAsync()` - Dosya yükleme
- `DeleteFileAsync()` - Dosya silme
- `IsValidFileExtension()` - Uzantı kontrolü
- `IsValidFileSize()` - Boyut kontrolü

#### c) FileUploadService.cs
**Yol:** `src/FreeStays.API/Services/FileUploadService.cs`

Tüm file upload operasyonlarını yöneten servis. Özellikler:
- UUID ile unique dosya isimleri
- Alt klasör desteği
- Validation (uzantı, boyut)
- Comprehensive logging
- Error handling

---

### 4. Program.cs Güncellemesi

```csharp
// File Upload Service DI
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// File Upload Settings
builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUpload"));
```

**Not:** `app.UseStaticFiles()` zaten mevcuttu, değişiklik yapılmadı.

---

### 5. Yeni Controller

**Dosya:** `src/FreeStays.API/Controllers/Admin/FileUploadController.cs`

#### Endpoints:

##### 1. Tekil Resim Yükleme
```
POST /api/v1/admin/upload/image?folder=featured-destinations
Authorization: Bearer {token}
Form-Data: file
```

##### 2. Çoklu Resim Yükleme
```
POST /api/v1/admin/upload/images?folder=images
Authorization: Bearer {token}
Form-Data: files[]
```

##### 3. Dosya Silme
```
DELETE /api/v1/admin/upload?fileUrl=/uploads/images/file.jpg
Authorization: Bearer {token}
```

##### 4. Dosya Validasyonu
```
POST /api/v1/admin/upload/validate
Authorization: Bearer {token}
Form-Data: file
```

**Güvenlik:**
- Sadece Admin/SuperAdmin erişebilir
- Request size limitleri (10 MB single, 50 MB multiple)
- File extension validation
- File size validation

---

## 📁 Klasör Yapısı

```
/app/wwwroot/uploads/
  ├── images/                    # Genel görseller
  ├── featured-destinations/     # Featured destination görselleri
  ├── featured-hotels/           # Featured hotel görselleri
  ├── logos/                     # Logo dosyaları
  └── [custom-folders]/          # Özel alt klasörler
```

Her dosya UUID formatında:
```
a1b2c3d4-e5f6-7890-abcd-ef1234567890.jpg
```

---

## 🔧 Dokploy Volume Mount

Dokploy'da aşağıdaki volume mount yapılandırılmalı:

```
Container Path: /app/wwwroot/uploads
Host Path: [Dokploy tarafından yönetilir]
```

Bu sayede container yeniden başlatıldığında dosyalar kaybolmaz.

---

## 🧪 Test Senaryoları

### 1. Featured Destination İçin Görsel Yükleme

```typescript
// 1. Görseli yükle
const formData = new FormData();
formData.append('file', selectedFile);

const uploadResponse = await fetch(
  'https://api.freestays.eu/api/v1/admin/upload/image?folder=featured-destinations',
  {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  }
);

const { url } = await uploadResponse.json();

// 2. Featured destination oluştur
await fetch('https://api.freestays.eu/api/v1/admin/featured-content/destinations', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    destinationId: "123",
    destinationName: "İstanbul",
    countryCode: "TR",
    country: "Türkiye",
    image: url,  // ← Yüklenen görsel
    // ... diğer alanlar
  })
});
```

### 2. Çoklu Görsel Yükleme

```typescript
const formData = new FormData();
files.forEach(file => formData.append('files', file));

const response = await fetch(
  'https://api.freestays.eu/api/v1/admin/upload/images?folder=gallery',
  {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  }
);

const { uploaded, errors } = await response.json();
console.log(`Uploaded: ${uploaded.length}, Errors: ${errors.length}`);
```

---

## 🚀 Deployment Checklist

- [x] Dockerfile güncellendi (`/app/wwwroot/uploads` klasörü)
- [x] appsettings.json'a FileUpload konfigürasyonu eklendi
- [x] FileUploadService implementasyonu tamamlandı
- [x] FileUploadController oluşturuldu
- [x] Program.cs'e DI kayıtları eklendi
- [x] Build başarılı ✅

**Sıradaki Adım:** 
1. Docker image build et
2. Dokploy'a push et
3. Volume mount yapılandır
4. Container'ı başlat
5. Upload endpoint'lerini test et

---

## 📝 Notlar

1. **Static Files:** ASP.NET Core otomatik olarak `wwwroot` klasörünü serve eder. `app.UseStaticFiles()` zaten Program.cs'te mevcut.

2. **URL Format:** Yüklenen dosyalara şu şekilde erişilir:
   ```
   https://api.freestays.eu/uploads/featured-destinations/abc123.jpg
   ```

3. **Güvenlik:** 
   - Sadece authenticated admin kullanıcılar dosya yükleyebilir
   - File extension ve size validasyonları var
   - Unique file names ile overwrite riski yok

4. **Performans:**
   - Single upload: Max 10 MB
   - Multiple upload: Max 50 MB total
   - Async operations ile non-blocking I/O

5. **Error Handling:**
   - Comprehensive logging
   - User-friendly error messages
   - Graceful degradation

---

## 🔍 Debugging

Logları kontrol et:
```bash
docker logs [container-id] | grep -i "upload"
```

Klasör izinlerini kontrol et:
```bash
docker exec -it [container-id] ls -la /app/wwwroot/uploads
```

Test upload:
```bash
curl -X POST "https://api.freestays.eu/api/v1/admin/upload/image" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test.jpg"
```
