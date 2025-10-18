-- Market Prices
-- Stores averaged/adjusted market prices from ESI

CREATE TABLE IF NOT EXISTS market_prices (
    type_id INTEGER PRIMARY KEY,
    adjusted_price DECIMAL(20, 2),
    average_price DECIMAL(20, 2),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Comments
COMMENT ON TABLE market_prices IS 'Global market prices from ESI (CCP calculated)';
COMMENT ON COLUMN market_prices.adjusted_price IS 'Adjusted price used for industry calculations';
COMMENT ON COLUMN market_prices.average_price IS 'Average market price';
