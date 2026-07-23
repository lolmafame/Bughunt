-- Firestore source: users/{uid}.stats map.
CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.user_stats (
    user_id text PRIMARY KEY REFERENCES bughunt.users(firebase_uid) ON DELETE CASCADE,
    max_level smallint NOT NULL DEFAULT 0 CHECK (max_level BETWEEN 0 AND 5),
    play_time_hours double precision NOT NULL DEFAULT 0 CHECK (play_time_hours >= 0),
    total_bugs_fixed bigint NOT NULL DEFAULT 0 CHECK (total_bugs_fixed >= 0),
    updated_at timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE bughunt.user_stats IS
    'Typed one-to-one projection of users.stats: max_level, play_time_hours, and total_bugs_fixed.';

CREATE OR REPLACE FUNCTION bughunt.record_user_stats(
    p_user_id text,
    p_max_level integer DEFAULT 0,
    p_play_time_hours numeric DEFAULT 0,
    p_bugs_fixed bigint DEFAULT 0
)
RETURNS bughunt.user_stats
LANGUAGE plpgsql
AS $$
DECLARE
    result bughunt.user_stats;
BEGIN
    IF p_play_time_hours < 0 OR p_bugs_fixed < 0 THEN
        RAISE EXCEPTION 'Stat increments cannot be negative';
    END IF;

    INSERT INTO bughunt.user_stats (
        user_id, max_level, play_time_hours, total_bugs_fixed
    )
    VALUES (
        p_user_id,
        p_max_level::smallint,
        p_play_time_hours::double precision,
        p_bugs_fixed
    )
    ON CONFLICT (user_id) DO UPDATE
    SET max_level = GREATEST(bughunt.user_stats.max_level, EXCLUDED.max_level),
        play_time_hours = bughunt.user_stats.play_time_hours + EXCLUDED.play_time_hours,
        total_bugs_fixed = bughunt.user_stats.total_bugs_fixed + EXCLUDED.total_bugs_fixed,
        updated_at = now()
    RETURNING * INTO result;

    RETURN result;
END;
$$;

COMMENT ON FUNCTION bughunt.record_user_stats(text, integer, numeric, bigint) IS
    'Raises the maximum reached level and atomically adds play time and fixed-bug counters.';
