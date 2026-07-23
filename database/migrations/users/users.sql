-- Firestore source: users/{uid} profile fields.
CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.users (
    firebase_uid text PRIMARY KEY CHECK (length(btrim(firebase_uid)) > 0),
    email text,
    username text,
    display_name text,
    user_type text NOT NULL DEFAULT 'guest'
        CHECK (user_type IN ('guest', 'google', 'email', 'admin')),
    agreed_to_terms boolean NOT NULL DEFAULT false,
    is_premium boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    last_login_at timestamptz,
    legacy_created_at text,
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS users_email_ci_uq
    ON bughunt.users (lower(email)) WHERE email IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS users_username_ci_uq
    ON bughunt.users (lower(username)) WHERE username IS NOT NULL;

COMMENT ON TABLE bughunt.users IS
    'Core user profiles migrated from Firestore users/{uid}; the primary key remains the Firebase Authentication UID.';
COMMENT ON COLUMN bughunt.users.legacy_created_at IS
    'Original users.created_at string retained when it cannot be safely converted to timestamptz.';

CREATE OR REPLACE FUNCTION bughunt.touch_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS users_touch_updated_at ON bughunt.users;
CREATE TRIGGER users_touch_updated_at
BEFORE UPDATE ON bughunt.users
FOR EACH ROW EXECUTE FUNCTION bughunt.touch_updated_at();

CREATE OR REPLACE FUNCTION bughunt.upsert_user_profile(
    p_firebase_uid text,
    p_email text DEFAULT NULL,
    p_username text DEFAULT NULL,
    p_display_name text DEFAULT NULL,
    p_user_type text DEFAULT 'guest',
    p_agreed_to_terms boolean DEFAULT false
)
RETURNS bughunt.users
LANGUAGE plpgsql
AS $$
DECLARE
    result bughunt.users;
BEGIN
    INSERT INTO bughunt.users (
        firebase_uid, email, username, display_name, user_type,
        agreed_to_terms, last_login_at
    )
    VALUES (
        p_firebase_uid, nullif(btrim(p_email), ''), nullif(lower(btrim(p_username)), ''),
        nullif(btrim(p_display_name), ''), p_user_type, p_agreed_to_terms, now()
    )
    ON CONFLICT (firebase_uid) DO UPDATE
    SET email = COALESCE(EXCLUDED.email, bughunt.users.email),
        username = COALESCE(EXCLUDED.username, bughunt.users.username),
        display_name = COALESCE(EXCLUDED.display_name, bughunt.users.display_name),
        user_type = EXCLUDED.user_type,
        agreed_to_terms = bughunt.users.agreed_to_terms OR EXCLUDED.agreed_to_terms,
        last_login_at = now()
    RETURNING * INTO result;

    RETURN result;
END;
$$;

COMMENT ON FUNCTION bughunt.upsert_user_profile(text, text, text, text, text, boolean) IS
    'Creates a user or refreshes mutable profile fields and last-login time without replacing entitlement data.';
