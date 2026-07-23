-- Firestore source: users/{uid}.purchases map.
CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.user_purchases (
    user_id text NOT NULL REFERENCES bughunt.users(firebase_uid) ON DELETE CASCADE,
    item_id text NOT NULL,
    payment_id text REFERENCES bughunt.payments(order_id) ON DELETE SET NULL,
    is_active boolean NOT NULL DEFAULT true,
    purchased_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, item_id)
);

CREATE INDEX IF NOT EXISTS user_purchases_payment_idx
    ON bughunt.user_purchases (payment_id) WHERE payment_id IS NOT NULL;

COMMENT ON TABLE bughunt.user_purchases IS
    'Normalized truthy entries from users.purchases; item IDs are retained because current payment IDs do not match the live products document IDs.';

CREATE OR REPLACE FUNCTION bughunt.grant_purchase(
    p_user_id text,
    p_item_id text,
    p_payment_id text DEFAULT NULL
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO bughunt.user_purchases (user_id, item_id, payment_id, is_active)
    VALUES (p_user_id, p_item_id, p_payment_id, true)
    ON CONFLICT (user_id, item_id) DO UPDATE
    SET payment_id = COALESCE(EXCLUDED.payment_id, bughunt.user_purchases.payment_id),
        is_active = true;

    IF p_item_id = 'premium_plan' THEN
        UPDATE bughunt.users SET is_premium = true WHERE firebase_uid = p_user_id;
    ELSE
        PERFORM bughunt.grant_inventory_item(p_user_id, p_item_id, 'purchase');
    END IF;
END;
$$;

COMMENT ON FUNCTION bughunt.grant_purchase(text, text, text) IS
    'Idempotently grants a purchased entitlement; premium_plan updates account tier and other items enter inventory.';

CREATE OR REPLACE FUNCTION bughunt.finalize_paid_payment(p_order_id text)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    payment_row bughunt.payments;
BEGIN
    SELECT * INTO payment_row
    FROM bughunt.payments
    WHERE order_id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Unknown payment order: %', p_order_id;
    END IF;

    IF payment_row.user_id IS NULL OR payment_row.product_code IS NULL THEN
        RAISE EXCEPTION 'Payment % lacks a user or item identifier', p_order_id;
    END IF;

    IF payment_row.status <> 'PAID' THEN
        PERFORM bughunt.set_payment_status(p_order_id, 'PAID');
    END IF;

    PERFORM bughunt.grant_purchase(payment_row.user_id, payment_row.product_code, p_order_id);
END;
$$;

COMMENT ON FUNCTION bughunt.finalize_paid_payment(text) IS
    'Transactionally marks an order paid and provisions its entitlement; call only after server-side provider verification.';
