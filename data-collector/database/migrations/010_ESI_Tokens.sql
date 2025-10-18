-- ESI OAuth Tokens
-- Stores access and refresh tokens for authenticated ESI requests

CREATE TABLE IF NOT EXISTS esi_tokens (
    id SERIAL PRIMARY KEY,
    application_id INTEGER NOT NULL REFERENCES esi_applications(id) ON DELETE CASCADE,
    character_id BIGINT NOT NULL REFERENCES characters(character_id) ON DELETE CASCADE,
    access_token TEXT NOT NULL,
    refresh_token TEXT NOT NULL,
    token_type VARCHAR(50) NOT NULL DEFAULT 'Bearer',
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    scopes TEXT[] NOT NULL DEFAULT '{}',
    is_valid BOOLEAN NOT NULL DEFAULT true,
    last_refreshed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(application_id, character_id)
);

-- Indexes
CREATE INDEX idx_esi_tokens_character ON esi_tokens(character_id);
CREATE INDEX idx_esi_tokens_expires ON esi_tokens(expires_at) WHERE is_valid = true;
CREATE INDEX idx_esi_tokens_valid ON esi_tokens(is_valid);

-- Comments
COMMENT ON TABLE esi_tokens IS 'OAuth tokens for ESI authenticated requests';
COMMENT ON COLUMN esi_tokens.access_token IS 'JWT access token (20 min lifetime)';
COMMENT ON COLUMN esi_tokens.refresh_token IS 'Long-lived refresh token';
COMMENT ON COLUMN esi_tokens.expires_at IS 'When the access token expires';
COMMENT ON COLUMN esi_tokens.is_valid IS 'False if token was revoked or failed to refresh';
