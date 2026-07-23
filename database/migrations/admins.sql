CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.admins (
    principal text PRIMARY KEY CHECK (length(btrim(principal)) > 0),
    display_name text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE bughunt.admins IS
    'Administrative principals migrated from Firestore admins/{documentId}; live document IDs do not match users.firebase_uid.';

CREATE OR REPLACE FUNCTION bughunt.is_admin(p_principal text)
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1 FROM bughunt.admins a
        WHERE a.principal = p_principal
    );
$$;

COMMENT ON FUNCTION bughunt.is_admin(text) IS
    'Returns true when the exact authenticated principal is registered as an administrator.';
