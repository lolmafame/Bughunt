CREATE SCHEMA IF NOT EXISTS bughunt;

CREATE TABLE IF NOT EXISTS bughunt.products (
    product_code text PRIMARY KEY,
    display_name text NOT NULL,
    currency char(3) NOT NULL CHECK (currency = upper(currency)),
    price_amount numeric(12,2) NOT NULL CHECK (price_amount >= 0),
    slot_type text NOT NULL,
    texture_id text,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS products_active_idx
    ON bughunt.products (slot_type, product_code) WHERE is_active;

COMMENT ON TABLE bughunt.products IS
    'Purchasable catalog migrated from Firestore products/{productCode}; current payment item IDs require an explicit catalog-alias cleanup before adding a foreign key.';

CREATE OR REPLACE FUNCTION bughunt.list_active_products(p_slot_type text DEFAULT NULL)
RETURNS SETOF bughunt.products
LANGUAGE sql
STABLE
AS $$
    SELECT p.*
    FROM bughunt.products p
    WHERE p.is_active
      AND (p_slot_type IS NULL OR p.slot_type = p_slot_type)
    ORDER BY p.slot_type, p.display_name;
$$;

COMMENT ON FUNCTION bughunt.list_active_products(text) IS
    'Lists active catalog entries, optionally filtered by slot type.';
