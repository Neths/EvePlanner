-- Migration: 005_Universe_Constellations
-- Description: Create constellations table for EVE constellations
-- Phase: 1 (Universe Static Data)

CREATE TABLE IF NOT EXISTS constellations (
    constellation_id INTEGER PRIMARY KEY,
    region_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    position_x DOUBLE PRECISION,
    position_y DOUBLE PRECISION,
    position_z DOUBLE PRECISION,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (region_id) REFERENCES regions(region_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_constellations_region ON constellations(region_id);
CREATE INDEX IF NOT EXISTS idx_constellations_name ON constellations(name);

-- Comments
COMMENT ON TABLE constellations IS 'EVE Online constellations from ESI';
COMMENT ON COLUMN constellations.constellation_id IS 'Unique constellation ID from ESI';
COMMENT ON COLUMN constellations.region_id IS 'Parent region ID';
COMMENT ON COLUMN constellations.name IS 'Constellation name';
COMMENT ON COLUMN constellations.position_x IS 'X coordinate in space';
COMMENT ON COLUMN constellations.position_y IS 'Y coordinate in space';
COMMENT ON COLUMN constellations.position_z IS 'Z coordinate in space';
