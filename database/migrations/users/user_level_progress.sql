-- Firestore source: dynamic progress fields on users/{uid}.
CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.user_level_progress (
    user_id text NOT NULL REFERENCES bughunt.users(firebase_uid) ON DELETE CASCADE,
    language text NOT NULL
        CHECK (language IN ('python', 'javascript', 'csharp', 'java', 'cplusplus')),
    level_number smallint NOT NULL CHECK (level_number BETWEEN 0 AND 5),
    completed boolean NOT NULL DEFAULT false,
    best_time_seconds double precision CHECK (best_time_seconds >= 0),
    completed_at timestamptz,
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, language, level_number),
    CHECK (level_number <> 0 OR best_time_seconds IS NULL),
    CHECK (completed OR completed_at IS NULL)
);

CREATE INDEX IF NOT EXISTS user_level_progress_language_idx
    ON bughunt.user_level_progress (language, level_number, completed);

COMMENT ON TABLE bughunt.user_level_progress IS
    'Normalized replacement for dynamic users.{language}_tutorial_completed, levelN_completed, levelN_best_time, and all_levels_completed fields; level 0 is the tutorial.';

CREATE OR REPLACE VIEW bughunt.user_language_completion AS
SELECT
    user_id,
    language,
    count(*) FILTER (WHERE level_number BETWEEN 1 AND 5 AND completed) AS completed_levels,
    count(*) FILTER (WHERE level_number BETWEEN 1 AND 5 AND completed) = 5 AS all_levels_completed
FROM bughunt.user_level_progress
GROUP BY user_id, language;

COMMENT ON VIEW bughunt.user_language_completion IS
    'Derived per-language completion summary; replaces redundant Firestore all_levels_completed flags.';

CREATE OR REPLACE FUNCTION bughunt.record_level_completion(
    p_user_id text,
    p_language text,
    p_level_number integer,
    p_best_time_seconds numeric DEFAULT NULL
)
RETURNS bughunt.user_level_progress
LANGUAGE plpgsql
AS $$
DECLARE
    normalized_language text := lower(btrim(p_language));
    result bughunt.user_level_progress;
BEGIN
    IF p_level_number = 0 AND p_best_time_seconds IS NOT NULL THEN
        RAISE EXCEPTION 'Tutorial completion does not accept a best time';
    END IF;

    INSERT INTO bughunt.user_level_progress (
        user_id, language, level_number, completed,
        best_time_seconds, completed_at
    )
    VALUES (
        p_user_id, normalized_language, p_level_number::smallint, true,
        p_best_time_seconds::double precision, now()
    )
    ON CONFLICT (user_id, language, level_number) DO UPDATE
    SET completed = true,
        best_time_seconds = CASE
            WHEN EXCLUDED.best_time_seconds IS NULL
                THEN bughunt.user_level_progress.best_time_seconds
            WHEN bughunt.user_level_progress.best_time_seconds IS NULL
                THEN EXCLUDED.best_time_seconds
            ELSE LEAST(
                bughunt.user_level_progress.best_time_seconds,
                EXCLUDED.best_time_seconds
            )
        END,
        completed_at = COALESCE(bughunt.user_level_progress.completed_at, now()),
        updated_at = now()
    RETURNING * INTO result;

    RETURN result;
END;
$$;

COMMENT ON FUNCTION bughunt.record_level_completion(text, text, integer, numeric) IS
    'Idempotently marks a tutorial/level complete and retains the lowest submitted best time for regular levels.';
