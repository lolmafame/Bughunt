# Firebase-to-PostgreSQL Database Guide

These migrations document the live `bug-hunt-game` Firestore schema as observed on 2026-07-22 and translate it to PostgreSQL 15. They contain schema only: no production user, payment, or credential values are embedded.

## Apply the schema

Apply the standalone SQL files with `psql` in dependency order: `users/users.sql`, `admins.sql`, `usernames.sql`, `game_config.sql`, `products.sql`, `payments.sql`, followed by the remaining files under `users/`, then `user_achievements.sql`.

```powershell
psql "$env:DATABASE_URL" -v ON_ERROR_STOP=1 -f database/migrations/users/users.sql
```

## Migration layout

The migration tree follows the top-level Firestore collections:

```text
migrations/
|-- admins.sql
|-- game_config.sql
|-- payments.sql
|-- products.sql
|-- user_achievements.sql
|-- usernames.sql
|-- users/
|   |-- users.sql
|   |-- user_inventory.sql
|   |-- user_purchases.sql
|   |-- user_stats.sql
|   |-- user_wallets.sql
|   `-- user_level_progress.sql
```

The `users/` folder contains multiple relational tables because the Firestore `users` documents embed arrays, maps, and dynamic progress fields. Other collections map to one SQL file and remain at the migration root.

## Cloud functions and SQL connection

Runtime payment code is stored in `database/cloud/`:

```text
database/
|-- cloud/
|   |-- index.js       # Firebase callable, HTTP, and scheduled functions
|   |-- package.json   # Node.js 24 and Firebase dependencies
|   `-- firebase.json  # Firebase Functions deployment configuration
`-- migrations/       # PostgreSQL schema and stored functions
```

The cloud code and SQL are **not connected yet**. `cloud/index.js` initializes Firebase Admin and reads/writes Firestore directly; `cloud/package.json` has no PostgreSQL driver or connection configuration. The current production flow is:

```text
Unity client -> Firebase Cloud Function -> Firestore
```

After migrating, the intended flow is:

```text
Unity client -> Firebase Cloud Function -> PostgreSQL stored function -> SQL tables
```

| Cloud operation | Current Firestore access | PostgreSQL replacement |
|---|---|---|
| `createMayaCheckout` | Creates `payments/{orderId}` | `bughunt.create_payment(...)` |
| `mayaWebhook` | Updates payment status and `users.purchases` | `bughunt.set_payment_status(...)` followed by `bughunt.grant_purchase(...)`, or `bughunt.finalize_paid_payment(...)` |
| `verifyMayaPayment` / `_markPaid` | Confirms payment and grants entitlement | `bughunt.finalize_paid_payment(...)` |
| `scheduledPaymentCleanup` | Marks stale pending payments `EXPIRED` | Update eligible `bughunt.payments` rows through a server-only cleanup transaction |
| `cleanupStalePendingPayments` | Expires one player's stale orders | Same cleanup filtered by `user_id` |
| Premium ownership check | Reads `users.purchases.premium_plan` | Reads `bughunt.users.is_premium` or `bughunt.user_purchases` |
| Hard-coded `storePrices` | In-memory JavaScript catalog | Read active prices from `bughunt.products` |

To activate PostgreSQL, add a server-side PostgreSQL client, store `DATABASE_URL` in Google Secret Manager, use a small connection pool, and replace Firestore mutations with the stored functions above. Keep Firebase Authentication: its UID maps directly to `bughunt.users.firebase_uid`. Do not connect the Unity client directly to PostgreSQL.

Deployment note: `cloud/firebase.json` currently declares the Functions source as `functions`, while `index.js` and `package.json` are directly inside `cloud/`. Before deployment, either change the source to `.` or move those files into `cloud/functions/`.

Security note: `cloud/index.js` currently contains Maya sandbox credentials in source and exposes a debug endpoint. Rotate the credentials, move both keys to Secret Manager, and remove or authenticate the debug endpoint before any production deployment.

## Firestore mapping

| Firestore source | PostgreSQL destination | Transformation |
|---|---|---|
| `admins/{id}` | `bughunt.admins` | Document ID becomes `principal`; `name` becomes `display_name`. |
| `game_config/{id}` | `bughunt.game_config` | Camel-case numeric fields become snake_case columns. |
| `products/{id}` | `bughunt.products` | Document ID becomes `product_code`. |
| `payments/{orderId}` | `bughunt.payments` | `clientUserId` or `playerId` becomes `user_id`; `clientItemId` or `itemId` becomes `product_code`. String/integer amounts must be parsed to `numeric`. |
| `users/{uid}` | `bughunt.users` | Document ID becomes `firebase_uid`; profile/auth metadata stays on the user row. |
| `users.inventory[]` | `bughunt.user_inventory` | One row per item. |
| `users.purchases.*` | `bughunt.user_purchases` | One row per truthy map entry. |
| `users.stats` | `bughunt.user_stats` | Map fields become typed columns. |
| `users.wallet` | `bughunt.user_wallets` | Coins and gems become typed balances. |
| `users.{language}_level*` | `bughunt.user_level_progress` | Dynamic fields become rows keyed by language and level. Tutorial uses level `0`. |
| `usernames/{name}` | `bughunt.usernames` | Document ID becomes normalized username; `uid` references users. |
| `user_achievements/{uid}.unlocked` | `bughunt.user_achievements` | One row per truthy achievement-map entry. |

Legacy `created_at` strings should be retained in `users.legacy_created_at`; valid values may also be parsed into `created_at`. Firebase Authentication passwords are not stored in Firestore and are outside these migrations.

## Known migration cleanup

- Current user emails, profile usernames, and username reservations have no case-insensitive duplicates.
- `22` of `60` payment documents lack both known item-ID aliases. Import them with a null `product_code` and retain non-secret source metadata in `raw_payload`; they cannot be provisioned automatically.
- Existing payment item IDs (`player_skin1`, `premium_plan`, and `unknown_item`) do not match current product document IDs. `payments.product_code` therefore intentionally has no foreign key until aliases are reconciled.
- All currently paid records include `paidAt`; convert it directly to `paid_at`.

## Database functions

- `upsert_user_profile`: creates or refreshes a Firebase-backed user profile.
- `claim_username`: atomically reserves a case-insensitive username and updates the user.
- `is_admin`: checks an admin principal.
- `get_game_config`, `list_active_products`: return client-facing configuration/catalog data.
- `create_payment`, `set_payment_status`: create payments and enforce status transitions.
- `grant_inventory_item`, `grant_purchase`, `finalize_paid_payment`: provision paid entitlements transactionally.
- `record_user_stats`, `adjust_wallet`: update gameplay counters and balances.
- `record_level_completion`: upserts completion and retains the best (lowest) time.
- `unlock_achievement`: idempotently records an unlocked achievement.

Security rules do not translate automatically to SQL. Grant application roles only `EXECUTE` on required functions and add provider-specific row-level-security policies before exposing these tables directly to clients.
