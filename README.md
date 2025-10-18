# EVE Planner

EVE Online data collection and planning tools.

## Projects

### Data Collector

Autonomous data collector for EVE Online via ESI (EVE Swagger Interface), deployable with Docker, with PostgreSQL database storage for custom dashboards and analysis tools.

**Stack:** .NET 8, Dapper, PostgreSQL, NSwag, TickerQ

[📖 Full Documentation](./data-collector/PLAN.md)

#### Key Features

- **Static Universe Data Collection** (Phase 1)
  - Items, types, groups, categories
  - Regions, constellations, systems, stations
  - Bulk import with weekly updates

- **Character & Corporation Data** (Phase 2)
  - Wallet (journal + transactions)
  - Assets with diff tracking
  - Market orders
  - SSO authentication with automatic token refresh

- **Public Market Data** (Phase 3)
  - Market orders by region
  - Historical prices
  - Price aggregates

- **Industry Data** (Phase 4)
  - Industry indices
  - Industry jobs (character + corp)

#### Quick Start

```bash
cd data-collector

# Using Docker Compose
docker-compose up -d

# Or build and run manually
dotnet build
dotnet run --project src/EveDataCollector.App
```

See [data-collector/README.md](./data-collector/README.md) for detailed setup instructions.

## Project Status

- ✅ **Planning Phase** - Complete architecture and technical design
- 🚧 **Phase 1 (MVP)** - Universe static data collection (In Progress)
- ⏳ **Phase 2** - Wallet, Assets, Orders with SSO
- ⏳ **Phase 3** - Public market data
- ⏳ **Phase 4** - Industry data
- ⏳ **Phase 5** - Production ready

## Architecture

```
EVE ESI API (CCP)
        ↓
Data Collector (.NET 8 Console App)
├── ESI Client (NSwag Generated)
├── HTTP Resilience (Polly)
├── Collector Services
├── Scheduler (TickerQ)
└── Data Layer (Dapper + Npgsql)
        ↓
PostgreSQL Database
```

## Technology Stack

- **Runtime:** .NET 8
- **Database:** PostgreSQL 16 (with optional TimescaleDB)
- **ORM:** Dapper (micro-ORM)
- **ESI Client:** NSwag (auto-generated from Swagger)
- **Migrations:** DbUp
- **Scheduler:** TickerQ
- **Resilience:** Polly
- **Logging:** Serilog
- **Monitoring:** OpenTelemetry + Prometheus

## Contributing

This is a personal project for EVE Online planning and analysis. Feel free to fork and adapt for your own needs.

## Resources

- **ESI Documentation:** https://docs.esi.evetech.net/
- **ESI Swagger UI:** https://esi.evetech.net/ui/
- **EVE Developers:** https://developers.eveonline.com/

## License

GNU General Public License v3.0 - See LICENSE file for details.

## Disclaimer

This project is not affiliated with or endorsed by CCP Games. EVE Online and the EVE logo are trademarks of CCP hf.
