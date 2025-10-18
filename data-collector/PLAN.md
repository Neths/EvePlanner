# Plan de Définition - Collecteur de Données EVE Online ESI

## Vue d'ensemble

Collecteur autonome de données EVE Online via l'API ESI (EVE Swagger Interface), déployable en Docker, avec stockage en base de données pour alimenter des dashboards et outils d'analyse personnalisés.

---

## 1. Objectifs

### Objectifs Principaux (par ordre de priorité)
1. ✅ **Collecter les données statiques** (univers, items, régions, systèmes)
2. ✅ **Collecter les données personnelles et corporation** (wallet, assets, orders)
   - 🔐 Authentification SSO EVE Online requise
3. ✅ **Collecter les données de marché publiques** (orders, history, prices)
4. ✅ **Collecter les données industrielles** (indices, jobs)
5. ✅ **Stocker les données** dans une base de données PostgreSQL
6. ✅ **Déploiement autonome** via Docker
7. ✅ **Configuration flexible** via variables d'environnement
8. ✅ **Gestion automatique des rate limits ESI** (300 req/min)
9. ✅ **Retry et résilience** en cas d'erreur

### Objectifs Secondaires
- 📊 Métriques et monitoring (OpenTelemetry + Prometheus)
- 🔄 Mise à jour incrémentale efficace
- 🔍 Support multi-character et multi-corporation
- 📈 API REST pour interroger les données collectées (optionnel)

---

## 2. Architecture Technique

### Stack Technologique

