# JWT Token Expired - Login Problemi Çözümü

## 🐛 Sorun

Frontend'den login olmaya çalışırken şu hata alınıyordu:

```
[13:58:39 ERR] JWT Authentication failed
Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException: IDX10223: Lifetime validation failed. 
The token is expired. ValidTo (UTC): '27.12.2025 03:58:47', Current time (UTC): '27.12.2025 10:58:39'.
```

### Sorunun Nedeni

JWT authentication middleware, `[AllowAnonymous]` attribute'a sahip endpoint'lerde bile HTTP header'da token varsa onu validate etmeye çalışıyordu. Frontend'den eski/expired bir token ile login endpoint'ine istek atıldığında, middleware authentication'ı başarısız sayıyor ve endpoint'e ulaşmaya izin vermiyordu.

## ✅ Çözüm

`Program.cs` dosyasındaki JWT Events yapılandırmasını güncelledik:

### 1. `OnAuthenticationFailed` Event'i

```csharp
OnAuthenticationFailed = context =>
{
    // AllowAnonymous endpoint'lerde expired token hatalarını ignore et
    var endpoint = context.HttpContext.GetEndpoint();
    var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;
    
    if (allowAnonymous && context.Exception is SecurityTokenExpiredException)
    {
        Log.Warning("Expired token on AllowAnonymous endpoint: {Path}", context.HttpContext.Request.Path);
        context.Response.Headers.Append("Token-Expired", "true");
        // AllowAnonymous endpoint için authentication'ı başarılı say
        context.NoResult();
        return Task.CompletedTask;
    }
    
    Log.Error(context.Exception, "JWT Authentication failed");
    if (context.Exception is SecurityTokenExpiredException)
    {
        context.Response.Headers.Append("Token-Expired", "true");
    }
    return Task.CompletedTask;
}
```

**Açıklama:**
- Endpoint'in `[AllowAnonymous]` olup olmadığını kontrol eder
- Eğer AllowAnonymous ise ve token expired ise, `context.NoResult()` ile authentication'ı bypass eder
- Frontend için "Token-Expired" header'ı ekler (opsiyonel bilgi)

### 2. `OnChallenge` Event'i

```csharp
OnChallenge = context =>
{
    // AllowAnonymous endpoint'lerde challenge'ı bypass et
    var endpoint = context.HttpContext.GetEndpoint();
    var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;
    
    if (allowAnonymous)
    {
        Log.Information("Challenge bypassed for AllowAnonymous endpoint: {Path}", context.HttpContext.Request.Path);
        context.HandleResponse();
        return Task.CompletedTask;
    }
    
    Log.Warning("JWT Authentication challenge: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
    return Task.CompletedTask;
}
```

**Açıklama:**
- AllowAnonymous endpoint'lerde authentication challenge'ı bypass eder
- `context.HandleResponse()` ile response handling'i middleware'e bırakır

### 3. Gerekli Using Statement

```csharp
using Microsoft.AspNetCore.Authorization; // IAllowAnonymous için gerekli
```

## 🎯 Sonuç

Artık frontend'den:
- ✅ Eski/expired token ile login yapılabilir
- ✅ Token olmadan login yapılabilir
- ✅ `[AllowAnonymous]` endpoint'ler token validation'dan muaf
- ✅ Korumalı endpoint'ler hala normal şekilde authenticate ediliyor

## 📝 Test

### Login Endpoint'i Test Etme

```bash
# Token olmadan login
curl -X POST https://localhost:7001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'

# Expired token ile login (header'da eski token)
curl -X POST https://localhost:7001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <expired_token>" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

Her iki durumda da başarılı response alınmalı.

## 🔒 Güvenlik Notu

Bu değişiklik **sadece** `[AllowAnonymous]` attribute'una sahip endpoint'leri etkiler:
- `/api/v1/auth/login`
- `/api/v1/auth/register`
- `/api/v1/public/*` (yeni eklenen public endpoint'ler)
- Diğer AllowAnonymous endpoint'ler

Korumalı endpoint'ler (`[Authorize]` attribute'una sahip) **tam güvenlik kontrolü ile çalışmaya devam eder**.

## 📌 İlgili Dosyalar

- [Program.cs](src/FreeStays.API/Program.cs) - JWT Events yapılandırması

## 🚀 Frontend İçin Öneriler

1. **LocalStorage Temizleme:** Kullanıcı logout olduğunda token'ı localSt orage'dan temizle
2. **Token Refresh:** Refresh token ile otomatik token yenileme implementasyonu
3. **Error Handling:** "Token-Expired" header'ını kontrol et ve kullanıcıya bilgi ver
4. **Request Interceptor:** Axios/Fetch interceptor ile expired token'ları request'ten önce temizle

```javascript
// Örnek Axios Interceptor
axios.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    const expiresAt = localStorage.getItem('token_expires_at');
    
    // Token expired mı kontrol et
    if (token && expiresAt && new Date(expiresAt) < new Date()) {
      // Token expired, header'a ekleme
      localStorage.removeItem('token');
      localStorage.removeItem('token_expires_at');
    } else if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    return config;
  },
  (error) => Promise.reject(error)
);
```
