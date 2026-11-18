BEGIN;
INSERT INTO users(id, email, first_name, last_name) VALUES
('10000000-0000-0000-0000-000000000001', 'olena@example.test', 'Олена', 'Коваль'),
('10000000-0000-0000-0000-000000000002', 'andrii@example.test', 'Андрій', 'Мельник')
ON CONFLICT DO NOTHING;
INSERT INTO user_profiles(user_id, bio) VALUES
('10000000-0000-0000-0000-000000000001', '.NET developer'),
('10000000-0000-0000-0000-000000000002', 'QA engineer') ON CONFLICT DO NOTHING;
-- Same identifiers as Companies; snapshots come from a documented seed fixture, not a cross-DB JOIN.
INSERT INTO company_directory(id, name, source_version) VALUES
('20000000-0000-0000-0000-000000000001', 'Contoso', 1),
('20000000-0000-0000-0000-000000000002', 'Fabrikam', 1) ON CONFLICT DO NOTHING;
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM membership_applications WHERE id = '30000000-0000-0000-0000-000000000001') THEN
        CALL create_application('30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001',
            '20000000-0000-0000-0000-000000000001', 'Developer');
    END IF;
END $$;
COMMIT;
