#!/bin/bash
# Database management script

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCKER_DIR="$SCRIPT_DIR/../docker"

cd "$DOCKER_DIR"

case "$1" in
    start)
        echo -e "${YELLOW}Starting PostgreSQL...${NC}"
        docker-compose up -d postgres
        echo -e "${GREEN}✓ PostgreSQL started${NC}"
        echo "Connection string: postgresql://eveuser:changeme@localhost:5432/evedata"
        ;;

    stop)
        echo -e "${YELLOW}Stopping PostgreSQL...${NC}"
        docker-compose down
        echo -e "${GREEN}✓ PostgreSQL stopped${NC}"
        ;;

    restart)
        echo -e "${YELLOW}Restarting PostgreSQL...${NC}"
        docker-compose restart postgres
        echo -e "${GREEN}✓ PostgreSQL restarted${NC}"
        ;;

    logs)
        docker-compose logs -f postgres
        ;;

    reset)
        echo -e "${YELLOW}⚠ This will delete all data!${NC}"
        read -p "Are you sure? (yes/no): " -r
        if [[ $REPLY == "yes" ]]; then
            echo "Stopping and removing volumes..."
            docker-compose down -v
            echo "Starting fresh PostgreSQL..."
            docker-compose up -d postgres
            echo -e "${GREEN}✓ Database reset complete${NC}"
        else
            echo "Aborted."
        fi
        ;;

    psql)
        echo "Connecting to PostgreSQL..."
        docker-compose exec postgres psql -U eveuser -d evedata
        ;;

    backup)
        BACKUP_FILE="backup_$(date +%Y%m%d_%H%M%S).sql"
        echo -e "${YELLOW}Creating backup: $BACKUP_FILE${NC}"
        docker-compose exec -T postgres pg_dump -U eveuser evedata > "$BACKUP_FILE"
        echo -e "${GREEN}✓ Backup created: $BACKUP_FILE${NC}"
        ;;

    restore)
        if [ -z "$2" ]; then
            echo "Usage: $0 restore <backup_file.sql>"
            exit 1
        fi
        echo -e "${YELLOW}Restoring from: $2${NC}"
        docker-compose exec -T postgres psql -U eveuser -d evedata < "$2"
        echo -e "${GREEN}✓ Restore complete${NC}"
        ;;

    pgadmin)
        echo -e "${YELLOW}Starting pgAdmin...${NC}"
        docker-compose --profile tools up -d pgadmin
        echo -e "${GREEN}✓ pgAdmin started at http://localhost:5050${NC}"
        echo "Email: admin@eveplanner.local"
        echo "Password: admin"
        ;;

    *)
        echo "EVE Planner - Database Management"
        echo ""
        echo "Usage: $0 {command}"
        echo ""
        echo "Commands:"
        echo "  start      Start PostgreSQL"
        echo "  stop       Stop PostgreSQL"
        echo "  restart    Restart PostgreSQL"
        echo "  logs       Show PostgreSQL logs"
        echo "  reset      Reset database (delete all data)"
        echo "  psql       Connect to PostgreSQL with psql"
        echo "  backup     Create database backup"
        echo "  restore    Restore from backup file"
        echo "  pgadmin    Start pgAdmin UI"
        echo ""
        exit 1
        ;;
esac
