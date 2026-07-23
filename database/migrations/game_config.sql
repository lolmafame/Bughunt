CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.game_config (
    config_key text PRIMARY KEY,
    enemy_id text NOT NULL,
    base_move_speed double precision NOT NULL CHECK (base_move_speed >= 0),
    detection_radius double precision NOT NULL CHECK (detection_radius >= 0),
    max_speed_cap double precision NOT NULL CHECK (max_speed_cap >= base_move_speed),
    speed_growth_per_second double precision NOT NULL CHECK (speed_growth_per_second >= 0),
    description text,
    updated_at timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE bughunt.game_config IS
    'Typed runtime configuration migrated from Firestore game_config/{configKey}; the live key is global_settings.';

CREATE OR REPLACE FUNCTION bughunt.get_game_config(p_config_key text DEFAULT 'global_settings')
RETURNS SETOF bughunt.game_config
LANGUAGE sql
STABLE
AS $$
    SELECT gc.* FROM bughunt.game_config gc WHERE gc.config_key = p_config_key;
$$;

COMMENT ON FUNCTION bughunt.get_game_config(text) IS
    'Returns one typed game-configuration record by key without exposing unrelated tables.';
