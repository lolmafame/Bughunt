CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.user_achievements (
    user_id text NOT NULL REFERENCES bughunt.users(firebase_uid) ON DELETE CASCADE,
    achievement_id text NOT NULL CHECK (length(btrim(achievement_id)) > 0),
    unlocked boolean NOT NULL DEFAULT true,
    unlocked_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, achievement_id)
);

CREATE INDEX IF NOT EXISTS user_achievements_achievement_idx
    ON bughunt.user_achievements (achievement_id, unlocked);

COMMENT ON TABLE bughunt.user_achievements IS
    'Normalized rows from Firestore user_achievements/{uid}.unlocked maps; current live maps are empty, so achievement IDs come from truthy keys during export.';

CREATE OR REPLACE FUNCTION bughunt.unlock_achievement(
    p_user_id text,
    p_achievement_id text
)
RETURNS boolean
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO bughunt.user_achievements (user_id, achievement_id, unlocked)
    VALUES (p_user_id, p_achievement_id, true)
    ON CONFLICT (user_id, achievement_id) DO UPDATE
    SET unlocked = true;

    RETURN FOUND;
END;
$$;

COMMENT ON FUNCTION bughunt.unlock_achievement(text, text) IS
    'Idempotently records an achievement unlock for a user.';
