```bash
# EVE Data Collector - Database Setup

## Overview

This directory contains the database schema and migrations for the EVE Data Collector.

## Quick Start

### 1. Start PostgreSQL

```bash
cd data-collector/docker
docker-compose up -d postgres
```

This will:
- Start PostgreSQL 16 Alpine
- Create the `evedata` database
- Run all migrations in `../database/migrations/` automatically
- Expose PostgreSQL on port 5432

### 2. Verify Setup

```bash
cd data-collector/scripts
./test-db-connection.sh
```

This will check:
- Database connectivity
- Tables created
- Row counts

### 3. Access Database

#### Using psql (via Docker)

```bash
./scripts/db-manage.sh psql
```

#### Using pgAdmin (Web UI)

```bash
./scripts/db-manage.sh pgadmin
```

Then open http://localhost:5050
- Email: `admin@eveplanner.local`
- Password: `admin`

#### Using your favorite client

```
Host: localhost
Port: 5432
Database: evedata
User: eveuser
Password: changeme
```

## Database Management Script

The `db-manage.sh` script provides convenient commands:

```bash
./scripts/db-manage.sh start      # Start PostgreSQL
./scripts/db-manage.sh stop       # Stop PostgreSQL
./scripts/db-manage.sh restart    # Restart PostgreSQL
./scripts/db-manage.sh logs       # Show logs
./scripts/db-manage.sh reset      # Reset database (delete all data)
./scripts/db-manage.sh psql       # Connect with psql
./scripts/db-manage.sh backup     # Create backup
./scripts/db-manage.sh restore <file>  # Restore from backup
./scripts/db-manage.sh pgadmin    # Start pgAdmin UI
```

## Migrations

### Phase 1: Universe Static Data

Migrations are executed in order during PostgreSQL initialization:

1. **001_Universe_Categories.sql** - Item categories
2. **002_Universe_Groups.sql** - Item groups
3. **003_Universe_Types.sql** - Item types (all items)
4. **004_Universe_Regions.sql** - Regions
5. **005_Universe_Constellations.sql** - Constellations
6. **006_Universe_Systems.sql** - Solar systems
7. **007_Universe_Stations.sql** - NPC stations

### Future Phases

- **Phase 2**: Wallet, Assets, Orders (migrations 008-014)
- **Phase 3**: Market Data (migration 015)
- **Phase 4**: Industry Data (migration 016)

## Schema Overview

```
categories
  └─ groups
      └─ types

regions
  └─ constellations
      └─ systems
          └─ stations
```

### Key Tables

**types** - All EVE items (~35,000 items)
- Primary data for market analysis
- Includes volume, mass, capacity
- Full-text search support

**systems** - All solar systems (~8,000 systems)
- Security status for filtering
- Position data for distance calculations

**stations** - NPC stations (~4,500 stations)
- Available services array
- Market hub identification

## Connection String

For .NET applications:

```csharp
"ConnectionStrings": {
  "Default": "Host=localhost;Database=evedata;Username=eveuser;Password=changeme"
}
```

For environment variables:

```bash
DATABASE_URL=postgresql://eveuser:changeme@localhost:5432/evedata
```

## Troubleshooting

### PostgreSQL won't start

```bash
# Check logs
docker-compose -f docker/docker-compose.yml logs postgres

# Check if port 5432 is already in use
lsof -i :5432

# Reset everything
./scripts/db-manage.sh reset
```

### Migrations didn't run

Migrations only run on first initialization. To re-run:

```bash
# Stop and remove volumes
docker-compose -f docker/docker-compose.yml down -v

# Start again (migrations will run)
docker-compose -f docker/docker-compose.yml up -d postgres
```

### Connection refused

```bash
# Wait for PostgreSQL to be ready
docker-compose -f docker/docker-compose.yml ps

# Check health status
docker inspect eve-planner-db | grep -A 10 Health
```

### Permission denied

```bash
# Make scripts executable
chmod +x scripts/*.sh
```

## Backup and Restore

### Create Backup

```bash
./scripts/db-manage.sh backup
# Creates: backup_YYYYMMDD_HHMMSS.sql
```

### Restore from Backup

```bash
./scripts/db-manage.sh restore backup_20250118_120000.sql
```

### Manual Backup

```bash
docker-compose -f docker/docker-compose.yml exec -T postgres \
  pg_dump -U eveuser evedata > backup.sql
```

### Manual Restore

```bash
docker-compose -f docker/docker-compose.yml exec -T postgres \
  psql -U eveuser -d evedata < backup.sql
```

## Performance Tips

### Indexes

All critical columns have indexes:
- Primary keys (automatic)
- Foreign keys
- Frequently queried columns (name, published, etc.)
- Full-text search on item names

### Connection Pooling

PostgreSQL is configured for connection pooling:
- Max connections: 100 (default)
- Use connection pooling in your application (recommended)

### Query Optimization

```sql
-- Use EXPLAIN ANALYZE to check query performance
EXPLAIN ANALYZE SELECT * FROM types WHERE name ILIKE '%Tritanium%';

-- Check index usage
SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;
```

## Security

### Default Credentials

**⚠️ Change default passwords in production!**

Edit `docker/.env`:
```bash
POSTGRES_PASSWORD=your_strong_password_here
```

### Network

By default, PostgreSQL is exposed on `localhost:5432`. For production:
- Remove port mapping in `docker-compose.yml`
- Use Docker network only
- Enable SSL/TLS

## Extensions

### pg_trgm (Trigram)

Used for fuzzy text search on item names:

```sql
-- Already enabled in migration 003
SELECT * FROM types WHERE name % 'Tritaium';  -- Fuzzy match
```

### Future Extensions

For Phase 2+:
- `uuid-ossp` - UUID generation
- `pg_stat_statements` - Query statistics
- `timescaledb` - Time-series optimization (optional)

## Monitoring

### Database Size

```sql
SELECT pg_size_pretty(pg_database_size('evedata'));
```

### Table Sizes

```sql
SELECT
  schemaname,
  tablename,
  pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Active Connections

```sql
SELECT count(*) FROM pg_stat_activity WHERE datname = 'evedata';
```

## References

- [PostgreSQL 16 Documentation](https://www.postgresql.org/docs/16/)
- [Docker PostgreSQL](https://hub.docker.com/_/postgres)
- [ESI Database](https://docs.esi.evetech.net/)
