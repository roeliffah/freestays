# File Upload Service - Kullanım Kılavuzu

## Genel Bakış

FreeStays API, görsel dosyaları yüklemek için güvenli ve kullanımı kolay bir file upload servisi sunar. Bu servis, Featured Content, logo ve diğer görseller için kullanılabilir.

## Yapılandırma

### Docker Volume Mount
Dokploy üzerinde aşağıdaki volume mount yapılandırılmıştır:
```
/app/wwwroot/uploads
```

### Dockerfile
Dockerfile içinde uploads klasörü otomatik olarak oluşturulur ve gerekli izinler verilir:
```dockerfile
RUN mkdir -p /app/wwwroot/uploads && chmod 777 /app/wwwroot/uploads
```

### Ayarlar (appsettings.json)

**Production:**
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

**Development:**
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

## API Endpoints

### 1. Tekil Resim Yükleme
**Endpoint:** `POST /api/v1/admin/upload/image`

**Authorization:** Bearer Token (Admin/SuperAdmin)

**Query Parameters:**
- `folder` (optional): Alt klasör adı (örn: "featured-destinations", "logos")

**Form Data:**
- `file`: Yüklenecek resim dosyası

**Örnek Request (cURL):**
```bash
curl -X POST "https://api.freestays.eu/api/v1/admin/upload/image?folder=featured-destinations" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@/path/to/image.jpg"
```

**Örnek Response:**
```json
{
  "success": true,
  "url": "/uploads/featured-destinations/a1b2c3d4-e5f6-7890-abcd-ef1234567890.jpg",
  "fileName": "image.jpg",
  "size": 245678
}
```

### 2. Çoklu Resim Yükleme
**Endpoint:** `POST /api/v1/admin/upload/images`

**Authorization:** Bearer Token (Admin/SuperAdmin)

**Query Parameters:**
- `folder` (optional): Alt klasör adı

**Form Data:**
- `files`: Yüklenecek resim dosyaları (array)

**Örnek Response:**
```json
{
  "success": true,
  "uploaded": [
    {
      "url": "/uploads/images/file1.jpg",
      "fileName": "image1.jpg",
      "size": 123456
    },
    {
      "url": "/uploads/images/file2.jpg",
      "fileName": "image2.jpg",
      "size": 234567
    }
  ],
  "errors": [],
  "totalUploaded": 2,
  "totalErrors": 0
}
```

### 3. Dosya Silme
**Endpoint:** `DELETE /api/v1/admin/upload`

**Authorization:** Bearer Token (Admin/SuperAdmin)

**Query Parameters:**
- `fileUrl` (required): Silinecek dosyanın URL'i

**Örnek Request:**
```bash
curl -X DELETE "https://api.freestays.eu/api/v1/admin/upload?fileUrl=/uploads/images/file.jpg" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Örnek Response:**
```json
{
  "success": true,
  "message": "File deleted successfully"
}
```

### 4. Dosya Validasyonu
**Endpoint:** `POST /api/v1/admin/upload/validate`

**Authorization:** Bearer Token (Admin/SuperAdmin)

**Form Data:**
- `file`: Validate edilecek dosya

**Örnek Response (Valid):**
```json
{
  "valid": true,
  "message": "File is valid",
  "fileName": "image.jpg",
  "size": 123456
}
```

**Örnek Response (Invalid):**
```json
{
  "valid": false,
  "message": "Invalid file extension"
}
```

## Frontend Entegrasyonu

### React/Next.js Örneği

```typescript
// File Upload Component
async function uploadImage(file: File, folder?: string): Promise<string> {
  const formData = new FormData();
  formData.append('file', file);

  const queryParams = folder ? `?folder=${folder}` : '';
  
  const response = await fetch(
    `${API_URL}/api/v1/admin/upload/image${queryParams}`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
      body: formData,
    }
  );

  if (!response.ok) {
    throw new Error('Upload failed');
  }

  const data = await response.json();
  return data.url;
}

