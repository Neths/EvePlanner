-- Market History
-- Stores historical market data (daily aggregates)

CREATE TABLE IF NOT EXISTS market_history (
    type_id INTEGER NOT NULL,
    region_id INTEGER NOT NULL,
    date DATE NOT NULL,
    average DECIMAL(20, 2) NOT NULL,
    highest DECIMAL(20, 2) NOT NULL,
    lowest DECIMAL(20, 2) NOT NULL,
    volume BIGINT NOT NULL,
    order_count BIGINT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    PRIMARY KEY (type_id, region_id, date)
);

-- Indexes for time-series queries
CREATE INDEX idx_market_history_type ON market_history(type_id);
CREATE INDEX idx_market_history_region ON market_history(region_id);
CREATE INDEX idx_market_history_date ON market_history(date DESC);
CREATE INDEX idx_market_history_type_date ON market_history(type_id, date DESC);

-- Comments
COMMENT ON TABLE market_history IS 'Daily market history aggregates';
COMMENT ON COLUMN market_history.average IS 'Average price for the day';
COMMENT ON COLUMN market_history.highest IS 'Highest price for the day';
COMMENT ON COLUMN market_history.lowest IS 'Lowest price for the day';
COMMENT ON COLUMN market_history.volume IS 'Total volume traded';
COMMENT ON COLUMN market_history.order_count IS 'Number of orders';
