-- Firestore source: users/{uid}.wallet map.
CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.user_wallets (
    user_id text PRIMARY KEY REFERENCES bughunt.users(firebase_uid) ON DELETE CASCADE,
    coins bigint NOT NULL DEFAULT 0 CHECK (coins >= 0),
    gems bigint NOT NULL DEFAULT 0 CHECK (gems >= 0),
    updated_at timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE bughunt.user_wallets IS
    'Typed one-to-one projection of users.wallet; nonnegative constraints prevent accidental overdrafts.';

CREATE OR REPLACE FUNCTION bughunt.adjust_wallet(
    p_user_id text,
    p_coin_delta bigint DEFAULT 0,
    p_gem_delta bigint DEFAULT 0
)
RETURNS bughunt.user_wallets
LANGUAGE plpgsql
AS $$
DECLARE
    result bughunt.user_wallets;
BEGIN
    INSERT INTO bughunt.user_wallets (user_id, coins, gems)
    VALUES (p_user_id, p_coin_delta, p_gem_delta)
    ON CONFLICT (user_id) DO UPDATE
    SET coins = bughunt.user_wallets.coins + EXCLUDED.coins,
        gems = bughunt.user_wallets.gems + EXCLUDED.gems,
        updated_at = now()
    RETURNING * INTO result;

    RETURN result;
EXCEPTION
    WHEN check_violation THEN
        RAISE EXCEPTION 'Wallet adjustment would produce a negative balance for user %', p_user_id;
END;
$$;

COMMENT ON FUNCTION bughunt.adjust_wallet(text, bigint, bigint) IS
    'Atomically applies coin and gem deltas and rejects results below zero.';
