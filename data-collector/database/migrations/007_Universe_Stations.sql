-- Migration: 007_Universe_Stations
-- Description: Create stations table for EVE NPC stations
-- Phase: 1 (Universe Static Data)

CREATE TABLE IF NOT EXISTS stations (
    station_id BIGINT PRIMARY KEY,
    system_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    type_id INTEGER,
    owner INTEGER,
    race_id INTEGER,
    position_x DOUBLE PRECISION,
    position_y DOUBLE PRECISION,
    position_z DOUBLE PRECISION,
    max_dockable_ship_volume DOUBLE PRECISION,
    office_rental_cost DOUBLE PRECISION,
    reprocessing_efficiency DOUBLE PRECISION,
    reprocessing_stations_take DOUBLE PRECISION,
    services TEXT[], -- Array of available services
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (system_id) REFERENCES systems(system_id) ON DELETE CASCADE,
    FOREIGN KEY (type_id) REFERENCES types(type_id) ON DELETE SET NULL
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_stations_system ON stations(system_id);
CREATE INDEX IF NOT EXISTS idx_stations_name ON stations(name);
CREATE INDEX IF NOT EXISTS idx_stations_owner ON stations(owner);
CREATE INDEX IF NOT EXISTS idx_stations_type ON stations(type_id);

-- GIN index for services array
CREATE INDEX IF NOT EXISTS idx_stations_services ON stations USING gin(services);

-- Comments
COMMENT ON TABLE stations IS 'EVE Online NPC stations from ESI';
COMMENT ON COLUMN stations.station_id IS 'Unique station ID from ESI';
COMMENT ON COLUMN stations.system_id IS 'Parent system ID';
COMMENT ON COLUMN stations.name IS 'Station name';
COMMENT ON COLUMN stations.type_id IS 'Station type ID (structure type)';
COMMENT ON COLUMN stations.owner IS 'Owner corporation ID';
COMMENT ON COLUMN stations.race_id IS 'Race ID of the station';
COMMENT ON COLUMN stations.services IS 'Array of available services (e.g., market, refinery, etc.)';
