-- Market Orders
-- Stores active buy/sell orders from EVE Online markets

CREATE TABLE IF NOT EXISTS market_orders (
    order_id BIGINT PRIMARY KEY,
    type_id INTEGER NOT NULL,
    region_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    system_id INTEGER NOT NULL,
    is_buy_order BOOLEAN NOT NULL,
    price DECIMAL(20, 2) NOT NULL,
    volume_remain INTEGER NOT NULL,
    volume_total INTEGER NOT NULL,
    min_volume INTEGER NOT NULL DEFAULT 1,
    duration INTEGER NOT NULL,
    issued TIMESTAMP WITH TIME ZONE NOT NULL,
    range VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for common queries
CREATE INDEX idx_market_orders_type ON market_orders(type_id);
CREATE INDEX idx_market_orders_region ON market_orders(region_id);
CREATE INDEX idx_market_orders_location ON market_orders(location_id);
CREATE INDEX idx_market_orders_type_region ON market_orders(type_id, region_id);
CREATE INDEX idx_market_orders_is_buy ON market_orders(is_buy_order);
CREATE INDEX idx_market_orders_issued ON market_orders(issued DESC);

-- Comments
COMMENT ON TABLE market_orders IS 'Active market orders from EVE Online';
COMMENT ON COLUMN market_orders.order_id IS 'Unique order ID from ESI';
COMMENT ON COLUMN market_orders.type_id IS 'Item type ID';
COMMENT ON COLUMN market_orders.region_id IS 'Region where order is placed';
COMMENT ON COLUMN market_orders.is_buy_order IS 'True for buy orders, false for sell orders';
COMMENT ON COLUMN market_orders.price IS 'Price per unit in ISK';
COMMENT ON COLUMN market_orders.volume_remain IS 'Remaining quantity';
COMMENT ON COLUMN market_orders.range IS 'Order range (station, system, region, etc.)';
