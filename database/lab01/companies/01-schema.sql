-- Project 2: five entities, ready for EF Core mapping in lab 03.
BEGIN;
CREATE TABLE IF NOT EXISTS companies (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL CHECK (length(trim(name)) > 0),
    email varchar(254) NOT NULL UNIQUE CHECK (position('@' in email) > 1),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
CREATE TABLE IF NOT EXISTS company_details (
    company_id uuid PRIMARY KEY REFERENCES companies(id) ON DELETE CASCADE,
    address varchar(300) NOT NULL,
    website varchar(300),
    founded_year integer CHECK (founded_year BETWEEN 1800 AND 2100),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
CREATE TABLE IF NOT EXISTS departments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id uuid NOT NULL REFERENCES companies(id) ON DELETE RESTRICT,
    name varchar(100) NOT NULL CHECK (length(trim(name)) > 0),
    budget numeric(14,2) NOT NULL DEFAULT 1000 CHECK (budget > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE(company_id, name)
);
CREATE TABLE IF NOT EXISTS tags (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(50) NOT NULL UNIQUE CHECK (length(trim(name)) > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
CREATE TABLE IF NOT EXISTS company_tags (
    company_id uuid NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    tag_id uuid NOT NULL REFERENCES tags(id) ON DELETE RESTRICT,
    added_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by text NOT NULL DEFAULT session_user,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT session_user,
    is_deleted boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    PRIMARY KEY(company_id, tag_id)
);
-- Prefix search by lower(name); partial index excludes archived companies.
CREATE INDEX IF NOT EXISTS ix_companies_name ON companies(lower(name) text_pattern_ops) WHERE NOT is_deleted;
-- Reverse lookup: companies carrying a given tag; PK already covers company -> tags.
CREATE INDEX IF NOT EXISTS ix_company_tags_tag ON company_tags(tag_id, company_id) WHERE NOT is_deleted;
-- PK/UNIQUE indexes cover identifier/email lookups, company departments and tag name lookup.
CREATE OR REPLACE FUNCTION set_audit_version() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.created_at := OLD.created_at; NEW.created_by := OLD.created_by;
    NEW.updated_at := clock_timestamp(); NEW.updated_by := session_user;
    NEW.version := OLD.version + 1;
    RETURN NEW;
END $$;
DO $$ DECLARE t text; BEGIN
    FOREACH t IN ARRAY ARRAY['companies','company_details','departments','tags','company_tags'] LOOP
        EXECUTE format('CREATE OR REPLACE TRIGGER audit_version BEFORE UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION set_audit_version()', t);
    END LOOP;
END $$;
COMMIT;
