-- All verification changes are rolled back. Any failed assertion aborts the script.
BEGIN;
DO $$
DECLARE
    u uuid := gen_random_uuid(); a uuid := gen_random_uuid(); result jsonb; n bigint;
BEGIN
    INSERT INTO users(id, email, first_name, last_name) VALUES (u, u || '@example.test', 'Test', 'User');
    CALL create_application(a, u, '20000000-0000-0000-0000-000000000001', 'Developer');
    CALL get_application(a, result);
    IF result->>'status' <> 'Submitted' THEN RAISE EXCEPTION 'Create/read failed'; END IF;
    CALL update_application(a, 'QA', 1);
    BEGIN
        CALL change_application_status(a, 'AwaitingValidation', NULL);
        RAISE EXCEPTION 'NULL version bypassed concurrency control';
    EXCEPTION WHEN serialization_failure THEN NULL;
    END;
    BEGIN
        CALL update_application(a, 'Stale edit', 1);
        RAISE EXCEPTION 'Expected version conflict';
    EXCEPTION WHEN serialization_failure THEN NULL;
    END;
    BEGIN
        CALL change_application_status(a, 'Active', 2);
        RAISE EXCEPTION 'Expected invalid transition' USING ERRCODE = 'XX000';
    EXCEPTION WHEN SQLSTATE 'P0001' THEN NULL;
    END;
    SELECT count(*) INTO n FROM application_status_history WHERE application_id = a;
    IF n <> 1 OR (SELECT status FROM membership_applications WHERE id = a) <> 'Submitted'
        OR (SELECT version FROM membership_applications WHERE id = a) <> 2 THEN
        RAISE EXCEPTION 'Rejected transition changed the aggregate';
    END IF;
    CALL change_application_status(a, 'AwaitingValidation', 2);
    CALL change_application_status(a, 'Approved', 3);
    CALL change_application_status(a, 'Active', 4);
    BEGIN
        CALL change_application_status(a, 'Cancelled', 5);
        RAISE EXCEPTION 'Expected cancellation refusal' USING ERRCODE = 'XX000';
    EXCEPTION WHEN SQLSTATE 'P0001' THEN NULL;
    END;
    SELECT count(*) INTO n FROM application_status_history WHERE application_id = a;
    IF n <> 4 THEN RAISE EXCEPTION 'Status history is incomplete'; END IF;
    a := gen_random_uuid();
    CALL create_application(a, u, '20000000-0000-0000-0000-000000000002', 'QA');
    CALL change_application_status(a, 'Cancelled', 1);
    CALL delete_application(a, 2);
    IF NOT (SELECT is_deleted FROM membership_applications WHERE id = a) THEN RAISE EXCEPTION 'Soft delete failed'; END IF;
    BEGIN
        CALL get_application(a, result);
        RAISE EXCEPTION 'Deleted record was returned';
    EXCEPTION WHEN SQLSTATE 'P0002' THEN NULL;
    END;
    BEGIN
        DELETE FROM users WHERE id = u;
        RAISE EXCEPTION 'Expected history protection';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;
END $$;
-- Indexed search + pagination. Pass a real user/status/page from the application.
SELECT id, company_id, requested_role, status FROM membership_applications
WHERE user_id = '10000000-0000-0000-0000-000000000001' AND status = 'Submitted' AND NOT is_deleted
ORDER BY created_at DESC, id LIMIT 10 OFFSET 0;
-- All referenced tables belong to this database; PostgreSQL cannot create cross-database FKs.
SELECT conrelid::regclass AS child, confrelid::regclass AS parent FROM pg_constraint WHERE contype = 'f';
ROLLBACK;
