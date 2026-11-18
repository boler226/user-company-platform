-- Additional fields required by the existing registration/login API.
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS role integer NOT NULL DEFAULT 0;
ALTER TABLE users ADD COLUMN IF NOT EXISTS company_id uuid;
-- company_id is a logical external identifier, never a cross-database FK.
