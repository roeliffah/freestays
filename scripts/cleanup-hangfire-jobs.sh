#!/bin/bash

# FreeStays - Hangfire Job Cleanup Script
# Bu script takılı kalmış "Running" job'ları temizler
# Docker container'lar içinde çalışır

echo "=== FreeStays Hangfire Job Cleanup ==="
echo ""

# Database credentials - update these if needed
DB_NAME="freestays"
DB_USER="freestays"
DB_PASSWORD="freestays"

# Auto-detect PostgreSQL container
echo "Detecting PostgreSQL container..."
PG_CONTAINER=$(docker ps --filter "name=postgres" --filter "status=running" --format "{{.Names}}" | head -n 1)

if [ -z "$PG_CONTAINER" ]; then
    echo "❌ PostgreSQL container not found. Trying common names..."
    PG_CONTAINER=$(docker ps --filter "status=running" --format "{{.Names}}" | grep -E "postgres|pgsql|db" | head -n 1)
fi

if [ -z "$PG_CONTAINER" ]; then
    echo "❌ ERROR: Could not find running PostgreSQL container"
    echo "Run: docker ps | grep postgres"
    exit 1
fi

echo "✅ Using PostgreSQL container: $PG_CONTAINER"
echo ""

echo "1. Checking stuck Running jobs in database..."
docker exec -i $PG_CONTAINER psql -U $DB_USER -d $DB_NAME << EOF
-- Running job'ları listele
SELECT id, job_type, status, start_time, 
       NOW() - start_time as duration
FROM job_histories 
WHERE status = 'Running' 
ORDER BY start_time DESC;
EOF

echo ""
echo "2. Updating stuck jobs to Failed status..."
docker exec -i $PG_CONTAINER psql -U $DB_USER -d $DB_NAME << EOF
-- 30 dakikadan uzun süre Running olan job'ları Failed yap
UPDATE job_histories 
SET status = 'Failed',
    end_time = NOW(),
    error_message = 'Automatically cancelled - exceeded timeout (cleanup script)',
    updated_at = NOW()
WHERE status = 'Running' 
  AND start_time < NOW() - INTERVAL '30 minutes'
RETURNING id, job_type, start_time, NOW() - start_time as was_running_for;
EOF

echo ""
echo "3. Checking Redis Hangfire keys..."
REDIS_CONTAINER=$(docker ps --filter "name=redis" --filter "status=running" --format "{{.Names}}" | head -n 1)

if [ -z "$REDIS_CONTAINER" ]; then
    echo "⚠️  Redis container not found, skipping Redis cleanup"
else
    echo "✅ Using Redis container: $REDIS_CONTAINER"
    KEY_COUNT=$(docker exec -i $REDIS_CONTAINER redis-cli KEYS "hangfire:*" | wc -l)
    echo "Found $KEY_COUNT Hangfire keys in Redis"
fi

echo ""
echo "=== Cleanup Complete ==="
echo ""
echo "Next steps:"
echo "1. Restart the application: docker restart <app-container>"
echo "2. Check Hangfire Dashboard: /hangfire"
echo "3. DO NOT click 'Trigger Now' - jobs run automatically"
echo ""
echo "To see running containers: docker ps"
