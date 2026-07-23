CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.payments (
    order_id text PRIMARY KEY,
    checkout_id text UNIQUE,
    user_id text REFERENCES bughunt.users(firebase_uid) ON DELETE SET NULL,
    product_code text,
    item_name text,
    amount numeric(12,2) CHECK (amount >= 0),
    client_amount numeric(12,2) CHECK (client_amount >= 0),
    skin_id text,
    status text NOT NULL DEFAULT 'PENDING'
        CHECK (status IN ('PENDING', 'PAID', 'FAILED', 'CANCELLED', 'EXPIRED')),
    created_at timestamptz NOT NULL DEFAULT now(),
    paid_at timestamptz,
    raw_payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (status <> 'PAID' OR paid_at IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS payments_user_created_idx
    ON bughunt.payments (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS payments_pending_idx
    ON bughunt.payments (created_at) WHERE status = 'PENDING';

COMMENT ON TABLE bughunt.payments IS
    'Maya checkout records migrated from Firestore payments/{orderId}; legacy client/server aliases are consolidated into user_id and product_code.';
COMMENT ON COLUMN bughunt.payments.raw_payload IS
    'Optional non-secret provider metadata not represented by typed columns; never store Maya credentials here.';

CREATE OR REPLACE FUNCTION bughunt.create_payment(
    p_order_id text,
    p_user_id text,
    p_product_code text,
    p_item_name text,
    p_client_amount numeric DEFAULT NULL,
    p_checkout_id text DEFAULT NULL
)
RETURNS bughunt.payments
LANGUAGE plpgsql
AS $$
DECLARE
    result bughunt.payments;
BEGIN
    INSERT INTO bughunt.payments (
        order_id, checkout_id, user_id, product_code, item_name,
        client_amount, status
    )
    VALUES (
        p_order_id, p_checkout_id, p_user_id, p_product_code,
        p_item_name, p_client_amount, 'PENDING'
    )
    RETURNING * INTO result;

    RETURN result;
END;
$$;

COMMENT ON FUNCTION bughunt.create_payment(text, text, text, text, numeric, text) IS
    'Creates one pending checkout record; authoritative amount and paid state must come from the verified server/webhook response.';

CREATE OR REPLACE FUNCTION bughunt.set_payment_status(
    p_order_id text,
    p_status text,
    p_amount numeric DEFAULT NULL,
    p_checkout_id text DEFAULT NULL,
    p_provider_payload jsonb DEFAULT '{}'::jsonb
)
RETURNS bughunt.payments
LANGUAGE plpgsql
AS $$
DECLARE
    current_payment bughunt.payments;
    result bughunt.payments;
BEGIN
    SELECT * INTO current_payment
    FROM bughunt.payments
    WHERE order_id = p_order_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Unknown payment order: %', p_order_id;
    END IF;

    IF current_payment.status <> 'PENDING' AND current_payment.status <> p_status THEN
        RAISE EXCEPTION 'Illegal payment transition from % to %', current_payment.status, p_status;
    END IF;

    IF p_status NOT IN ('PENDING', 'PAID', 'FAILED', 'CANCELLED', 'EXPIRED') THEN
        RAISE EXCEPTION 'Unsupported payment status: %', p_status;
    END IF;

    UPDATE bughunt.payments
    SET status = p_status,
        amount = COALESCE(p_amount, amount),
        checkout_id = COALESCE(p_checkout_id, checkout_id),
        paid_at = CASE WHEN p_status = 'PAID' THEN COALESCE(paid_at, now()) ELSE paid_at END,
        raw_payload = raw_payload || COALESCE(p_provider_payload, '{}'::jsonb),
        updated_at = now()
    WHERE order_id = p_order_id
    RETURNING * INTO result;

    RETURN result;
END;
$$;

COMMENT ON FUNCTION bughunt.set_payment_status(text, text, numeric, text, jsonb) IS
    'Applies an idempotent terminal status from a server-verified Maya response and rejects terminal-to-terminal changes.';
