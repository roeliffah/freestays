"""PostgreSQL database configuration and queries"""
import asyncpg
import logging
import os

logger = logging.getLogger(__name__)

# PostgreSQL connection details from environment variables
PG_HOST = os.environ.get('PG_HOST', '')
PG_PORT = int(os.environ.get('PG_PORT', '5432'))
PG_USER = os.environ.get('PG_USER', '')
PG_PASSWORD = os.environ.get('PG_PASSWORD', '')
PG_DATABASE = os.environ.get('PG_DATABASE', '')

async def search_destinations_postgres(query: str):
    """Search destinations from PostgreSQL sunhotels_destinations_cache"""
    try:
        conn = await asyncpg.connect(
            host=PG_HOST,
            port=PG_PORT,
            user=PG_USER,
            password=PG_PASSWORD,
            database=PG_DATABASE
        )
        
        try:
            results = await conn.fetch("""
                SELECT 
                    "DestinationId" as destination_id,
                    "Name" as name,
                    "Country" as country,
                    "CountryId" as country_id,
                    "CountryCode" as country_code,
                    "DestinationCode" as destination_code
                FROM sunhotels_destinations_cache
                WHERE LOWER("Name") LIKE $1 
                   OR LOWER("Country") LIKE $1
                LIMIT 10
            """, f"%{query.lower()}%")
            
            destinations = []
            for row in results:
                destinations.append({
                    "id": str(row["destination_id"]),
                    "name": row["name"],
                    "country": row["country"],
                    "country_id": str(row["country_id"] or ""),
                    "city_id": str(row["destination_id"]),  # Use destination_id as city_id
                    "resort_id": "",  # Optional
                    "type": "city",
                    "display": f"{row['name']}, {row['country']}"
                })
            
            logger.info(f"✅ PostgreSQL: Found {len(destinations)} destinations for '{query}'")
            return destinations
            
        finally:
            await conn.close()
            
    except Exception as e:
        logger.error(f"PostgreSQL error: {str(e)}")
        return []
