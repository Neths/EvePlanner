-- Migration: 004_Universe_Regions
-- Description: Create regions table for EVE regions
-- Phase: 1 (Universe Static Data)

CREATE TABLE IF NOT EXISTS regions (
    region_id INTEGER PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_regions_name ON regions(name);

-- Comments
COMMENT ON TABLE regions IS 'EVE Online regions from ESI';
COMMENT ON COLUMN regions.region_id IS 'Unique region ID from ESI';
COMMENT ON COLUMN regions.name IS 'Region name (e.g., The Forge, Domain, etc.)';
COMMENT ON COLUMN regions.description IS 'Region description';
