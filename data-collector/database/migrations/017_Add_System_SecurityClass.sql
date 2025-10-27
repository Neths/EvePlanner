-- Migration: 017_Add_System_SecurityClass
-- Description: Add security_class column to systems table
-- Phase: 1 (Universe Static Data - Fix)

-- Add security_class column to systems table
ALTER TABLE systems
ADD COLUMN IF NOT EXISTS security_class VARCHAR(50);

-- Add index for security_class
CREATE INDEX IF NOT EXISTS idx_systems_security_class ON systems(security_class);

-- Comment
COMMENT ON COLUMN systems.security_class IS 'Security classification (e.g., A, B, C, D, E, F, G, H for high/low/null sec)';
