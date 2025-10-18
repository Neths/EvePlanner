# EVE Data Collector - .NET 8 Application

## Overview

Console application pour collecter les données d'EVE Online via l'API ESI et les stocker dans PostgreSQL.

## Architecture

```
EveDataCollector.sln
├── EveDataCollector.App/           # Console application (entry point)
│   ├── Program.cs                   # Generic Host + DI setup
│   ├── appsettings.json             # Configuration
│   └── appsettings.Development.json # Dev configuration
├── EveDataCollector.Core/           # Domain models & interfaces
├── EveDataCollector.Infrastructure/ # Data access & ESI client
│   └── ESI/
│       └── EsiClient.cs             # Manual typed ESI client
└── EveDataCollector.Shared/         # Shared utilities
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL 16 (running via Docker)
- ESI API access (public endpoints)

## Configuration

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=evedata;Username=eveuser;Password=changeme"
  },
  "EsiClient": {
    "BaseUrl": "https://esi.evetech.net/latest",
    "UserAgent": "EveDataCollector/0.1.0",
    "Timeout": 30,
    "MaxRetries": 3
  }
}
```

## Running the Application

### 1. Start PostgreSQL

```bash
cd ../docker
docker-compose up -d postgres
```

### 2. Run the application

```bash
cd src/EveDataCollector.App
dotnet run
```

Expected output:
```
[16:42:25 INF] Starting EVE Data Collector...
[16:42:25 INF] Database connection successful: evedata
[16:42:25 INF] ESI client test successful: Found 47 categories
[16:42:25 INF] All systems operational. Press Ctrl+C to exit.
```

## Features

### ✅ Implemented

- Generic Host with Dependency Injection
- Structured logging with Serilog
- PostgreSQL connection via Npgsql
- ESI client with typed DTOs
- Polly retry policies for HTTP calls
- Configuration management
- Database connection test
- ESI API integration test

### Universe Endpoints Supported

- `GET /universe/categories/` - List all categories
- `GET /universe/categories/{id}/` - Get category details
- `GET /universe/groups/` - List all groups
- `GET /universe/groups/{id}/` - Get group details
- `GET /universe/types/{id}/` - Get type details
- `GET /universe/regions/` - List all regions
- `GET /universe/regions/{id}/` - Get region details
- `GET /universe/constellations/{id}/` - Get constellation details
- `GET /universe/systems/{id}/` - Get system details
- `GET /universe/stations/{id}/` - Get station details

## Building

```bash
dotnet build
```

## Testing

The application includes built-in tests on startup:
1. **Database Test**: Connects to PostgreSQL and verifies connection
2. **ESI Test**: Calls ESI API to fetch categories list

## Technologies

- **.NET 8** - Runtime & SDK
- **Microsoft.Extensions.Hosting** - Generic Host & DI
- **Serilog** - Structured logging
- **Npgsql 9.0** - PostgreSQL driver
- **Dapper 2.1** - Micro-ORM (ready to use)
- **Polly 8.6** - Resilience & retry policies
- **System.Text.Json** - JSON serialization

## Next Steps

Phase 1 MVP remaining tasks:
1. Implement Universe data collectors
2. Implement Dapper repositories with bulk insert
3. Create scheduled jobs with TickerQ
4. Add data validation and error handling

## Troubleshooting

### Connection refused to PostgreSQL

```bash
# Check if PostgreSQL is running
docker-compose -f ../docker/docker-compose.yml ps

# Check logs
docker-compose -f ../docker/docker-compose.yml logs postgres
```

### ESI API timeout

Check `EsiClient.Timeout` in appsettings.json and increase if needed.

### Build errors

```bash
# Clean and rebuild
dotnet clean
dotnet build
```
