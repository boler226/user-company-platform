CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE TABLE companies (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        email character varying(254) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        version bigint NOT NULL,
        is_deleted boolean NOT NULL,
        CONSTRAINT "PK_companies" PRIMARY KEY (id),
        CONSTRAINT ck_company_name CHECK (length(trim(name)) > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE TABLE tags (
        id uuid NOT NULL,
        name character varying(50) NOT NULL,
        CONSTRAINT "PK_tags" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE TABLE company_details (
        company_id uuid NOT NULL,
        address character varying(300) NOT NULL,
        website character varying(300),
        founded_year integer,
        CONSTRAINT "PK_company_details" PRIMARY KEY (company_id),
        CONSTRAINT ck_founded_year CHECK (founded_year BETWEEN 1800 AND 2100),
        CONSTRAINT "FK_company_details_companies_company_id" FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE TABLE departments (
        id uuid NOT NULL,
        company_id uuid NOT NULL,
        name character varying(100) NOT NULL,
        budget numeric(18,2) NOT NULL,
        version bigint NOT NULL,
        CONSTRAINT "PK_departments" PRIMARY KEY (id),
        CONSTRAINT ck_budget CHECK (budget > 0),
        CONSTRAINT "FK_departments_companies_company_id" FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE TABLE company_tags (
        company_id uuid NOT NULL,
        tag_id uuid NOT NULL,
        added_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT "PK_company_tags" PRIMARY KEY (company_id, tag_id),
        CONSTRAINT "FK_company_tags_companies_company_id" FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE CASCADE,
        CONSTRAINT "FK_company_tags_tags_tag_id" FOREIGN KEY (tag_id) REFERENCES tags (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    INSERT INTO tags (id, name)
    VALUES ('50000000-0000-0000-0000-000000000001', 'IT');
    INSERT INTO tags (id, name)
    VALUES ('50000000-0000-0000-0000-000000000002', 'Remote');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_companies_email" ON companies (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE INDEX "IX_companies_name" ON companies (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE INDEX "IX_company_tags_tag_id_company_id" ON company_tags (tag_id, company_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_departments_company_id_name" ON departments (company_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_tags_name" ON tags (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261120190746_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261120190746_InitialCreate', '9.0.1');
    END IF;
END $EF$;
COMMIT;