// Kullanım
const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
  const file = event.target.files?.[0];
  if (!file) return;

  try {
    const imageUrl = await uploadImage(file, 'featured-destinations');
    console.log('Image uploaded:', imageUrl);
    // imageUrl'i form state'ine kaydet
  } catch (error) {
    console.error('Upload error:', error);
  }
};
```

## Featured Content Kullanımı

### Featured Destination Oluşturma
```typescript
// 1. Önce görseli yükle
const imageUrl = await uploadImage(file, 'featured-destinations');

// 2. Featured destination oluştur
const response = await fetch(`${API_URL}/api/v1/admin/featured-content/destinations`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  },
  body: JSON.stringify({
    destinationId: "123",
    destinationName: "İstanbul",
    countryCode: "TR",
    country: "Türkiye",
    priority: 1,
    status: "Active",
    season: "AllSeason",
    image: imageUrl, // Yüklenen görsel URL'i
    description: "Tarihi ve kültürel zenginlikleriyle...",
    validFrom: "2024-01-01T00:00:00Z",
    validUntil: "2024-12-31T23:59:59Z"
  })
});
```

## Güvenlik ve Limitler

### Rate Limiting
- Global: 100 istek/dakika
- Auth endpoints: 10 istek/dakika

### Dosya Limitleri
**Production:**
- Maksimum dosya boyutu: 5 MB
- İzin verilen formatlar: .jpg, .jpeg, .png, .gif, .webp

**Development:**
- Maksimum dosya boyutu: 10 MB
- İzin verilen formatlar: .jpg, .jpeg, .png, .gif, .webp, .svg

### Request Size Limitleri
- Tekil yükleme: 10 MB
- Çoklu yükleme: 50 MB (toplam)

## Dosya Yapısı

Yüklenen dosyalar aşağıdaki yapıda saklanır:

```
/app/wwwroot/uploads/
  ├── images/              # Genel görseller
  ├── featured-destinations/  # Featured destination görselleri
  ├── featured-hotels/     # Featured hotel görselleri
  ├── logos/               # Logo dosyaları
  └── ...                  # Diğer alt klasörler
```

Her dosya UUID ile unique bir isim alır:
```
a1b2c3d4-e5f6-7890-abcd-ef1234567890.jpg
```

## Hata Yönetimi

### Yaygın Hatalar

**400 Bad Request - Dosya uzantısı izin verilmiyor:**
```json
{
  "message": "File extension not allowed. Allowed extensions: .jpg, .jpeg, .png, .gif, .webp"
}
```

**400 Bad Request - Dosya boyutu fazla:**
```json
{
  "message": "File size exceeds the maximum allowed size of 5 MB"
}
```

**404 Not Found - Dosya bulunamadı:**
```json
{
  "message": "File not found"
}
```

**401 Unauthorized - Token geçersiz:**
```json
{
  "message": "Unauthorized"
}
```

## Best Practices

1. **Önce Validate Edin:** Dosyayı yüklemeden önce `/validate` endpoint'ini kullanarak kontrol edin
2. **Uygun Klasör Seçin:** Dosyaları organize etmek için alt klasörler kullanın
3. **Hata Yönetimi:** Upload işlemlerini try-catch bloklarında yapın
4. **Progress Gösterimi:** Kullanıcıya upload progress gösterin
5. **Optimizasyon:** Büyük dosyaları client-side'da resize edin
6. **Cleanup:** Kullanılmayan dosyaları DELETE endpoint ile silin

## Sorun Giderme

### Volume Mount Kontrolü
Dokploy'da volume mount'un doğru yapılandırıldığından emin olun:
```
Container Path: /app/wwwroot/uploads
```

### Permissions
Container içinde klasör izinlerini kontrol edin:
```bash
ls -la /app/wwwroot/uploads
# Çıktı: drwxrwxrwx ... uploads
```

### Log Kontrol
Upload hatalarını loglardan takip edin:
```bash
docker logs [container-id] | grep -i "upload"
```
