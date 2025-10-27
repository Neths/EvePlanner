-- Character Wallet
-- Stores character wallet balance and transaction history

CREATE TABLE IF NOT EXISTS character_wallet (
    character_id BIGINT PRIMARY KEY REFERENCES characters(character_id) ON DELETE CASCADE,
    balance DECIMAL(20, 2) NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS character_wallet_journal (
    id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id) ON DELETE CASCADE,
    date TIMESTAMP WITH TIME ZONE NOT NULL,
    ref_type VARCHAR(100) NOT NULL,
    first_party_id BIGINT,
    second_party_id BIGINT,
    amount DECIMAL(20, 2) NOT NULL,
    balance DECIMAL(20, 2) NOT NULL,
    reason TEXT,
    tax DECIMAL(20, 2),
    tax_receiver_id BIGINT,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS character_wallet_transactions (
    transaction_id BIGINT PRIMARY KEY,
    character_id BIGINT NOT NULL REFERENCES characters(character_id) ON DELETE CASCADE,
    date TIMESTAMP WITH TIME ZONE NOT NULL,
    type_id INTEGER NOT NULL,
    location_id BIGINT NOT NULL,
    quantity BIGINT NOT NULL,
    unit_price DECIMAL(20, 2) NOT NULL,
    client_id BIGINT NOT NULL,
    is_buy BOOLEAN NOT NULL,
    is_personal BOOLEAN NOT NULL,
    journal_ref_id BIGINT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_character_wallet_journal_character ON character_wallet_journal(character_id);
CREATE INDEX idx_character_wallet_journal_date ON character_wallet_journal(date DESC);
CREATE INDEX idx_character_wallet_transactions_character ON character_wallet_transactions(character_id);
CREATE INDEX idx_character_wallet_transactions_date ON character_wallet_transactions(date DESC);
CREATE INDEX idx_character_wallet_transactions_type ON character_wallet_transactions(type_id);

-- Comments
COMMENT ON TABLE character_wallet IS 'Character wallet balance';
COMMENT ON TABLE character_wallet_journal IS 'Character wallet journal entries';
COMMENT ON TABLE character_wallet_transactions IS 'Character market transactions';
