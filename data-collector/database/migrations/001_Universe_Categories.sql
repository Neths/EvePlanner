-- Migration: 001_Universe_Categories
-- Description: Create categories table for EVE item categories
-- Phase: 1 (Universe Static Data)

CREATE TABLE IF NOT EXISTS categories (
    category_id INTEGER PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_categories_name ON categories(name);
CREATE INDEX IF NOT EXISTS idx_categories_published ON categories(published);

-- Comments
COMMENT ON TABLE categories IS 'EVE Online item categories from ESI';
COMMENT ON COLUMN categories.category_id IS 'Unique category ID from ESI';
COMMENT ON COLUMN categories.name IS 'Category name (e.g., Ship, Module, etc.)';
COMMENT ON COLUMN categories.published IS 'Whether this category is published/visible';
