-- EVE Online Characters
-- Stores basic character information from ESI

CREATE TABLE IF NOT EXISTS characters (
    character_id BIGINT PRIMARY KEY,
    character_name VARCHAR(255) NOT NULL,
    corporation_id BIGINT,
    corporation_name VARCHAR(255),
    alliance_id BIGINT,
    alliance_name VARCHAR(255),
    faction_id INTEGER,
    birthday TIMESTAMP WITH TIME ZONE,
    gender VARCHAR(10),
    race_id INTEGER,
    bloodline_id INTEGER,
    ancestry_id INTEGER,
    security_status DECIMAL(10, 2),
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for common queries
CREATE INDEX idx_characters_name ON characters(character_name);
CREATE INDEX idx_characters_corporation ON characters(corporation_id);
CREATE INDEX idx_characters_alliance ON characters(alliance_id);

-- Comments
COMMENT ON TABLE characters IS 'EVE Online character information from ESI';
COMMENT ON COLUMN characters.character_id IS 'Unique EVE character ID';
COMMENT ON COLUMN characters.security_status IS 'Character security status (-10 to 10)';
