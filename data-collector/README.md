# EVE Data Collector

Autonomous data collector for EVE Online ESI API with PostgreSQL storage.

## Overview

This data collector automatically gathers EVE Online data through the official ESI API:
- Static universe data (items, systems, regions)
- Character and corporation data (wallet, assets, orders) via SSO
- Public market data (orders, history, prices)
- Industry data (indices, jobs)

All data is stored in PostgreSQL for analysis, dashboards, and custom tools.

## Quick Start

### Prerequisites

- .NET 8 SDK
- PostgreSQL 16
- Docker & Docker Compose (optional)

### Using Docker Compose (Recommended)

```bash
# Start PostgreSQL and the collector
docker-compose up -d

# View logs
docker-compose logs -f collector

# Stop services
docker-compose down
```

### Manual Setup

1. **Setup PostgreSQL**
```bash
# Create database
createdb evedata

# Set connection string
export DATABASE_URL="Host=localhost;Database=evedata;Username=postgres;Password=yourpassword"
```

2. **Build and run**
```bash
# Restore dependencies
dotnet restore

# Run migrations
dotnet run --project src/EveDataCollector.App -- migrate

# Start collector
dotnet run --project src/EveDataCollector.App
```

## Configuration

Configuration is done via `appsettings.json` or environment variables:

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=evedata;Username=postgres"
  },
  "ESI": {
    "UserAgent": "eve-data-collector/1.0 (your@email.com)",
    "RateLimit": 300,
    "ClientId": "your_client_id",
    "ClientSecret": "your_client_secret"
  },
  "Collection": {
    "MarketRegions": [10000002, 10000043, 10000032],
    "MarketOrdersInterval": "5m",
    "AssetsInterval": "1h"
  }
}
```

### Environment Variables

- `DATABASE_URL` - PostgreSQL connection string
- `ESI_USER_AGENT` - User agent for ESI requests
- `ESI_CLIENT_ID` - EVE SSO Client ID (for authenticated endpoints)
- `ESI_CLIENT_SECRET` - EVE SSO Client Secret
- `MARKET_REGIONS` - Comma-separated region IDs to collect
- `LOG_LEVEL` - Logging level (Debug, Information, Warning, Error)

## Architecture

See [PLAN.md](./PLAN.md) for complete architecture documentation.

### Project Structure

```
data-collector/
├── src/
│   ├── EveDataCollector.App/          # Console application
│   ├── EveDataCollector.Core/         # Business logic
│   ├── EveDataCollector.Infrastructure/  # Data access + ESI
│   └── EveDataCollector.Shared/       # DTOs, constants
├── tests/
│   ├── EveDataCollector.UnitTests/
│   └── EveDataCollector.IntegrationTests/
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
└── scripts/
    ├── generate-esi-client.sh
    └── run-migrations.sh
```

## Development Phases

- ✅ **Phase 1 (MVP):** Universe static data collection
- 🚧 **Phase 2:** Wallet, Assets, Orders (SSO required)
- ⏳ **Phase 3:** Public market data
- ⏳ **Phase 4:** Industry data
- ⏳ **Phase 5:** Production ready (tests, CI/CD, monitoring)

## SSO Authentication Setup

For Phase 2+ (character/corporation data), you need to setup EVE SSO:

1. Create an application at https://developers.eveonline.com/
2. Add required scopes:
   - `esi-wallet.read_character_wallet.v1`
   - `esi-assets.read_assets.v1`
   - `esi-markets.read_character_orders.v1`
   - etc.
3. Configure callback URL: `http://localhost:8080/callback`
4. Add Client ID and Secret to configuration

## Token Management

### Adding a Character Token

```bash
# Start the token registration flow
curl -X POST http://localhost:8080/api/tokens/register

# Follow OAuth flow in browser
# Token will be automatically stored and refreshed
```

### Managing Tokens

```bash
# List all tokens
curl http://localhost:8080/api/tokens

# Disable a token
curl -X PUT http://localhost:8080/api/tokens/{id}/disable

# Remove a token
curl -X DELETE http://localhost:8080/api/tokens/{id}
```

## Database Schema

See [PLAN.md](./PLAN.md#4-schéma-de-base-de-données) for complete schema documentation.

### Key Tables

**Phase 1 - Universe:**
- `categories`, `groups`, `types`
- `regions`, `constellations`, `systems`, `stations`

**Phase 2 - Character/Corp:**
- `characters`, `corporations`
- `esi_tokens`, `collector_assignments`
- `wallet_journal`, `wallet_transactions`
- `assets`, `asset_diffs`
- `character_orders`, `corporation_orders`

**Phase 3 - Market:**
- `market_orders_public`
- `market_history`

**Phase 4 - Industry:**
- `industry_indices`
- `industry_jobs`

## Monitoring

Metrics are exposed on `/metrics` (port 9090) for Prometheus:

- `esi_requests_total` - Total ESI requests
- `esi_request_duration_seconds` - Request duration
- `collector_last_run_timestamp` - Last collection time
- `collector_items_processed` - Items processed per run

## Troubleshooting

### Database Connection Issues

```bash
# Test connection
psql $DATABASE_URL -c "SELECT 1"

# Check migrations
dotnet run --project src/EveDataCollector.App -- migrate --status
```

### ESI Rate Limiting

If you hit rate limits (429 errors):
- Reduce collection frequency in config
- Check `ESI_RATE_LIMIT` setting (default: 300 req/min)

### Token Issues

```bash
# Check token expiry
dotnet run --project src/EveDataCollector.App -- tokens list

# Force refresh
dotnet run --project src/EveDataCollector.App -- tokens refresh
```

## Contributing

This is a personal project. Feel free to fork and adapt for your needs.

## Resources

- [Full Planning Document](./PLAN.md)
- [ESI Documentation](https://docs.esi.evetech.net/)
- [EVE SSO Guide](https://developers.eveonline.com/blog/article/sso-to-authenticated-calls)

## License

GNU GPL v3.0 - See LICENSE file for details.
