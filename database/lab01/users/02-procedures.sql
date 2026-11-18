-- Exactly five procedures. Application login gets EXECUTE + SELECT, no direct DML.
-- SECURITY DEFINER is restricted to a fixed search_path; all names are qualified.
BEGIN;
CREATE OR REPLACE PROCEDURE create_application(
    p_id uuid, p_user_id uuid, p_company_id uuid, p_role varchar)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public AS $$
DECLARE v_name varchar(100);
BEGIN
    PERFORM 1 FROM public.users WHERE id = p_user_id AND NOT is_deleted FOR SHARE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0002', MESSAGE = 'User not found'; END IF;
    SELECT name INTO v_name FROM public.company_directory WHERE id = p_company_id AND NOT is_deleted FOR SHARE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0002', MESSAGE = 'Company not found'; END IF;
    INSERT INTO public.membership_applications(id, user_id, company_id, company_name_snapshot, requested_role,
    created_by)
        VALUES (p_id, p_user_id, p_company_id, v_name, p_role, session_user);
    INSERT INTO public.application_status_history(application_id, new_status,
    created_by)
        VALUES (p_id, 'Submitted', session_user);
END $$;

CREATE OR REPLACE PROCEDURE get_application(p_id uuid, INOUT p_result jsonb)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public AS $$
BEGIN
    SELECT to_jsonb(a) INTO p_result FROM public.membership_applications a WHERE id = p_id AND NOT is_deleted;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0002', MESSAGE = 'Application not found'; END IF;
END $$;

CREATE OR REPLACE PROCEDURE update_application(p_id uuid, p_role varchar, p_expected_version bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public AS $$
DECLARE a public.membership_applications;
BEGIN
    SELECT * INTO a FROM public.membership_applications WHERE id = p_id AND NOT is_deleted FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0002', MESSAGE = 'Application not found'; END IF;
    IF p_expected_version IS NULL OR a.version <> p_expected_version THEN RAISE EXCEPTION USING ERRCODE = '40001', MESSAGE = 'Stale application version'; END IF;
    IF a.status <> 'Submitted' THEN RAISE EXCEPTION USING ERRCODE = 'P0001', MESSAGE = 'Only submitted applications can be edited'; END IF;
    UPDATE public.membership_applications SET requested_role = p_role WHERE id = p_id;
END $$;

CREATE OR REPLACE PROCEDURE delete_application(p_id uuid, p_expected_version bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public AS $$
DECLARE a public.membership_applications;
BEGIN
    SELECT * INTO a FROM public.membership_applications WHERE id = p_id AND NOT is_deleted FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0002', MESSAGE = 'Application not found'; END IF;
    IF p_expected_version IS NULL OR a.version <> p_expected_version THEN RAISE EXCEPTION USING ERRCODE = '40001', MESSAGE = 'Stale application version'; END IF;
    IF a.status <> 'Cancelled' THEN RAISE EXCEPTION USING ERRCODE = 'P0001', MESSAGE = 'Cancel the application before deleting it'; END IF;
    UPDATE public.membership_applications SET is_deleted = true WHERE id = p_id;
END $$;

CREATE OR REPLACE PROCEDURE change_application_status(p_id uuid, p_status varchar, p_expected_version bigint)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public AS $$
DECLARE a public.membership_applications;
BEGIN
    -- CALL is atomic in PostgreSQL. This lock lasts until its caller's transaction ends.
    SELECT * INTO a FROM public.membership_applications WHERE id = p_id AND NOT is_deleted FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION USING ERRCODE = 'P0002', MESSAGE = 'Application not found'; END IF;
    IF p_expected_version IS NULL OR a.version <> p_expected_version THEN RAISE EXCEPTION USING ERRCODE = '40001', MESSAGE = 'Stale application version'; END IF;
    IF p_status IS NULL OR NOT (
        (a.status = 'Submitted' AND p_status = 'AwaitingValidation') OR
        (a.status = 'AwaitingValidation' AND p_status = 'Approved') OR
        (a.status = 'Approved' AND p_status = 'Active') OR
        (a.status IN ('Submitted','AwaitingValidation','Approved') AND p_status = 'Cancelled')
    ) THEN
        RAISE EXCEPTION USING ERRCODE = 'P0001', MESSAGE = 'Invalid membership status transition';
    END IF;
    UPDATE public.membership_applications SET status = p_status WHERE id = p_id;
    INSERT INTO public.application_status_history(application_id, old_status, new_status,
    created_by)
        VALUES (p_id, a.status, p_status, session_user);
END $$;
-- Inside SECURITY DEFINER, session_user identifies the connecting service login.
CREATE OR REPLACE FUNCTION set_audit_version() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.created_at := OLD.created_at; NEW.created_by := OLD.created_by;
    NEW.updated_at := clock_timestamp(); NEW.updated_by := session_user;
    NEW.version := OLD.version + 1;
    RETURN NEW;
END $$;
REVOKE ALL ON ALL ROUTINES IN SCHEMA public FROM PUBLIC;
COMMIT;
