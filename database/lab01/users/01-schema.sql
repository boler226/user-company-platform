-- Project 1: users and applications for company membership (PostgreSQL 16).
BEGIN;
CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email varchar(254) NOT NULL UNIQUE CHECK (position('@' in email) > 1),
    first_name varchar(100) NOT NULL CHECK (length(trim(first_name)) > 0),
    last_name varchar(100) NOT NULL CHECK (length(trim(last_name)) > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
CREATE TABLE IF NOT EXISTS user_profiles (
    user_id uuid PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    bio varchar(1000) NOT NULL DEFAULT '',
    phone varchar(30),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
-- Local read model; company_id has no FK to the company database.
CREATE TABLE IF NOT EXISTS company_directory (
    id uuid PRIMARY KEY,
    name varchar(100) NOT NULL CHECK (length(trim(name)) > 0),
    source_version bigint NOT NULL CHECK (source_version > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
-- M:N users <-> companies; status, requested_role and company_name_snapshot belong to the link.
CREATE TABLE IF NOT EXISTS membership_applications (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    company_id uuid NOT NULL REFERENCES company_directory(id) ON DELETE RESTRICT,
    company_name_snapshot varchar(100) NOT NULL,
    requested_role varchar(50) NOT NULL CHECK (length(trim(requested_role)) > 0),
    status varchar(30) NOT NULL DEFAULT 'Submitted'
        CHECK (status IN ('Submitted', 'AwaitingValidation', 'Approved', 'Active', 'Cancelled')),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
CREATE TABLE IF NOT EXISTS application_status_history (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    application_id uuid NOT NULL REFERENCES membership_applications(id) ON DELETE RESTRICT,
    old_status varchar(30) CHECK (old_status IN ('Submitted', 'AwaitingValidation', 'Approved', 'Active', 'Cancelled')),
    new_status varchar(30) NOT NULL CHECK (new_status IN ('Submitted', 'AwaitingValidation', 'Approved', 'Active', 'Cancelled')),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user
);
-- Prevent duplicate current applications; cancelled/deleted applications remain as history.
CREATE UNIQUE INDEX IF NOT EXISTS ux_application_current ON membership_applications(user_id, company_id)
    WHERE NOT is_deleted AND status <> 'Cancelled';
-- User's applications by status, ordered newest first, with deterministic pagination.
CREATE INDEX IF NOT EXISTS ix_application_user_status ON membership_applications(user_id, status,
    created_at DESC, id)
    INCLUDE (company_id, requested_role) WHERE NOT is_deleted;
-- Company administrator's queue (also supports the local company FK).
CREATE INDEX IF NOT EXISTS ix_application_company ON membership_applications(company_id, status) WHERE NOT is_deleted;
-- Chronological history of one application (also supports its FK).
CREATE INDEX IF NOT EXISTS ix_history_application ON application_status_history(application_id,
    created_at, id);

CREATE OR REPLACE FUNCTION set_audit_version() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.created_at := OLD.created_at; NEW.created_by := OLD.created_by;
    NEW.updated_at := clock_timestamp(); NEW.updated_by := session_user;
    NEW.version := OLD.version + 1;
    RETURN NEW;
END $$;
DO $$ DECLARE t text; BEGIN
    FOREACH t IN ARRAY ARRAY['users','user_profiles','company_directory','membership_applications'] LOOP
        EXECUTE format('CREATE OR REPLACE TRIGGER audit_version BEFORE UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION set_audit_version()', t);
    END LOOP;
END $$;
COMMIT;
