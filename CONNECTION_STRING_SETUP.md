# Connection String Yapılandırması - Dokploy Ortamı

## 🔴 Mevcut Durum (İP Adresi Kullanıyor)
```
Host=3.72.175.63;Port=4848;Database=freestays;Username=usrarvas;Password=...
```

**Sorun:** Dış IP adresi kullanıldığında, Dokploy container'ları arasında network trafiği uzak bir yoldan geçiyor.

---

## ✅ Dokploy Üzerindeki Doğru Ayarlama

Eğer PostgreSQL de Dokploy'da containerized hale getirilirse:

### **Seçenek 1: Container Adı Kullanma (Önerilen)**
```
Host=<postgresql-container-adı>;Port=5432;Database=freestays;Username=freestays;Password=...
```

**Avantajları:**
- ✅ Daha hızlı (lokal network)
- ✅ DNS resolution otomatik (Dokploy tarafından)
- ✅ Daha güvenli (dış IP maruz kalmaz)

### **Seçenek 2: Environment Variable Kullanma**
docker-compose.yml veya Dokploy UI'de:
```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=freestays;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
```

Dokploy'da environment variables set edin:
- `POSTGRES_HOST` = PostgreSQL container adı (örn: `postgres-db`)
- `POSTGRES_PORT` = `5432`
- `POSTGRES_USER` = `freestays`
- `POSTGRES_PASSWORD` = şifre

---

## 🎯 Dokploy UI Adımları

1. **Application Settings** → **Environment Variables** bölümüne git
2. `ConnectionStrings__DefaultConnection` öğesi için:
   ```
   Host=<postgres-container-name>;Port=5432;Database=freestays;Username=freestays;Password=<şifre>
   ```
3. Save & Deploy

**NOT:** PostgreSQL container adını öğrenmek için:
```bash
# Dokploy sunucusunda:
docker ps | grep postgres
```

---

## 📝 Şu An Yapılandırması

**docker-compose.yml:**
- PostgreSQL: `Host=3.72.175.63:4848` (Dış IP)
- Redis: `freestays-cachedb-aucb6o:6379` (Container adı) ✅

**Önerilen Değişiklik:**
İkisini de container adları ile tutarlı hale getir.

---

## 🔗 İlişkili Dosyalar
- [docker-compose.yml](docker-compose.yml)
- [appsettings.json](src/FreeStays.API/appsettings.json)
- [Program.cs](src/FreeStays.API/Program.cs)
