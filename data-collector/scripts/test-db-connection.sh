#!/bin/bash
# Test database connection and verify migrations

set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
DB_HOST="${POSTGRES_HOST:-localhost}"
DB_PORT="${POSTGRES_PORT:-5432}"
DB_NAME="${POSTGRES_DB:-evedata}"
DB_USER="${POSTGRES_USER:-eveuser}"
DB_PASSWORD="${POSTGRES_PASSWORD:-changeme}"

echo -e "${YELLOW}Testing database connection...${NC}"
echo "Host: $DB_HOST"
echo "Port: $DB_PORT"
echo "Database: $DB_NAME"
echo "User: $DB_USER"
echo ""

# Test connection
if PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c '\q' 2>/dev/null; then
    echo -e "${GREEN}✓ Database connection successful${NC}"
else
    echo -e "${RED}✗ Database connection failed${NC}"
    echo ""
    echo "Make sure PostgreSQL is running:"
    echo "  cd data-collector/docker"
    echo "  docker-compose up -d postgres"
    exit 1
fi

echo ""
echo -e "${YELLOW}Checking database tables...${NC}"

# List tables
TABLES=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename;")

if [ -z "$TABLES" ]; then
    echo -e "${YELLOW}⚠ No tables found${NC}"
    echo "Run migrations first:"
    echo "  cd data-collector/docker"
    echo "  docker-compose down -v  # Clean volumes"
    echo "  docker-compose up -d postgres  # Migrations will run automatically"
else
    echo -e "${GREEN}✓ Tables found:${NC}"
    echo "$TABLES" | while read -r table; do
        if [ ! -z "$table" ]; then
            COUNT=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM $table;")
            echo "  - $table ($COUNT rows)"
        fi
    done
fi

echo ""
echo -e "${YELLOW}Checking database version...${NC}"
VERSION=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT version();")
echo "$VERSION"

echo ""
echo -e "${GREEN}✓ All checks completed${NC}"
