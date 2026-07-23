CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.usernames (
    username text PRIMARY KEY CHECK (username = lower(btrim(username)) AND length(username) > 0),
    user_id text NOT NULL UNIQUE REFERENCES bughunt.users(firebase_uid) ON DELETE CASCADE,
    legacy_email text,
    claimed_at timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE bughunt.usernames IS
    'Case-normalized username reservations migrated from Firestore usernames/{username}; uid becomes user_id.';

CREATE OR REPLACE FUNCTION bughunt.claim_username(
    p_user_id text,
    p_username text,
    p_email text DEFAULT NULL
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    normalized text := lower(btrim(p_username));
BEGIN
    IF normalized = '' THEN
        RAISE EXCEPTION 'Username cannot be empty';
    END IF;

    INSERT INTO bughunt.usernames (username, user_id, legacy_email)
    VALUES (normalized, p_user_id, p_email)
    ON CONFLICT DO NOTHING;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Username is already claimed or this user already has a username';
    END IF;

    UPDATE bughunt.users
    SET username = normalized,
        email = COALESCE(nullif(btrim(p_email), ''), email)
    WHERE firebase_uid = p_user_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Unknown user: %', p_user_id;
    END IF;
END;
$$;

COMMENT ON FUNCTION bughunt.claim_username(text, text, text) IS
    'Atomically reserves a lowercase username and mirrors it onto the referenced user profile.';
