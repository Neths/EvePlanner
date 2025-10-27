-- Character Assets
-- Stores character assets (items, ships, etc.)

CREATE TABLE IF NOT EXISTS character_assets (
    item_id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id) ON DELETE CASCADE,
    type_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    location_type VARCHAR(50) NOT NULL, -- station, solar_system, item (container)
    location_flag VARCHAR(50) NOT NULL, -- Hangar, Cargo, etc.
    quantity BIGINT NOT NULL DEFAULT 1,
    is_singleton BOOLEAN NOT NULL DEFAULT false,
    is_blueprint_copy BOOLEAN,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_character_assets_character ON character_assets(character_id);
CREATE INDEX idx_character_assets_location ON character_assets(location_id);
CREATE INDEX idx_character_assets_type ON character_assets(type_id);
CREATE INDEX idx_character_assets_character_location ON character_assets(character_id, location_id);

-- Comments
COMMENT ON TABLE character_assets IS 'Character assets from ESI';
COMMENT ON COLUMN character_assets.item_id IS 'Unique item instance ID';
COMMENT ON COLUMN character_assets.is_singleton IS 'True for unique items (ships, modules)';
COMMENT ON COLUMN character_assets.location_flag IS 'Where the item is located (Hangar, Cargo, etc.)';
