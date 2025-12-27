# Manual Hangfire Cleanup Commands

Sunucuda çalıştırılacak komutlar (Docker container'lar için)

## 1. Container'ları Listele

```bash
docker ps
```

PostgreSQL ve Redis container isimlerini not al.

## 2. PostgreSQL - Running Job'ları Listele

```bash
# PostgreSQL container'ına bağlan (container_name'i değiştir)
docker exec -it <postgres_container_name> psql -U freestays -d freestays

# Veya tek komutla:
docker exec -i <postgres_container_name> psql -U freestays -d freestays -c "
SELECT id, job_type, status, start_time, 
       NOW() - start_time as duration
FROM job_histories 
WHERE status = 'Running' 
ORDER BY start_time DESC;
"
```

## 3. Takılı Job'ları Temizle

```bash
docker exec -i <postgres_container_name> psql -U freestays -d freestays -c "
UPDATE job_histories 
SET status = 'Failed',
    end_time = NOW(),
    error_message = 'Cancelled - multiple concurrent instances',
    updated_at = NOW()
WHERE status = 'Running' 
  AND start_time < NOW() - INTERVAL '30 minutes'
RETURNING id, job_type;
"
```

## 4. Tüm Running Job'ları Temizle (Dikkatli!)

```bash
docker exec -i <postgres_container_name> psql -U freestays -d freestays -c "
UPDATE job_histories 
SET status = 'Failed',
    end_time = NOW(),
    error_message = 'Manual cleanup - concurrent execution fix applied',
    updated_at = NOW()
WHERE status = 'Running'
RETURNING id, job_type, start_time;
"
```

## 5. Redis Hangfire Keys

```bash
# Redis'e bağlan
docker exec -it <redis_container_name> redis-cli

# Hangfire key'lerini listele
KEYS hangfire:*

# Key sayısını öğren
KEYS hangfire:* | wc -l

# Tüm recurring job'ları göster
HGETALL hangfire:recurring-jobs

# Çıkış
exit
```

## 6. Application'ı Restart Et

```bash
# FreeStays API container'ını restart et
docker restart <freestays_api_container_name>

# Veya docker-compose kullanıyorsan
docker-compose restart
```

## Hızlı Temizleme (Tek Komut)

Container ismini öğrendikten sonra:

```bash
# Örnek - container_name değiştir
PG_CONTAINER="postgres-container"

docker exec -i $PG_CONTAINER psql -U freestays -d freestays << 'EOF'
UPDATE job_histories 
SET status = 'Failed',
    end_time = NOW(),
    error_message = 'Cleanup - concurrent execution fix applied'
WHERE status = 'Running';
EOF
```

## Container İsimlerini Otomatik Bul

```bash
# PostgreSQL
PG_CONTAINER=$(docker ps --filter "name=postgres" --format "{{.Names}}" | head -n 1)
echo "PostgreSQL: $PG_CONTAINER"

# Redis
REDIS_CONTAINER=$(docker ps --filter "name=redis" --format "{{.Names}}" | head -n 1)
echo "Redis: $REDIS_CONTAINER"

# FreeStays API
API_CONTAINER=$(docker ps --filter "name=freestays" --format "{{.Names}}" | head -n 1)
echo "API: $API_CONTAINER"
```
