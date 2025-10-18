-- Migration: 002_Universe_Groups
-- Description: Create groups table for EVE item groups
-- Phase: 1 (Universe Static Data)

CREATE TABLE IF NOT EXISTS groups (
    group_id INTEGER PRIMARY KEY,
    category_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (category_id) REFERENCES categories(category_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_groups_category ON groups(category_id);
CREATE INDEX IF NOT EXISTS idx_groups_name ON groups(name);
CREATE INDEX IF NOT EXISTS idx_groups_published ON groups(published);

-- Comments
COMMENT ON TABLE groups IS 'EVE Online item groups from ESI';
COMMENT ON COLUMN groups.group_id IS 'Unique group ID from ESI';
COMMENT ON COLUMN groups.category_id IS 'Parent category ID';
COMMENT ON COLUMN groups.name IS 'Group name (e.g., Frigate, Mining Laser, etc.)';
COMMENT ON COLUMN groups.published IS 'Whether this group is published/visible';
