-- ESI OAuth Applications
-- Stores OAuth application credentials registered on developers.eveonline.com

CREATE TABLE IF NOT EXISTS esi_applications (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    client_id VARCHAR(255) NOT NULL UNIQUE,
    client_secret TEXT NOT NULL,
    callback_url TEXT NOT NULL,
    scopes TEXT[] NOT NULL DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Index for quick lookup by client_id
CREATE INDEX idx_esi_applications_client_id ON esi_applications(client_id);

-- Comments
COMMENT ON TABLE esi_applications IS 'OAuth applications registered with EVE Online ESI';
COMMENT ON COLUMN esi_applications.client_id IS 'OAuth Client ID from developers.eveonline.com';
COMMENT ON COLUMN esi_applications.client_secret IS 'OAuth Client Secret (encrypted in production)';
COMMENT ON COLUMN esi_applications.callback_url IS 'OAuth callback URL';
COMMENT ON COLUMN esi_applications.scopes IS 'Array of ESI scopes this application can request';