**Stack .NET (Sélectionnée)**
- **Langage** : C# / .NET 8+
- **Type de projet** : Console Application avec hosting générique
- **Client ESI** : Généré automatiquement via [NSwag](https://github.com/RicoSuter/NSwag) depuis la définition Swagger ESI
- **Base de données** : PostgreSQL 16
- **ORM** : [Dapper](https://github.com/DapperLib/Dapper) (micro-ORM performant)
- **Migrations** : [DbUp](https://dbup.readthedocs.io/) ou [FluentMigrator](https://fluentmigrator.github.io/)
- **Configuration** : Configuration .NET (`IConfiguration`, `appsettings.json`, variables d'environnement)
- **Logging** : [Serilog](https://serilog.net/) avec structured logging
- **Scheduler** : [TickerQ](https://github.com/yourusername/TickerQ) pour la gestion des tâches périodiques
- **Dependency Injection** : Microsoft.Extensions.DependencyInjection (natif .NET)
- **HTTP Client** : `IHttpClientFactory` avec Polly pour resilience
- **Monitoring** : [OpenTelemetry](https://opentelemetry.io/) + Prometheus exporters

### Architecture des Composants

```
┌─────────────────────────────────────────────────┐
│           EVE ESI API (CCP)                     │
│        https://esi.evetech.net                  │
└────────────────┬────────────────────────────────┘
                 │
                 │ HTTP/HTTPS
                 │ Rate Limited: 300 req/min
                 │
┌────────────────▼────────────────────────────────┐
│    Data Collector (.NET 8 Console App)          │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │  ESI Client (NSwag Generated)            │  │
│  │  - Auto-generated from Swagger           │  │
│  │  - Typed API endpoints                   │  │
│  │  - IHttpClientFactory integration        │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │  HTTP Resilience (Polly)                 │  │
│  │  - Rate Limiter (300 req/min)            │  │
│  │  - Retry with exponential backoff        │  │
│  │  - Circuit Breaker                       │  │
│  │  - Timeout policies                      │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │  Collector Services                      │  │
│  │  - MarketOrderCollectorService           │  │
│  │  - MarketHistoryCollectorService         │  │
│  │  - UniverseDataCollectorService          │  │
│  │  - IndustryDataCollectorService          │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │  Scheduler (TickerQ)                     │  │
│  │  - Interval-based task execution         │  │
│  │  - Lightweight task queue                │  │
│  │  - Async task management                 │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │  Data Layer (Dapper + Npgsql)            │  │
│  │  - Raw SQL with connection pooling       │  │
│  │  - Repository pattern                    │  │
│  │  - DbUp/FluentMigrator for migrations    │  │
│  └──────────────────────────────────────────┘  │
└────────────────┬────────────────────────────────┘
                 │
                 │ PostgreSQL Protocol
                 │
┌────────────────▼────────────────────────────────┐
│       PostgreSQL Database Container             │
│                                                  │
│  - Market Orders (time-series)                  │
│  - Market History                               │
│  - Universe Static Data                         │
│  - Industry Data                                │
│  - Killmails                                    │
└─────────────────────────────────────────────────┘
```

---

## 3. Sources de Données ESI à Collecter

### Priorité 1 : Données Statiques (Universe/Reference Data)

#### Endpoints ESI
- `GET /universe/types/` - Tous les types d'items
- `GET /universe/types/{type_id}/` - Détails d'un item
- `GET /universe/groups/` - Groupes d'items
- `GET /universe/categories/` - Catégories d'items
- `GET /universe/systems/` - Systèmes solaires
- `GET /universe/systems/{system_id}/` - Détails d'un système
- `GET /universe/stations/` - Stations NPC
- `GET /universe/stations/{station_id}/` - Détails d'une station
- `GET /universe/structures/{structure_id}/` - Structures de joueurs (nécessite auth)
- `GET /universe/regions/` - Régions
- `GET /universe/regions/{region_id}/` - Détails d'une région
- `GET /universe/constellations/` - Constellations
- `GET /universe/constellations/{constellation_id}/` - Détails d'une constellation

#### Fréquence de collecte
- **Initial** : Au premier démarrage (bulk import complet)
- **Update** : 1 fois par semaine (données quasi-statiques)

---

### Priorité 2 : Données Personnelles et Corporation (Wallet & Assets)

#### Endpoints ESI (Nécessite authentification SSO)

**Wallet - Personnel**
- `GET /characters/{character_id}/wallet/` - Solde du portefeuille
- `GET /characters/{character_id}/wallet/journal/` - Journal des transactions
- `GET /characters/{character_id}/wallet/transactions/` - Historique des transactions

**Wallet - Corporation**
- `GET /corporations/{corporation_id}/wallets/` - Soldes des divisions
- `GET /corporations/{corporation_id}/wallets/{division}/journal/` - Journal par division
- `GET /corporations/{corporation_id}/wallets/{division}/transactions/` - Transactions par division

**Assets - Personnel**
- `GET /characters/{character_id}/assets/` - Liste complète des assets du personnage
- `GET /characters/{character_id}/assets/locations/` - Localisation des assets
- `GET /characters/{character_id}/assets/names/` - Noms des assets (containers, ships)

**Assets - Corporation**
- `GET /corporations/{corporation_id}/assets/` - Assets de la corporation
- `GET /corporations/{corporation_id}/assets/locations/` - Localisation des assets
- `GET /corporations/{corporation_id}/assets/names/` - Noms des assets

**Market Orders - Personnel & Corporation**
- `GET /characters/{character_id}/orders/` - Ordres de marché du personnage
- `GET /characters/{character_id}/orders/history/` - Historique des ordres
- `GET /corporations/{corporation_id}/orders/` - Ordres de la corporation
- `GET /corporations/{corporation_id}/orders/history/` - Historique des ordres

#### Fréquence de collecte
- **Wallet journal/transactions** : Toutes les 15-30 minutes
- **Assets** : Toutes les heures (données moins volatiles)
- **Market orders** : Toutes les 5-10 minutes (données volatiles)

---

### Priorité 3 : Données de Marché Publiques (Market Data)

#### Endpoints ESI
- `GET /markets/{region_id}/orders/` - Ordres de marché actifs par région
- `GET /markets/{region_id}/history/` - Historique des prix (daily aggregates)
- `GET /markets/prices/` - Prix ajustés et moyens globaux
- `GET /markets/structures/{structure_id}/` - Ordres sur les structures Citadel (nécessite auth)

#### Régions à collecter
- **The Forge** (10000002) - Jita, hub principal
- **Domain** (10000043) - Amarr
- **Sinq Laison** (10000032) - Dodixie
- **Heimatar** (10000030) - Rens
- **Metropolis** (10000042) - Hek
- **Configurable** via ENV pour autres régions

#### Fréquence de collecte
- **Orders** : Toutes les 5-10 minutes (données volatiles)
- **History** : 1 fois par jour à 12:00 EVE time
- **Prices** : 1 fois par heure

---

### Priorité 4 : Données Industrielles (Industry)

#### Endpoints ESI
- `GET /industry/systems/` - Coûts et indices d'industrie par système
- `GET /industry/facilities/` - Facilities industrielles
- `GET /characters/{character_id}/industry/jobs/` - Jobs industriels du personnage
- `GET /corporations/{corporation_id}/industry/jobs/` - Jobs industriels de la corporation
- `GET /universe/schematics/{schematic_id}/` - Schémas de production PI (Planetary Interaction)

#### Fréquence de collecte
- **Industry indices** : 1 fois par jour à 11:00 EVE time
- **Industry jobs** : Toutes les 30 minutes
- **Schematics** : Au premier démarrage (données statiques)

---

### Priorité 5 : Killmails (optionnel, via zKillboard)

#### Source
- **zKillboard API** : https://zkillboard.com/api/
- `https://zkillboard.com/api/history/{YYYYMMDD}.json`

#### Fréquence de collecte
- **Real-time** : Via WebSocket RedisQ (si souhaité)
- **Batch** : 1 fois par jour pour l'historique

---

## 4. Schéma de Base de Données

### Tables Principales (par priorité d'implémentation)

---

#### **Priorité 1 : Données Statiques Universe**

##### `categories`
```sql
CREATE TABLE categories (
    category_id INTEGER PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

##### `groups`
```sql
CREATE TABLE groups (
    group_id INTEGER PRIMARY KEY,
    category_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (category_id) REFERENCES categories(category_id),
    INDEX idx_groups_category (category_id)
);
```

##### `types` (Items)
```sql
CREATE TABLE types (
    type_id INTEGER PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    group_id INTEGER,
    market_group_id INTEGER,
    volume DOUBLE PRECISION,
    capacity DOUBLE PRECISION,
    packaged_volume DOUBLE PRECISION,
    mass DOUBLE PRECISION,
    portion_size INTEGER,
    published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (group_id) REFERENCES groups(group_id),
    INDEX idx_types_name (name),
    INDEX idx_types_group (group_id),
    INDEX idx_types_market_group (market_group_id),
    INDEX idx_types_published (published)
);
```

##### `regions`
```sql
CREATE TABLE regions (
    region_id INTEGER PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    INDEX idx_regions_name (name)
);
```

##### `constellations`
```sql
CREATE TABLE constellations (
    constellation_id INTEGER PRIMARY KEY,
    region_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    position_x DOUBLE PRECISION,
    position_y DOUBLE PRECISION,
    position_z DOUBLE PRECISION,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (region_id) REFERENCES regions(region_id),
    INDEX idx_constellations_region (region_id),
    INDEX idx_constellations_name (name)
);
```

##### `systems`
```sql
CREATE TABLE systems (
    system_id INTEGER PRIMARY KEY,
    constellation_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    security_status DOUBLE PRECISION,
    star_id INTEGER,
    position_x DOUBLE PRECISION,
    position_y DOUBLE PRECISION,
    position_z DOUBLE PRECISION,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (constellation_id) REFERENCES constellations(constellation_id),
    INDEX idx_systems_constellation (constellation_id),
    INDEX idx_systems_name (name),
    INDEX idx_systems_security (security_status)
);
```

##### `stations`
```sql
CREATE TABLE stations (
    station_id BIGINT PRIMARY KEY,
    system_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    type_id INTEGER,
    owner INTEGER,
    position_x DOUBLE PRECISION,
    position_y DOUBLE PRECISION,
    position_z DOUBLE PRECISION,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (system_id) REFERENCES systems(system_id),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    INDEX idx_stations_system (system_id),
    INDEX idx_stations_name (name)
);
```

##### `structures`
```sql
CREATE TABLE structures (
    structure_id BIGINT PRIMARY KEY,
    system_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    type_id INTEGER,
    owner_id INTEGER,
    position_x DOUBLE PRECISION,
    position_y DOUBLE PRECISION,
    position_z DOUBLE PRECISION,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (system_id) REFERENCES systems(system_id),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    INDEX idx_structures_system (system_id),
    INDEX idx_structures_owner (owner_id)
);
```

---

#### **Priorité 2 : Wallet, Assets, Orders (Personnel & Corporation)**

##### `characters`
```sql
CREATE TABLE characters (
    character_id INTEGER PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    corporation_id INTEGER,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    INDEX idx_characters_name (name),
    INDEX idx_characters_corporation (corporation_id)
);
```

##### `corporations`
```sql
CREATE TABLE corporations (
    corporation_id INTEGER PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    ticker VARCHAR(10),
    alliance_id INTEGER,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    INDEX idx_corporations_name (name),
    INDEX idx_corporations_alliance (alliance_id)
);
```

##### `esi_tokens` (Gestion des tokens SSO)
```sql
CREATE TABLE esi_tokens (
    id BIGSERIAL PRIMARY KEY,
    character_id INTEGER NOT NULL,
    character_name VARCHAR(255) NOT NULL,
    owner_hash VARCHAR(255) NOT NULL,
    access_token TEXT NOT NULL,
    refresh_token TEXT NOT NULL,
    token_type VARCHAR(50) NOT NULL DEFAULT 'Bearer',
    expires_at TIMESTAMP NOT NULL,
    scopes TEXT NOT NULL, -- Comma-separated list of scopes
    is_active BOOLEAN DEFAULT TRUE,
    last_refreshed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE,
    INDEX idx_esi_tokens_character (character_id),
    INDEX idx_esi_tokens_active (is_active),
    INDEX idx_esi_tokens_expires (expires_at),
    INDEX idx_esi_tokens_owner_hash (owner_hash)
);
```

##### `esi_token_scopes` (Configuration des scopes par token)
```sql
CREATE TABLE esi_token_scopes (
    id BIGSERIAL PRIMARY KEY,
    token_id BIGINT NOT NULL,
    scope VARCHAR(255) NOT NULL,
    granted_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (token_id) REFERENCES esi_tokens(id) ON DELETE CASCADE,
    UNIQUE (token_id, scope),
    INDEX idx_esi_token_scopes_token (token_id),
    INDEX idx_esi_token_scopes_scope (scope)
);
```

##### `collector_assignments` (Association characters/corporations aux collecteurs)
```sql
CREATE TABLE collector_assignments (
    id BIGSERIAL PRIMARY KEY,
    character_id INTEGER,
    corporation_id INTEGER,
    collector_type VARCHAR(100) NOT NULL, -- 'WALLET', 'ASSETS', 'ORDERS', 'INDUSTRY'
    is_enabled BOOLEAN DEFAULT TRUE,
    collection_interval_minutes INTEGER DEFAULT 60,
    last_collected_at TIMESTAMP,
    next_collection_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE,
    FOREIGN KEY (corporation_id) REFERENCES corporations(corporation_id) ON DELETE CASCADE,
    INDEX idx_collector_assignments_character (character_id),
    INDEX idx_collector_assignments_corporation (corporation_id),
    INDEX idx_collector_assignments_type (collector_type),
    INDEX idx_collector_assignments_enabled (is_enabled),
    INDEX idx_collector_assignments_next_collection (next_collection_at),
    CONSTRAINT chk_collector_assignments_owner CHECK (
        (character_id IS NOT NULL AND corporation_id IS NULL) OR
        (character_id IS NULL AND corporation_id IS NOT NULL)
    )
);
```

##### `wallet_journal` (Personnel & Corporation)
```sql
CREATE TABLE wallet_journal (
    id BIGSERIAL PRIMARY KEY,
    journal_id BIGINT NOT NULL,
    character_id INTEGER,
    corporation_id INTEGER,
    division INTEGER,
    date TIMESTAMP NOT NULL,
    ref_type VARCHAR(100) NOT NULL,
    first_party_id INTEGER,
    second_party_id INTEGER,
    amount DOUBLE PRECISION NOT NULL,
    balance DOUBLE PRECISION,
    reason VARCHAR(500),
    tax DOUBLE PRECISION,
    tax_receiver_id INTEGER,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE (journal_id, character_id, corporation_id, division),
    INDEX idx_wallet_journal_character (character_id),
    INDEX idx_wallet_journal_corporation (corporation_id),
    INDEX idx_wallet_journal_date (date),
    INDEX idx_wallet_journal_ref_type (ref_type)
);
```

##### `wallet_transactions` (Personnel & Corporation)
```sql
CREATE TABLE wallet_transactions (
    transaction_id BIGINT PRIMARY KEY,
    character_id INTEGER,
    corporation_id INTEGER,
    date TIMESTAMP NOT NULL,
    type_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price DOUBLE PRECISION NOT NULL,
    is_buy BOOLEAN NOT NULL,
    is_personal BOOLEAN NOT NULL,
    client_id INTEGER,
    journal_ref_id BIGINT,
    created_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    INDEX idx_wallet_transactions_character (character_id),
    INDEX idx_wallet_transactions_corporation (corporation_id),
    INDEX idx_wallet_transactions_date (date),
    INDEX idx_wallet_transactions_type (type_id)
);
```

##### `assets` (Personnel & Corporation)
```sql
CREATE TABLE assets (
    id BIGSERIAL PRIMARY KEY,
    item_id BIGINT NOT NULL,
    character_id INTEGER,
    corporation_id INTEGER,
    type_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    location_type VARCHAR(50) NOT NULL,
    location_flag VARCHAR(50),
    quantity INTEGER NOT NULL,
    is_singleton BOOLEAN NOT NULL,
    is_blueprint_copy BOOLEAN,
    snapshot_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    INDEX idx_assets_item (item_id),
    INDEX idx_assets_character (character_id),
    INDEX idx_assets_corporation (corporation_id),
    INDEX idx_assets_type (type_id),
    INDEX idx_assets_location (location_id),
    INDEX idx_assets_snapshot (snapshot_at),
    INDEX idx_assets_char_snapshot (character_id, snapshot_at),
    INDEX idx_assets_corp_snapshot (corporation_id, snapshot_at)
);
```

##### `asset_diffs` (Historique des changements)
```sql
CREATE TABLE asset_diffs (
    id BIGSERIAL PRIMARY KEY,
    character_id INTEGER,
    corporation_id INTEGER,
    item_id BIGINT NOT NULL,
    type_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    change_type VARCHAR(20) NOT NULL, -- 'ADDED', 'REMOVED', 'QUANTITY_CHANGED', 'LOCATION_CHANGED'
    old_quantity INTEGER,
    new_quantity INTEGER,
    old_location_id BIGINT,
    new_location_id BIGINT,
    detected_at TIMESTAMP DEFAULT NOW(),
    previous_snapshot_at TIMESTAMP NOT NULL,
    current_snapshot_at TIMESTAMP NOT NULL,
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    INDEX idx_asset_diffs_character (character_id),
    INDEX idx_asset_diffs_corporation (corporation_id),
    INDEX idx_asset_diffs_detected (detected_at),
    INDEX idx_asset_diffs_type (change_type)
);
```

##### `character_orders` (Personnel)
```sql
CREATE TABLE character_orders (
    order_id BIGINT PRIMARY KEY,
    character_id INTEGER NOT NULL,
    type_id INTEGER NOT NULL,
    region_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    volume_total INTEGER NOT NULL,
    volume_remain INTEGER NOT NULL,
    min_volume INTEGER,
    price DOUBLE PRECISION NOT NULL,
    is_buy_order BOOLEAN NOT NULL,
    duration INTEGER NOT NULL,
    issued TIMESTAMP NOT NULL,
    state VARCHAR(50) NOT NULL,
    escrow DOUBLE PRECISION,
    snapshot_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (character_id) REFERENCES characters(character_id),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    FOREIGN KEY (region_id) REFERENCES regions(region_id),
    INDEX idx_character_orders_character (character_id),
    INDEX idx_character_orders_type (type_id),
    INDEX idx_character_orders_state (state),
    INDEX idx_character_orders_snapshot (snapshot_at)
);
```

##### `corporation_orders` (Corporation)
```sql
CREATE TABLE corporation_orders (
    order_id BIGINT PRIMARY KEY,
    corporation_id INTEGER NOT NULL,
    issued_by INTEGER NOT NULL,
    type_id INTEGER NOT NULL,
    region_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    volume_total INTEGER NOT NULL,
    volume_remain INTEGER NOT NULL,
    min_volume INTEGER,
    price DOUBLE PRECISION NOT NULL,
    is_buy_order BOOLEAN NOT NULL,
    duration INTEGER NOT NULL,
    issued TIMESTAMP NOT NULL,
    state VARCHAR(50) NOT NULL,
    wallet_division INTEGER,
    escrow DOUBLE PRECISION,
    snapshot_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (corporation_id) REFERENCES corporations(corporation_id),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    FOREIGN KEY (region_id) REFERENCES regions(region_id),
    INDEX idx_corporation_orders_corporation (corporation_id),
    INDEX idx_corporation_orders_type (type_id),
    INDEX idx_corporation_orders_state (state),
    INDEX idx_corporation_orders_snapshot (snapshot_at)
);
```

---

#### **Priorité 3 : Market Data Public**

##### `market_orders_public` (Time-Series)
```sql
CREATE TABLE market_orders_public (
    order_id BIGINT PRIMARY KEY,
    type_id INTEGER NOT NULL,
    region_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    system_id INTEGER,
    volume_total INTEGER NOT NULL,
    volume_remain INTEGER NOT NULL,
    min_volume INTEGER,
    price DOUBLE PRECISION NOT NULL,
    is_buy_order BOOLEAN NOT NULL,
    duration INTEGER,
    issued TIMESTAMP NOT NULL,
    snapshot_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    FOREIGN KEY (region_id) REFERENCES regions(region_id),
    INDEX idx_market_orders_public_type (type_id),
    INDEX idx_market_orders_public_region (region_id),
    INDEX idx_market_orders_public_snapshot (snapshot_at),
    INDEX idx_market_orders_public_price (price)
);
```

##### `market_history`
```sql
CREATE TABLE market_history (
    id BIGSERIAL PRIMARY KEY,
    type_id INTEGER NOT NULL,
    region_id INTEGER NOT NULL,
    date DATE NOT NULL,
    average DOUBLE PRECISION NOT NULL,
    highest DOUBLE PRECISION NOT NULL,
    lowest DOUBLE PRECISION NOT NULL,
    order_count BIGINT NOT NULL,
    volume BIGINT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE (type_id, region_id, date),
    FOREIGN KEY (type_id) REFERENCES types(type_id),
    FOREIGN KEY (region_id) REFERENCES regions(region_id),
    INDEX idx_market_history_type_region (type_id, region_id),
    INDEX idx_market_history_date (date)
);
```

---

#### **Priorité 4 : Industry Data**

##### `industry_indices`
```sql
CREATE TABLE industry_indices (
    id BIGSERIAL PRIMARY KEY,
    solar_system_id INTEGER NOT NULL,
    activity VARCHAR(50) NOT NULL,
    cost_index DOUBLE PRECISION NOT NULL,
    date DATE NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE (solar_system_id, activity, date),
    FOREIGN KEY (solar_system_id) REFERENCES systems(system_id),
    INDEX idx_industry_indices_system (solar_system_id),
    INDEX idx_industry_indices_date (date)
);
```

##### `industry_jobs` (Personnel & Corporation)
```sql
CREATE TABLE industry_jobs (
    job_id BIGINT PRIMARY KEY,
    character_id INTEGER,
    corporation_id INTEGER,
    installer_id INTEGER NOT NULL,
    facility_id BIGINT NOT NULL,
    station_id BIGINT,
    activity_id INTEGER NOT NULL,
    blueprint_id BIGINT NOT NULL,
    blueprint_type_id INTEGER NOT NULL,
    blueprint_location_id BIGINT NOT NULL,
    output_location_id BIGINT NOT NULL,
    runs INTEGER NOT NULL,
    cost DOUBLE PRECISION,
    licensed_runs INTEGER,
    probability DOUBLE PRECISION,
    product_type_id INTEGER,
    status VARCHAR(50) NOT NULL,
    duration INTEGER NOT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    pause_date TIMESTAMP,
    completed_date TIMESTAMP,
    completed_character_id INTEGER,
    successful_runs INTEGER,
    created_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (blueprint_type_id) REFERENCES types(type_id),
    FOREIGN KEY (product_type_id) REFERENCES types(type_id),
    INDEX idx_industry_jobs_character (character_id),
    INDEX idx_industry_jobs_corporation (corporation_id),
    INDEX idx_industry_jobs_status (status),
    INDEX idx_industry_jobs_start_date (start_date)
);
```

### Optimisations

#### Partitioning
- **market_orders** : Partitionnement par `snapshot_at` (mensuel)
- **market_history** : Partitionnement par `date` (annuel)

#### Time-Series Optimization
- Utiliser **TimescaleDB** extension pour `market_orders` et `market_history`
- Compression automatique des anciennes données

#### Indexes
- Indexes composites pour les requêtes fréquentes
- BRIN indexes pour les timestamps (time-series)

---

## 5. Configuration et Déploiement

### Variables d'Environnement

```bash
# Database
DATABASE_URL=postgresql://user:password@db:5432/evedata
DATABASE_MAX_CONNECTIONS=25
DATABASE_MIN_CONNECTIONS=5

# ESI Configuration
ESI_USER_AGENT=eve-data-collector/1.0 (your@email.com)
ESI_CLIENT_ID=your_client_id_here
ESI_CLIENT_SECRET=your_client_secret_here
ESI_CALLBACK_URL=http://localhost:8080/callback

# Collection Settings
COLLECT_MARKET_ORDERS=true
COLLECT_MARKET_HISTORY=true
COLLECT_UNIVERSE_DATA=true
COLLECT_INDUSTRY_DATA=true
COLLECT_KILLMAILS=false

# Market Collection
MARKET_REGIONS=10000002,10000043,10000032,10000030,10000042
MARKET_ORDERS_INTERVAL=5m
MARKET_HISTORY_CRON=0 12 * * *

# Universe Data
UNIVERSE_UPDATE_CRON=0 2 * * 0  # Dimanche 2h

# Industry Data
INDUSTRY_UPDATE_CRON=0 3 * * *  # Tous les jours 3h

# Rate Limiting
ESI_RATE_LIMIT=300  # requests per minute
ESI_BURST_LIMIT=50

# Logging
LOG_LEVEL=info
LOG_FORMAT=json

# Monitoring (optionnel)
METRICS_ENABLED=true
METRICS_PORT=9090
```

### Structure Docker

#### Dockerfile (Multi-stage build)

```dockerfile
# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["EveDataCollector/EveDataCollector.csproj", "EveDataCollector/"]
RUN dotnet restore "EveDataCollector/EveDataCollector.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/EveDataCollector"
RUN dotnet build "EveDataCollector.csproj" -c Release -o /app/build

# --- Publish Stage ---
FROM build AS publish
RUN dotnet publish "EveDataCollector.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install timezone data and CA certificates
RUN apt-get update && apt-get install -y \
    ca-certificates \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080 9090

ENTRYPOINT ["dotnet", "EveDataCollector.dll"]
```

#### docker-compose.yml

```yaml
version: '3.8'

services:
  db:
    image: timescale/timescaledb:latest-pg16
    container_name: eve-db
    environment:
      POSTGRES_USER: eveuser
      POSTGRES_PASSWORD: ${DB_PASSWORD:-changeme}
      POSTGRES_DB: evedata
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U eveuser"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  collector:
    build: .
    container_name: eve-collector
    environment:
      DATABASE_URL: postgresql://eveuser:${DB_PASSWORD:-changeme}@db:5432/evedata
      ESI_USER_AGENT: eve-data-collector/1.0
      ESI_RATE_LIMIT: 300
      MARKET_REGIONS: 10000002,10000043,10000032,10000030,10000042
      MARKET_ORDERS_INTERVAL: 5m
      LOG_LEVEL: info
      LOG_FORMAT: json
    depends_on:
      db:
        condition: service_healthy
    restart: unless-stopped
    volumes:
      - ./config:/config
    ports:
      - "9090:9090"  # Metrics

  # Optionnel : Grafana pour visualisation
  grafana:
    image: grafana/grafana:latest
    container_name: eve-grafana
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_PASSWORD:-admin}
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./grafana/datasources:/etc/grafana/provisioning/datasources
    ports:
      - "3000:3000"
    depends_on:
      - db
    restart: unless-stopped

volumes:
  postgres_data:
  grafana_data:
```

---

## 6. Fonctionnalités Clés

### Rate Limiting ESI
- Implémentation d'un **token bucket** pour respecter la limite de 300 req/min
- **Burst** supporté jusqu'à 50 requêtes simultanées
- **Backoff exponentiel** en cas d'erreur 429

### Retry Logic
- **3 tentatives** par défaut avec backoff exponentiel
- Gestion spécifique des codes HTTP :
  - `429` : Rate limit, attendre et retry
  - `5xx` : Erreur serveur, retry avec backoff
  - `4xx` : Erreur client, pas de retry (sauf 429)

### Monitoring et Métriques
- **Prometheus metrics** :
  - `esi_requests_total{endpoint, status}`
  - `esi_request_duration_seconds{endpoint}`
  - `collector_last_run_timestamp{collector_type}`
  - `collector_items_processed{collector_type}`
  - `db_queries_total{query_type}`

### Healthchecks
- Endpoint `/health` pour vérifier l'état du collecteur
- Vérification connexion DB
- Vérification accès ESI

---

## 7. Plan de Développement

### Phase 1 : MVP - Données Statiques Universe
- [ ] Configuration du projet .NET 8 Console Application avec Generic Host
- [ ] Génération du client ESI avec NSwag depuis Swagger ESI
- [ ] Configuration Dapper + Npgsql pour PostgreSQL
- [ ] Mise en place DbUp pour migrations SQL
- [ ] Implémentation du rate limiting avec Polly
- [ ] Configuration TickerQ pour scheduling de base
- [ ] Schéma de base de données - Priorité 1 (Universe)
  - [ ] Tables: categories, groups, types, regions, constellations, systems, stations
- [ ] Collecteur de données statiques Universe
  - [ ] UniverseTypesCollectorService (bulk import)
  - [ ] UniverseRegionsCollectorService
  - [ ] UniverseSystemsCollectorService
  - [ ] UniverseStationsCollectorService
- [ ] Logging structuré avec Serilog (console)
- [ ] Docker & docker-compose
- [ ] README avec instructions

### Phase 2 : Wallet, Assets, Orders (Personnel & Corporation)
- [ ] Implémentation authentification SSO EVE Online
  - [ ] OAuth2 flow (SSOAuthenticationHandler)
  - [ ] Token refresh management (TokenManager)
  - [ ] Token encryption (TokenEncryptionService)
  - [ ] Scopes configuration et validation
- [ ] Schéma de base de données - Priorité 2
  - [ ] Tables: characters, corporations
  - [ ] Tables: esi_tokens, esi_token_scopes, collector_assignments
  - [ ] Tables: wallet_journal, wallet_transactions
  - [ ] Tables: assets, asset_diffs
  - [ ] Tables: character_orders, corporation_orders
- [ ] Repositories de gestion des tokens
  - [ ] EsiTokenRepository (CRUD tokens)
  - [ ] CollectorAssignmentRepository (configuration collecteurs)
- [ ] Services de gestion
  - [ ] TokenManagementService (ajout/suppression tokens)
  - [ ] CollectorAssignmentService (enable/disable collecteurs)
  - [ ] TokenRefreshScheduler (auto-refresh avant expiration)
- [ ] Collecteurs authentifiés
  - [ ] WalletJournalCollectorService (character + corp)
  - [ ] WalletTransactionsCollectorService (character + corp)
  - [ ] AssetsCollectorService (character + corp)
  - [ ] AssetDiffService (calcul des diffs entre snapshots)
  - [ ] CharacterOrdersCollectorService
  - [ ] CorporationOrdersCollectorService
- [ ] Configuration multi-character/multi-corporation
- [ ] Optimisation requêtes Dapper avec bulk operations
- [ ] API optionnelle de gestion des tokens (POST/DELETE/PUT)

### Phase 3 : Market Data Public
- [ ] Schéma de base de données - Priorité 3
  - [ ] Tables: market_orders_public, market_history
- [ ] Collecteurs de marché public
  - [ ] MarketOrdersCollectorService (multi-régions)
  - [ ] MarketHistoryCollectorService
  - [ ] MarketPricesCollectorService
- [ ] Support multi-régions configurable
- [ ] Optimisation time-series (TimescaleDB optionnel)
- [ ] Configuration avancée TickerQ (intervals différents par collecteur)

### Phase 4 : Industry Data
- [ ] Schéma de base de données - Priorité 4
  - [ ] Tables: industry_indices, industry_jobs
- [ ] Collecteurs industriels
  - [ ] IndustryIndicesCollectorService
  - [ ] IndustryJobsCollectorService (character + corp)
  - [ ] IndustryFacilitiesCollectorService
- [ ] Métriques avec OpenTelemetry + Prometheus exporter
- [ ] Retry policies avancées avec Polly

### Phase 5 : Production Ready
- [ ] Tests unitaires avec xUnit
- [ ] Tests d'intégration avec Testcontainers + Dapper
- [ ] CI/CD (GitHub Actions)
- [ ] Healthchecks endpoint
- [ ] Dashboards Grafana pour monitoring
- [ ] Documentation complète
- [ ] Optimisations de performance (partitioning, compression)

---

## 8. Questions Ouvertes / Décisions à Prendre

### Choix Techniques
- [x] **Langage** : .NET 8 avec C#
- [x] **Type de projet** : Console Application (pas de Worker Service)
- [x] **Client ESI** : NSwag généré depuis Swagger
- [x] **ORM** : Dapper (micro-ORM)
- [x] **Scheduler** : TickerQ
- [ ] **Migrations** : DbUp ou FluentMigrator ?
- [ ] **TimescaleDB** : Utiliser l'extension ou PostgreSQL vanilla ?
- [ ] **Cache** : Redis pour cache intermédiaire ?

### Fonctionnalités
- [ ] **SSO** : Implémenter dès le début ou plus tard ?
- [ ] **API REST** : Nécessaire ou juste accès direct à la DB ?
- [ ] **Killmails** : Via zKillboard WebSocket ou batch ?
- [ ] **Compression** : Archiver les anciennes données de marché ?

### Déploiement
- [ ] **Production** : Self-hosted ou cloud (AWS/GCP/Azure) ?
- [ ] **Backup** : Stratégie de sauvegarde automatique ?
- [ ] **Monitoring** : Stack complète (Prometheus + Grafana) ou simple ?

---

## 9. Ressources et Références

### Documentation Officielle
- **ESI Docs** : https://docs.esi.evetech.net/
- **ESI Swagger UI** : https://esi.evetech.net/ui/
- **EVE Developers** : https://developers.eveonline.com/

### Outils et Bibliothèques .NET
- **NSwag** : https://github.com/RicoSuter/NSwag
- **Dapper** : https://github.com/DapperLib/Dapper
- **Npgsql** (PostgreSQL pour .NET) : https://www.npgsql.org/
- **DbUp** : https://dbup.readthedocs.io/
- **FluentMigrator** : https://fluentmigrator.github.io/
- **Polly** (Resilience) : https://github.com/App-vNext/Polly
- **TickerQ** : (À confirmer le lien GitHub)
- **Serilog** : https://serilog.net/
- **OpenTelemetry .NET** : https://opentelemetry.io/docs/languages/net/

### Communauté
- **Discord #3rd-party-dev-and-esi** : Canal officiel CCP
- **r/Eve** : https://reddit.com/r/Eve
- **EVE Forums - Third Party Developers** : https://forums.eveonline.com/c/technology-research/third-party-developers/

### Outils Tiers (pour référence)
- **Fuzzwork** : https://market.fuzzwork.co.uk/
- **EVEMarketer** : https://evemarketer.com/
- **zKillboard** : https://zkillboard.com/

---

## 10. Prochaines Étapes

1. ✅ **Valider la stack technique** : .NET 8 + NSwag + Dapper + TickerQ
2. ✅ **Valider l'ordre de priorité** : Universe → Wallet/Assets/Orders → Market → Industry
3. **Générer le client ESI** : Utiliser NSwag CLI avec la définition Swagger ESI
4. **Initialiser le projet** : Console Application .NET 8 avec Generic Host
5. **Configurer Dapper + Npgsql** : Setup PostgreSQL avec connection pooling
6. **Mettre en place les migrations** : DbUp pour gestion des migrations SQL
7. **Implémenter Phase 1** : Collecte données statiques Universe
8. **Implémenter Phase 2** : SSO + Collecte Wallet/Assets/Orders (auth requise)
9. **Implémenter Phase 3** : Collecte Market Data public
10. **Implémenter Phase 4** : Collecte Industry Data
11. **Tester** et itérer
12. **Déployer** et monitorer

---

## 11. Structure du Projet .NET

```
EveDataCollector/
├── src/
│   ├── EveDataCollector.App/          # Console Application principale
│   │   ├── Program.cs                 # Entry point + Generic Host setup
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Services/
│   │   │   ├── Universe/              # Phase 1
│   │   │   │   ├── UniverseTypesCollectorService.cs
│   │   │   │   ├── UniverseRegionsCollectorService.cs
│   │   │   │   ├── UniverseSystemsCollectorService.cs
│   │   │   │   └── UniverseStationsCollectorService.cs
│   │   │   ├── Wallet/                # Phase 2
│   │   │   │   ├── WalletJournalCollectorService.cs
│   │   │   │   └── WalletTransactionsCollectorService.cs
│   │   │   ├── Assets/                # Phase 2
│   │   │   │   └── AssetsCollectorService.cs
│   │   │   ├── Orders/                # Phase 2
│   │   │   │   ├── CharacterOrdersCollectorService.cs
│   │   │   │   └── CorporationOrdersCollectorService.cs
│   │   │   ├── Market/                # Phase 3
│   │   │   │   ├── MarketOrdersCollectorService.cs
│   │   │   │   ├── MarketHistoryCollectorService.cs
│   │   │   │   └── MarketPricesCollectorService.cs
│   │   │   └── Industry/              # Phase 4
│   │   │       ├── IndustryIndicesCollectorService.cs
│   │   │       └── IndustryJobsCollectorService.cs
│   │   ├── Authentication/
│   │   │   ├── SSOAuthenticationHandler.cs   # OAuth2 flow
│   │   │   ├── TokenManager.cs                # Token refresh & storage
│   │   │   └── TokenEncryptionService.cs      # Chiffrement tokens en base
│   │   ├── Management/
│   │   │   ├── TokenManagementService.cs      # CRUD tokens
│   │   │   └── CollectorAssignmentService.cs  # Configuration collecteurs
│   │   └── Scheduling/
│   │       ├── CollectorScheduler.cs          # TickerQ configuration
│   │       └── TokenRefreshScheduler.cs       # Auto-refresh tokens
│   │
│   ├── EveDataCollector.Core/         # Logique métier
│   │   ├── Interfaces/
│   │   │   ├── ICollectorService.cs
│   │   │   ├── IRepository.cs
│   │   │   └── IAuthenticationService.cs
│   │   ├── Services/
│   │   └── Models/
│   │       ├── Universe/              # Phase 1
│   │       │   ├── Category.cs
│   │       │   ├── Group.cs
│   │       │   ├── ItemType.cs
│   │       │   ├── Region.cs
│   │       │   ├── Constellation.cs
│   │       │   ├── SolarSystem.cs
│   │       │   └── Station.cs
│   │       ├── Wallet/                # Phase 2
│   │       │   ├── WalletJournal.cs
│   │       │   └── WalletTransaction.cs
│   │       ├── Assets/                # Phase 2
│   │       │   └── Asset.cs
│   │       ├── Orders/                # Phase 2
│   │       │   ├── CharacterOrder.cs
│   │       │   └── CorporationOrder.cs
│   │       ├── Market/                # Phase 3
│   │       │   ├── MarketOrder.cs
│   │       │   └── MarketHistory.cs
│   │       └── Industry/              # Phase 4
│   │           ├── IndustryIndex.cs
│   │           └── IndustryJob.cs
│   │
│   ├── EveDataCollector.Infrastructure/  # Data access + ESI
│   │   ├── Data/
│   │   │   ├── ConnectionFactory.cs   # Npgsql connection pooling
│   │   │   └── Migrations/            # DbUp scripts (ordre d'implémentation)
│   │   │       ├── 001_Universe_Categories.sql
│   │   │       ├── 002_Universe_Groups.sql
│   │   │       ├── 003_Universe_Types.sql
│   │   │       ├── 004_Universe_Regions.sql
│   │   │       ├── 005_Universe_Constellations.sql
│   │   │       ├── 006_Universe_Systems.sql
│   │   │       ├── 007_Universe_Stations.sql
│   │   │       ├── 008_Characters_Corporations.sql
│   │   │       ├── 009_ESI_Tokens.sql
│   │   │       ├── 010_Collector_Assignments.sql
│   │   │       ├── 011_Wallet_Tables.sql
│   │   │       ├── 012_Assets_Tables.sql
│   │   │       ├── 013_Asset_Diffs.sql
│   │   │       ├── 014_Orders_Tables.sql
│   │   │       ├── 015_Market_Tables.sql
│   │   │       └── 016_Industry_Tables.sql
│   │   ├── Repositories/              # Dapper queries par domaine
│   │   │   ├── Authentication/
│   │   │   │   ├── EsiTokenRepository.cs
│   │   │   │   └── CollectorAssignmentRepository.cs
│   │   │   ├── Universe/
│   │   │   │   ├── TypeRepository.cs
│   │   │   │   ├── RegionRepository.cs
│   │   │   │   └── SystemRepository.cs
│   │   │   ├── Wallet/
│   │   │   │   ├── WalletJournalRepository.cs
│   │   │   │   └── WalletTransactionRepository.cs
│   │   │   ├── Assets/
│   │   │   │   ├── AssetRepository.cs
│   │   │   │   └── AssetDiffRepository.cs
│   │   │   ├── Orders/
│   │   │   │   ├── CharacterOrderRepository.cs
│   │   │   │   └── CorporationOrderRepository.cs
│   │   │   ├── Market/
│   │   │   │   ├── MarketOrderRepository.cs
│   │   │   │   └── MarketHistoryRepository.cs
│   │   │   └── Industry/
│   │   │       ├── IndustryIndexRepository.cs
│   │   │       └── IndustryJobRepository.cs
│   │   └── ESI/
│   │       └── Generated/              # Client NSwag généré ici
│   │
│   └── EveDataCollector.Shared/       # DTOs, Constants, Config
│       ├── Configuration/
│       │   ├── EsiConfiguration.cs
│       │   └── DatabaseConfiguration.cs
│       └── Constants/
│           └── EsiConstants.cs
│
├── tests/
│   ├── EveDataCollector.UnitTests/
│   └── EveDataCollector.IntegrationTests/
│
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
│
├── scripts/
│   ├── generate-esi-client.sh        # Script NSwag generation
│   └── run-migrations.sh             # Run DbUp/FluentMigrator
│
└── EveDataCollector.sln
```

---

**Date de création** : 2025-10-16
**Dernière mise à jour** : 2025-10-18
**Version** : 4.0.0

**Changelog v4.0.0** :
- ✅ Ajout gestion complète des tokens SSO (esi_tokens, esi_token_scopes, collector_assignments)
- ✅ Ajout système de diff pour assets (asset_diffs table)
- ✅ Ajout services de gestion des tokens (TokenManagementService, TokenEncryptionService)
- ✅ Ajout scheduler de refresh automatique des tokens (TokenRefreshScheduler)
- ✅ Ajout algorithme de calcul des diffs d'assets avec exemples de code
- ✅ Documentation complète du workflow d'ajout/suppression de tokens
- ✅ API optionnelle de gestion des tokens
- ✅ 16 migrations SQL au lieu de 13 (ajout tokens + diffs)

---

## 12. Notes Techniques Importantes

### Pourquoi Dapper au lieu d'EF Core ?
- **Performance** : Dapper est un micro-ORM ultra-rapide, idéal pour des insertions massives de données de marché
- **Contrôle** : Requêtes SQL explicites, meilleur contrôle sur les performances
- **Simplicité** : Pas de tracking, pas de complexité inutile pour un collecteur de données
- **Bulk operations** : Facilite les insertions par batch pour optimiser la collecte

### Pourquoi TickerQ ?
- **Léger** : Pas de persistence en base, pas de complexité Quartz.NET/Hangfire
- **Suffisant** : Pour des tâches périodiques simples (toutes les 5 min, toutes les heures)
- **Async-first** : Conçu pour les workloads asynchrones modernes
- **En mémoire** : Pas de dépendance externe pour le scheduling

### Pourquoi Console App et pas Worker Service ?
- **Flexibilité** : Generic Host donne accès à DI, configuration, logging sans le cadre rigide du Worker Service
- **Contrôle** : Gestion manuelle du lifecycle avec TickerQ
- **Simplicité** : Pas besoin du pattern BackgroundService si on utilise TickerQ

---

## 13. Gestion des Tokens SSO et Assets Tracking

### Gestion des Tokens ESI (SSO EVE Online)

#### Workflow d'ajout d'un token
1. **Authentification initiale** : Flow OAuth2 standard EVE SSO
2. **Stockage sécurisé** : Token chiffré en base dans `esi_tokens`
3. **Configuration collecteur** : Création d'entrées dans `collector_assignments`
4. **Refresh automatique** : Background job qui refresh les tokens avant expiration

#### Structure des tokens
```csharp
public class EsiToken
{
    public long Id { get; set; }
    public int CharacterId { get; set; }
    public string CharacterName { get; set; }
    public string OwnerHash { get; set; }  // Pour valider le propriétaire
    public string AccessToken { get; set; }  // Chiffré en base
    public string RefreshToken { get; set; } // Chiffré en base
    public DateTime ExpiresAt { get; set; }
    public List<string> Scopes { get; set; }
    public bool IsActive { get; set; }
}
```

#### Scopes requis par collecteur
- **Wallet** : `esi-wallet.read_character_wallet.v1`, `esi-wallet.read_corporation_wallets.v1`
- **Assets** : `esi-assets.read_assets.v1`, `esi-assets.read_corporation_assets.v1`
- **Orders** : `esi-markets.read_character_orders.v1`, `esi-markets.read_corporation_orders.v1`
- **Industry** : `esi-industry.read_character_jobs.v1`, `esi-industry.read_corporation_jobs.v1`

#### API de gestion des tokens (optionnelle)
```
POST   /api/tokens/register     - Démarrer le flow SSO
GET    /api/tokens/callback     - Callback OAuth2
GET    /api/tokens              - Liste tous les tokens
DELETE /api/tokens/{id}         - Supprimer un token
PUT    /api/tokens/{id}/enable  - Activer/désactiver
```

### Asset Diff Tracking

#### Principe
À chaque collecte d'assets, le système :
1. **Récupère** les assets actuels via ESI
2. **Compare** avec le dernier snapshot en base
3. **Détecte** les changements (ajout, suppression, quantité, localisation)
4. **Enregistre** les diffs dans `asset_diffs`
5. **Stocke** le nouveau snapshot dans `assets`

#### Types de changements détectés
```sql
-- ADDED: Nouvel asset apparu
-- REMOVED: Asset disparu (vendu, détruit, etc.)
-- QUANTITY_CHANGED: Quantité modifiée (stack modifié)
-- LOCATION_CHANGED: Asset déplacé vers un autre endroit
```

#### Algorithme de diff
```csharp
public async Task<List<AssetDiff>> ComputeAssetDiffs(
    int characterId,
    List<Asset> currentAssets,
    DateTime currentSnapshotAt)
{
    var diffs = new List<AssetDiff>();

    // 1. Récupérer le dernier snapshot
    var previousSnapshot = await GetLastSnapshot(characterId);
    if (previousSnapshot == null) return diffs; // Premier run

    var previousAssets = previousSnapshot.Assets.ToDictionary(a => a.ItemId);
    var currentAssetsDict = currentAssets.ToDictionary(a => a.ItemId);

    // 2. Détecter les assets ajoutés
    foreach (var asset in currentAssets.Where(a => !previousAssets.ContainsKey(a.ItemId)))
    {
        diffs.Add(new AssetDiff
        {
            ChangeType = "ADDED",
            ItemId = asset.ItemId,
            TypeId = asset.TypeId,
            NewQuantity = asset.Quantity,
            NewLocationId = asset.LocationId,
            // ...
        });
    }

    // 3. Détecter les assets supprimés
    foreach (var asset in previousAssets.Values.Where(a => !currentAssetsDict.ContainsKey(a.ItemId)))
    {
        diffs.Add(new AssetDiff
        {
            ChangeType = "REMOVED",
            ItemId = asset.ItemId,
            TypeId = asset.TypeId,
            OldQuantity = asset.Quantity,
            OldLocationId = asset.LocationId,
            // ...
        });
    }

    // 4. Détecter les changements (quantité ou localisation)
    foreach (var kvp in currentAssetsDict)
    {
        if (previousAssets.TryGetValue(kvp.Key, out var oldAsset))
        {
            var newAsset = kvp.Value;

            // Changement de quantité
            if (oldAsset.Quantity != newAsset.Quantity)
            {
                diffs.Add(new AssetDiff
                {
                    ChangeType = "QUANTITY_CHANGED",
                    ItemId = newAsset.ItemId,
                    TypeId = newAsset.TypeId,
                    OldQuantity = oldAsset.Quantity,
                    NewQuantity = newAsset.Quantity,
                    // ...
                });
            }

            // Changement de localisation
            if (oldAsset.LocationId != newAsset.LocationId)
            {
                diffs.Add(new AssetDiff
                {
                    ChangeType = "LOCATION_CHANGED",
                    ItemId = newAsset.ItemId,
                    TypeId = newAsset.TypeId,
                    OldLocationId = oldAsset.LocationId,
                    NewLocationId = newAsset.LocationId,
                    // ...
                });
            }
        }
    }

    return diffs;
}
```

#### Cas d'usage des diffs
- **Audit trail** : Historique complet des mouvements d'assets
- **Alertes** : Notifications sur assets disparus/ajoutés
- **Analyse** : Statistiques de mouvement d'inventaire
- **Détection anomalies** : Assets supprimés massivement (vol, destruction)

### Configuration des Collecteurs

#### Ajout d'un character pour collecte
```sql
-- 1. Ajouter le token
INSERT INTO esi_tokens (character_id, character_name, access_token, refresh_token, expires_at, scopes, ...)
VALUES (12345, 'John Doe', '...', '...', NOW() + INTERVAL '20 minutes', 'esi-wallet.read_character_wallet.v1,...', ...);

-- 2. Configurer les collecteurs pour ce character
INSERT INTO collector_assignments (character_id, collector_type, is_enabled, collection_interval_minutes)
VALUES
    (12345, 'WALLET', TRUE, 30),
    (12345, 'ASSETS', TRUE, 60),
    (12345, 'ORDERS', TRUE, 10),
    (12345, 'INDUSTRY', TRUE, 30);
```

#### Désactivation d'un collecteur
```sql
UPDATE collector_assignments
SET is_enabled = FALSE
WHERE character_id = 12345 AND collector_type = 'WALLET';
```

#### Suppression d'un token
```sql
-- CASCADE supprimera automatiquement les assignments et token_scopes
DELETE FROM esi_tokens WHERE character_id = 12345;
```
