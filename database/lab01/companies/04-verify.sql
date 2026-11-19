BEGIN;
DO $$ DECLARE c uuid := gen_random_uuid(); d uuid := gen_random_uuid(); affected integer; BEGIN
    INSERT INTO companies(id, name, email) VALUES(c, 'Test company', c || '@example.test');
    INSERT INTO company_details(company_id, address) VALUES(c, 'Test address');
    BEGIN
        INSERT INTO company_details(company_id, address) VALUES(c, 'Duplicate details');
        RAISE EXCEPTION 'Expected 1:1 uniqueness';
    EXCEPTION WHEN unique_violation THEN NULL;
    END;
    INSERT INTO departments(id, company_id, name, budget) VALUES(d, c, 'Test department', 100);
    BEGIN
        UPDATE departments SET budget = -1 WHERE id = d;
        RAISE EXCEPTION 'Expected CHECK violation';
    EXCEPTION WHEN check_violation THEN NULL;
    END;
    INSERT INTO company_tags(company_id, tag_id) VALUES(c, '50000000-0000-0000-0000-000000000001');
    UPDATE companies SET name = 'Updated company' WHERE id = c AND version = 1;
    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected <> 1 THEN RAISE EXCEPTION 'Update failed'; END IF;
    UPDATE companies SET name = 'Stale edit' WHERE id = c AND version = 1;
    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected <> 0 THEN RAISE EXCEPTION 'Stale update was accepted'; END IF;
    IF (SELECT version FROM companies WHERE id = c) <> 2 THEN RAISE EXCEPTION 'Version trigger failed'; END IF;
    BEGIN
        DELETE FROM companies WHERE id = c;
        RAISE EXCEPTION 'Expected department protection';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;
    DELETE FROM departments WHERE id = d;
    DELETE FROM companies WHERE id = c;
    IF EXISTS (SELECT 1 FROM company_details WHERE company_id = c)
        OR EXISTS (SELECT 1 FROM company_tags WHERE company_id = c) THEN RAISE EXCEPTION 'Cascade failed'; END IF;
END $$;
SELECT id, name FROM companies WHERE NOT is_deleted AND lower(name) LIKE 'con%' ORDER BY name, id LIMIT 10 OFFSET 0;
SELECT c.id, c.name, ct.added_at FROM companies c JOIN company_tags ct ON ct.company_id = c.id
WHERE ct.tag_id = '50000000-0000-0000-0000-000000000001' AND NOT ct.is_deleted AND NOT c.is_deleted;
SELECT conrelid::regclass AS child, confrelid::regclass AS parent FROM pg_constraint WHERE contype = 'f';
ROLLBACK;
