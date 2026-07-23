-- Firestore source: users/{uid}.inventory array.
CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.user_inventory (
    user_id text NOT NULL REFERENCES bughunt.users(firebase_uid) ON DELETE CASCADE,
    item_id text NOT NULL,
    source text NOT NULL DEFAULT 'migration',
    acquired_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, item_id)
);

COMMENT ON TABLE bughunt.user_inventory IS
    'Normalized rows from the Firestore users.inventory string array; duplicate items collapse by primary key.';

CREATE OR REPLACE FUNCTION bughunt.grant_inventory_item(
    p_user_id text,
    p_item_id text,
    p_source text DEFAULT 'system'
)
RETURNS boolean
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO bughunt.user_inventory (user_id, item_id, source)
    VALUES (p_user_id, p_item_id, p_source)
    ON CONFLICT (user_id, item_id) DO NOTHING;

    RETURN FOUND;
END;
$$;

COMMENT ON FUNCTION bughunt.grant_inventory_item(text, text, text) IS
    'Idempotently adds an item to a user inventory and returns true only when a row was inserted.';
