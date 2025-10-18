# Data Collector - Implementation TODO

## Phase 1: MVP - Universe Static Data

### Setup Initial
- [ ] Create .NET 8 solution and projects
  - [ ] EveDataCollector.App (Console with Generic Host)
  - [ ] EveDataCollector.Core (Business logic)
  - [ ] EveDataCollector.Infrastructure (Data + ESI)
  - [ ] EveDataCollector.Shared (DTOs, Config, Constants)
- [ ] Setup NuGet packages
  - [ ] Npgsql (PostgreSQL driver)
  - [ ] Dapper (micro-ORM)
  - [ ] DbUp (migrations)
  - [ ] Polly (resilience)
  - [ ] Serilog (logging)
  - [ ] TickerQ (scheduler)
- [ ] Generate ESI client with NSwag
  - [ ] Download ESI Swagger definition
  - [ ] Create NSwag configuration
  - [ ] Generate client code
  - [ ] Integrate with IHttpClientFactory

### Database Setup
- [ ] Create migration 001: Universe Categories
- [ ] Create migration 002: Universe Groups
- [ ] Create migration 003: Universe Types
- [ ] Create migration 004: Universe Regions
- [ ] Create migration 005: Universe Constellations
- [ ] Create migration 006: Universe Systems
- [ ] Create migration 007: Universe Stations
- [ ] Create DbUp migration runner
- [ ] Setup connection factory with pooling

### Repositories (Dapper)
- [ ] TypeRepository
- [ ] RegionRepository
- [ ] SystemRepository
- [ ] StationRepository

### Collectors
- [ ] UniverseTypesCollectorService
  - [ ] Fetch all type IDs
  - [ ] Bulk fetch type details
  - [ ] Bulk insert with Dapper
- [ ] UniverseRegionsCollectorService
  - [ ] Fetch all regions
  - [ ] Store in database
- [ ] UniverseSystemsCollectorService
  - [ ] Fetch all systems
  - [ ] Bulk insert
- [ ] UniverseStationsCollectorService
  - [ ] Fetch all stations
  - [ ] Bulk insert

### Infrastructure
- [ ] Rate limiting with Polly (300 req/min)
- [ ] Retry policy with exponential backoff
- [ ] Circuit breaker
- [ ] Timeout policies
- [ ] Logging with Serilog (console output)
- [ ] TickerQ scheduler configuration
  - [ ] Schedule universe collectors (weekly)

### Docker
- [ ] Dockerfile (multi-stage build)
- [ ] docker-compose.yml
  - [ ] PostgreSQL service
  - [ ] Collector service
  - [ ] Volume for database
- [ ] Health check endpoint

### Documentation
- [ ] README with setup instructions
- [ ] Configuration examples
- [ ] Docker quick start guide

---

## Phase 2: Wallet, Assets, Orders (SSO)

### SSO Authentication
- [ ] SSOAuthenticationHandler (OAuth2 flow)
- [ ] TokenManager (refresh management)
- [ ] TokenEncryptionService (encrypt tokens in DB)
- [ ] Token refresh scheduler

### Database Migrations
- [ ] Migration 008: Characters & Corporations
- [ ] Migration 009: ESI Tokens
- [ ] Migration 010: Collector Assignments
- [ ] Migration 011: Wallet Tables
- [ ] Migration 012: Assets Tables
- [ ] Migration 013: Asset Diffs
- [ ] Migration 014: Orders Tables

### Token Management
- [ ] EsiTokenRepository
- [ ] CollectorAssignmentRepository
- [ ] TokenManagementService
- [ ] CollectorAssignmentService

### Collectors
- [ ] WalletJournalCollectorService
- [ ] WalletTransactionsCollectorService
- [ ] AssetsCollectorService
- [ ] AssetDiffService (diff calculation)
- [ ] CharacterOrdersCollectorService
- [ ] CorporationOrdersCollectorService

### API (Optional)
- [ ] POST /api/tokens/register
- [ ] GET /api/tokens/callback
- [ ] GET /api/tokens
- [ ] DELETE /api/tokens/{id}
- [ ] PUT /api/tokens/{id}/enable

---

## Phase 3: Public Market Data

### Database
- [ ] Migration 015: Market Tables

### Collectors
- [ ] MarketOrdersCollectorService (multi-region)
- [ ] MarketHistoryCollectorService
- [ ] MarketPricesCollectorService

### Optimization
- [ ] Bulk operations for market orders
- [ ] Consider TimescaleDB for time-series

---

## Phase 4: Industry Data

### Database
- [ ] Migration 016: Industry Tables

### Collectors
- [ ] IndustryIndicesCollectorService
- [ ] IndustryJobsCollectorService

### Monitoring
- [ ] OpenTelemetry setup
- [ ] Prometheus metrics exporter

---

## Phase 5: Production Ready

### Testing
- [ ] Unit tests (xUnit)
- [ ] Integration tests (Testcontainers)
- [ ] E2E tests

### CI/CD
- [ ] GitHub Actions workflow
- [ ] Build + test pipeline
- [ ] Docker image publishing

### Monitoring
- [ ] Grafana dashboards
- [ ] Alerting rules
- [ ] Health checks

### Documentation
- [ ] Complete API documentation
- [ ] Deployment guide
- [ ] Troubleshooting guide
- [ ] Performance tuning guide

---

## Notes

- Each phase should be completed and tested before moving to the next
- Keep PRs small and focused
- Update this TODO as requirements evolve
