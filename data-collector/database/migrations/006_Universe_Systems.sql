-- Migration: 006_Universe_Systems
-- Description: Create systems table for EVE solar systems
-- Phase: 1 (Universe Static Data)

CREATE TABLE IF NOT EXISTS systems (
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
    FOREIGN KEY (constellation_id) REFERENCES constellations(constellation_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_systems_constellation ON systems(constellation_id);
CREATE INDEX IF NOT EXISTS idx_systems_name ON systems(name);
CREATE INDEX IF NOT EXISTS idx_systems_security ON systems(security_status);

-- Comments
COMMENT ON TABLE systems IS 'EVE Online solar systems from ESI';
COMMENT ON COLUMN systems.system_id IS 'Unique system ID from ESI';
COMMENT ON COLUMN systems.constellation_id IS 'Parent constellation ID';
COMMENT ON COLUMN systems.name IS 'System name (e.g., Jita, Amarr, etc.)';
COMMENT ON COLUMN systems.security_status IS 'Security status (-1.0 to 1.0)';
COMMENT ON COLUMN systems.star_id IS 'Star type ID in this system';
COMMENT ON COLUMN systems.position_x IS 'X coordinate in space';
COMMENT ON COLUMN systems.position_y IS 'Y coordinate in space';
COMMENT ON COLUMN systems.position_z IS 'Z coordinate in space';
