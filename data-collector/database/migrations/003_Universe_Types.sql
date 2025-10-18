-- Migration: 003_Universe_Types
-- Description: Create types table for EVE item types (items)
-- Phase: 1 (Universe Static Data)

CREATE TABLE IF NOT EXISTS types (
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
    radius DOUBLE PRECISION,
    published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (group_id) REFERENCES groups(group_id) ON DELETE SET NULL
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_types_name ON types(name);
CREATE INDEX IF NOT EXISTS idx_types_group ON types(group_id);
CREATE INDEX IF NOT EXISTS idx_types_market_group ON types(market_group_id);
CREATE INDEX IF NOT EXISTS idx_types_published ON types(published);

-- Full-text search index for item names
CREATE INDEX IF NOT EXISTS idx_types_name_trgm ON types USING gin(name gin_trgm_ops);

-- Comments
COMMENT ON TABLE types IS 'EVE Online item types (all items in the game) from ESI';
COMMENT ON COLUMN types.type_id IS 'Unique type ID from ESI';
COMMENT ON COLUMN types.name IS 'Item name (e.g., Tritanium, Rifter, etc.)';
COMMENT ON COLUMN types.description IS 'Item description text';
COMMENT ON COLUMN types.group_id IS 'Parent group ID';
COMMENT ON COLUMN types.market_group_id IS 'Market group for trading';
COMMENT ON COLUMN types.volume IS 'Item volume in m³';
COMMENT ON COLUMN types.capacity IS 'Item cargo/container capacity in m³';
COMMENT ON COLUMN types.packaged_volume IS 'Volume when packaged in m³';
COMMENT ON COLUMN types.mass IS 'Item mass in kg';
COMMENT ON COLUMN types.portion_size IS 'Default stack size';
COMMENT ON COLUMN types.published IS 'Whether this item is published/visible';
