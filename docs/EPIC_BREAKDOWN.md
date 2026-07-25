# CROWD Coffee — Epic → Story → Task Breakdown

Working execution checklist derived from `CROWD_Coffee_PRD.md`. Check tasks off as they're completed; work through epics roughly in the order below (dependencies noted per story). See "Context & Decisions" for why this order deviates from the PRD's raw §13 list, and the "Open Questions Index" for §14 items to resolve before their listed epic.

---

## Context & Decisions

**Ordering deviations from PRD §13**, both dependency-driven:
- **Order State Machine moved from position 7 to position 3** (right after the domain model), per §15 instruction #5: *"Build the domain model and order state machine first. Everything else is CRUD."*
- **Inventory Core moved from position 10 to position 4.** Catalog's availability filtering (§8) depends on `StockItem`/`Recipe`/reservation logic existing, so Inventory has to land before the Catalog API epic that consumes it.

**Frontend: Vue 3 + Vite + TypeScript + Pinia.** The design assets are static HTML with `{{ }}` interpolation and one component class per page — structurally a Vue SFC already. Vue's small runtime and per-route code-splitting also suit the 3G/mobile constraint (§5, §12) better than React/Angular. Pinia holds cart state (mirrors the `localStorage` cart in §3) and modifier-modal state (§4a).

**Payment gateway: recommend Paymob**, isolated behind `IPaymentGateway` so Fawry can be added later. Broader single-integration coverage of cards + Vodafone Cash + InstaPay with documented REST + webhook flow. Still needs business-side confirmation (merchant account availability) — not purely a technical call.

---

## Open Questions Index (PRD §14)

| # | Question | First touches |
|---|---|---|
| 1 | Delivery fee flat everywhere, or capped by distance? | Epic 9 (Checkout) |
| 2 | Block ordering outside opening hours? What happens to an order placed at closing? | Epic 9 (Checkout) |
| 3 | Are the 40 EGP alt-milk / 30–40 EGP syrup surcharges uniform across every drink category? | Epic 7 / Epic 2 seed data |
| 4 | Minimum order value for delivery? | Epic 9 (Checkout) |
| 5 | Does the customer get order tracking post-checkout, or does confirmation end the journey? | Epic 6 / Epic 15 |
| 6 | Who receives low-stock alerts, and through what channel? | Epic 4 / Epic 14 |
| 7 | Is there an existing in-store POS consuming the same stock? | Epic 4 (Inventory) |

None block starting Epic 1–3. Resolve each before the epic it's listed against begins.

---

## Epic 1 — Foundations

Goal: a buildable, deployable skeleton with the layering the rest of the project depends on.

### Story 1.1 — Solution structure & layering ✅
AC: solution builds; Domain project references nothing outside `System.*`; dependency direction is one-way (API → Infrastructure/Application → Domain).
- [x] T1.1.1 Create solution + Domain / Application / Infrastructure / API projects
- [x] T1.1.2 Wire project references enforcing one-way dependency direction
- [x] T1.1.3 Add `.editorconfig`
- [x] T1.1.4 Add `Directory.Build.props` (nullable enable, shared settings)
- Size: S
- Delivered: monorepo layout, `server/` holds the solution (`src/CrowdCoffee.{Domain,Application,Infrastructure,Api}`, `tests/CrowdCoffee.Domain.Tests`), all targeting net8.0. Domain has zero `ProjectReference`s (verified). `dotnet build` and `dotnet test` both pass. Root `.gitignore` added.

### Story 1.2 — Cross-cutting: DI, logging, config, health checks ✅
AC: Serilog structured logging with correlation id per request; typed config; `/health` returns 200 with dependency checks.
- [x] T1.2.1 Add Serilog + `UseSerilogRequestLogging`
- [x] T1.2.2 Add correlation-id middleware
- [x] T1.2.3 Add typed `IOptions<T>` settings classes for connection strings/secrets
- [x] T1.2.4 Add `AddHealthChecks()` incl. SQL Server check
- Size: S · Depends on: 1.1
- Delivered: Serilog (console + rolling file) wired via `UseSerilogRequestLogging`; `CorrelationIdMiddleware` reads/generates `X-Correlation-Id` and pushes it into the Serilog `LogContext`; `DatabaseOptions` typed options class in Infrastructure bound from `Database:ConnectionString`; `/health` maps to `AddSqlServer` check. Verified live: `dotnet run` + `curl localhost:5076/health` → correctly returned `503 Unhealthy` with a clear SQL error, since the `CrowdCoffee` database doesn't exist yet (created in Epic 2). Connection string uses Windows integrated auth (no secret), so it's safe to commit as the local-dev default; production will override via environment variable per §12.

### Story 1.3 — FluentValidation & Result-type error handling scaffold ✅
AC: `Result<T>`/`Error` pattern exists in Application; one example validator proves validation failure → 400 with structured body, not an exception.
- [x] T1.3.1 Define `Result`/`Result<T>`/`Error` types
- [x] T1.3.2 Add FluentValidation package + one example validator
- [x] T1.3.3 Wire validation into the request pipeline
- [x] T1.3.4 Add global exception handler middleware as last-resort net
- Size: S · Depends on: 1.1
- Delivered: `Result`/`Result<T>`/`Error` live in **Domain/Common** (not Application as originally sketched) — Epic 3's state machine needs to return domain errors without Domain depending upward, so the shared type has to sit at the bottom of the graph. `EchoRequestValidator` (FluentValidation) + `GlobalExceptionHandlerMiddleware` wired in. Verified live: `POST /api/diagnostics/echo` with an empty message → `400` with a structured `errors.Message` body; with a valid message → `200` with the echoed value.

### Story 1.4 — Application layer pattern ✅
AC: one vertical slice implemented end-to-end proving the chosen pattern (API → Application → Domain → Infrastructure).
- [x] T1.4.1 Decide MediatR vs. plain application services
- [x] T1.4.2 Add validation/logging pipeline behaviors (if MediatR)
- [x] T1.4.3 Implement one placeholder slice (e.g. "list categories") end-to-end
- [x] T1.4.4 Write short `ARCHITECTURE.md` documenting the pattern
- Size: S · Depends on: 1.1, 1.3
- Delivered: decided **against MediatR** — plain DI-resolved handler classes (`EchoHandler`), validation invoked explicitly per-action rather than via pipeline behavior (see `ARCHITECTURE.md`). Placeholder slice is the `EchoHandler`/`EchoRequestValidator`/`DiagnosticsController` set from Story 1.3, not a fake "list categories" — `Category` doesn't exist as an entity until Epic 2, so a categories placeholder now would just be thrown away. Documented the full decision and pipeline order in `server/ARCHITECTURE.md`.

### Story 1.5 — CI pipeline ✅
AC: PR triggers build + unit test run; fails the check on red tests.
- [x] T1.5.1 Add GitHub Actions workflow: restore, build, test
- [x] T1.5.2 Cache NuGet packages in the workflow
- Size: S · Depends on: 1.1
- Delivered: `.github/workflows/ci.yml`, runs on push/PR to `main`, `ubuntu-latest` (no Windows-specific dependency yet), NuGet cache keyed on `**/*.csproj` hash, restore → build (Release) → test (Release). Will start executing once this repo has a GitHub remote to push to — not yet pushed.

## Epic 1 status: ✅ complete (5/5 stories)

---

## Epic 2 — Domain Model & Migrations

Goal: settle every entity from §6/§4a, seed real menu data, get EF Core migrations working. Highest-risk epic alongside Epic 3.

### Story 2.1 — Catalog entities
AC: `Category`, `Product`, `ModifierGroup`, `ModifierOption`, `ProductModifierGroup` per §6/§4a; unique indexes on slugs.
- [ ] T2.1.1 `Category` entity + EF configuration
- [ ] T2.1.2 `Product` entity + EF configuration
- [ ] T2.1.3 `ModifierGroup` entity (SelectionType, MaxSelections, IsRequired, DisplayOrder)
- [ ] T2.1.4 `ModifierOption` entity (Slug, PriceDelta, IsActive, DisplayOrder)
- [ ] T2.1.5 `ProductModifierGroup` join (category-wide for v1)
- [ ] T2.1.6 Unique indexes on `Product.Slug`, `Category.Slug`
- Size: M

### Story 2.2 — Ordering entities
AC: `Order`, `OrderItem`, `OrderItemModifier`, `OrderStatusHistory`; `OrderNumber` separate from PK; money `decimal(18,2)`; timestamps UTC.
- [ ] T2.2.1 `Order` entity + EF configuration
- [ ] T2.2.2 `OrderItem` entity with snapshot fields (ProductName, UnitPrice)
- [ ] T2.2.3 `OrderItemModifier` entity with snapshot fields (ModifierName, PriceDelta)
- [ ] T2.2.4 `OrderStatusHistory` entity
- [ ] T2.2.5 Unique constraint on `OrderNumber`
- Size: M · Depends on: 2.1

### Story 2.3 — Inventory entities
AC: `StockItem`, `Recipe` (linking Product and ModifierOption), `StockMovement` as append-only ledger.
- [ ] T2.3.1 `StockItem` entity + EF configuration
- [ ] T2.3.2 `Recipe` entity linking to `Product`
- [ ] T2.3.3 `Recipe` linking to `ModifierOption` (§4a rule 5)
- [ ] T2.3.4 `StockMovement` entity, signed `Quantity`, `MovementType` enum
- [ ] T2.3.5 Composite index `(StockItemId, CreatedAt)`
- [ ] T2.3.6 Ensure Infrastructure exposes no update path for `StockMovement` rows (append-only by design)
- Size: M · Depends on: 2.1

### Story 2.4 — Staff entities
AC: `StaffUser` with `Role` enum (Manager|Barista).
- [ ] T2.4.1 `StaffUser` entity + EF configuration
- [ ] T2.4.2 Size password hash column for the algorithm chosen in Epic 11
- Size: S

### Story 2.5 — Menu seed migration
AC: 19 categories, 161 products, modifier groups/options extracted programmatically from `Menu.html` — not retyped. Missing `cheesecakes` description handled gracefully.
- [ ] T2.5.1 Write extraction script/parser for `Menu.html`'s `CATEGORIES`, `D`, `ITEMS`
- [ ] T2.5.2 Extract `MILK_OPTIONS`, `SHOT_OPTION`, `SYRUP_OPTIONS`, `DRINK_CATS`
- [ ] T2.5.3 Handle missing `cheesecakes` description (18 keys in `D` vs. 19 categories) without crashing
- [ ] T2.5.4 Emit seed migration / EF data seeding from extracted data
- [ ] T2.5.5 Verify seeded counts: 19 categories, 161 products
- [ ] T2.5.6 Flag recipe *quantities* (grams/ml per drink) as TBD business data — not in design files, structural seed only
- Size: M · Depends on: 2.1, 2.3

### Story 2.6 — Rebuild/reconcile command for stock ledger
AC: recomputes `QuantityOnHand` from `StockMovement` ledger, reports drift from cached value.
- [ ] T2.6.1 Application command: recompute on-hand from ledger per `StockItem`
- [ ] T2.6.2 Report/log drift between recomputed and cached values
- [ ] T2.6.3 Expose via CLI or admin-only endpoint trigger
- Size: S · Depends on: 2.3

---

## Epic 3 — Order State Machine

Goal: pure domain logic, no I/O, fully unit-testable. Highest-risk epic alongside Epic 2.

### Story 3.1 — State machine implementation
AC: implements exactly the §7 graph; illegal transitions rejected, not silently ignored; every successful transition appends `OrderStatusHistory` in the same call.
- [ ] T3.1.1 `OrderStatus` enum
- [ ] T3.1.2 Transition-guard method on `Order` aggregate (`TransitionTo(newStatus, changedBy, reason)`)
- [ ] T3.1.3 Validate legal edges from §7 graph before mutating
- [ ] T3.1.4 Append `OrderStatusHistory` row on every successful transition
- Size: M · Depends on: 2.2

### Story 3.2 — Entry-point routing by payment method
AC: cash orders constructible only at `Pending`; card/wallet only at `PendingPayment`.
- [ ] T3.2.1 `Order.CreateCash(...)` factory method
- [ ] T3.2.2 `Order.CreatePendingPayment(...)` factory method
- [ ] T3.2.3 Remove/restrict public constructor to prevent arbitrary initial status
- Size: S · Depends on: 3.1

### Story 3.3 — Cancellation releases reservations (domain event)
AC: cancelling raises `OrderCancelled`; state machine itself doesn't know about `StockItem`.
- [ ] T3.3.1 Add lightweight in-memory domain event dispatch
- [ ] T3.3.2 `OrderCancelled` event carrying order id + line items
- [ ] T3.3.3 Raise event from the cancellation transition
- Size: S · Depends on: 3.1

### Story 3.4 — Unit test suite for the state machine
AC: every legal edge tested positive, every illegal edge tested negative; cash vs. card entry-point rules tested.
- [ ] T3.4.1 xUnit project against Domain only, no database
- [ ] T3.4.2 Positive tests for every legal transition
- [ ] T3.4.3 Negative tests for illegal transitions (e.g. `Ready → Pending`)
- [ ] T3.4.4 Entry-point tests (cash vs. card/wallet)
- Size: M · Depends on: 3.1, 3.2

---

## Epic 4 — Inventory Core

Goal: ingredient-level stock, reservation, auto-availability (§8).

### Story 4.1 — Reservation flow
AC: `Available = QuantityOnHand − QuantityReserved`; reservation on `Confirmed` only, never `Pending`.
- [ ] T4.1.1 `IStockReservationService` with `Reserve`, `ReleaseReservation`, `Fulfill`
- [ ] T4.1.2 Subscribe to `OrderCancelled` to auto-release reservations
- [ ] T4.1.3 Ensure reservation writes no ledger row (only `Fulfill` does)
- Size: M · Depends on: 2.3, 3.3

### Story 4.2 — Sale movement on order completion
AC: on `Delivered`/`PickedUp`, reservation converts to `Sale` movement, `QuantityOnHand` decrements, idempotent per order line.
- [ ] T4.2.1 Wire `Fulfill` to the completion transition
- [ ] T4.2.2 Write `Sale` `StockMovement` per ingredient consumed
- [ ] T4.2.3 Guard against double-fulfillment of the same order
- Size: M · Depends on: 4.1, 3.1

### Story 4.3 — Auto-availability computation
AC: product/modifier option unavailable when any recipe ingredient can't cover one unit at current `Available` stock.
- [ ] T4.3.1 `IAvailabilityService.IsAvailable(productId | modifierOptionId)`
- [ ] T4.3.2 Decide computed-on-read vs. cached-flag (recommend computed-on-read for v1; revisit if <200ms budget is at risk)
- [ ] T4.3.3 Unit tests for boundary cases (exactly enough stock, zero stock)
- Size: M · Depends on: 4.1

### Story 4.4 — Manual availability override
AC: manager's manual `IsAvailable = false` always wins over computed value.
- [ ] T4.4.1 Combine `Product.IsAvailable && ComputedAvailable` in the read path
- Size: S · Depends on: 4.3

### Story 4.5 — Low-stock alerting
AC: `StockItem`s below `ReorderPoint` surface on an admin-queryable list.
- [ ] T4.5.1 Query/service returning items below `ReorderPoint`
- [ ] T4.5.2 Leave push/email/SMS alert unimplemented pending §14.6 answer; wire list into Epic 14 dashboard only
- Size: S · Depends on: 2.3

### Story 4.6 — Purchase/waste/adjustment recording
AC: purchase increases on-hand, waste/adjustment decrease with mandatory reason — all as ledger entries.
- [ ] T4.6.1 Application command: record `Purchase` movement
- [ ] T4.6.2 Application command: record `Waste` movement (mandatory reason)
- [ ] T4.6.3 Application command: record `Adjustment` movement (mandatory reason)
- [ ] T4.6.4 Validation enforcing required reason on Waste/Adjustment
- Size: M · Depends on: 2.3
- Note (§14.7): an in-store POS consuming the same stock outside this system would cause ledger drift regardless of this epic's correctness — confirm before go-live.

---

## Epic 5 — Catalog API

Goal: categories/products/modifiers with availability and caching, <200ms (§12).

### Story 5.1 — GET /categories, GET /products
AC: 19 categories in display order; products filtered to `IsActive && IsAvailable` (manual + computed).
- [ ] T5.1.1 Read-model query for categories (display order)
- [ ] T5.1.2 Read-model query for products (`AsNoTracking`/Dapper projections, avoid full aggregate loads)
- [ ] T5.1.3 Apply Epic 4.4's combined availability filter
- [ ] T5.1.4 Decide response shape (grouped-by-category vs. flat + ref) based on Epic 6 needs
- Size: M · Depends on: 2.1, 4.3

### Story 5.2 — GET /products/{slug}/modifiers
AC: returns modifier groups/options applicable per §4a `DRINK_CATS`, each option flagged available/unavailable.
- [ ] T5.2.1 Query joining `ProductModifierGroup` → `ModifierGroup` → `ModifierOption`
- [ ] T5.2.2 Apply per-option availability from Epic 4.3
- Size: M · Depends on: 2.1, 4.3

### Story 5.3 — Caching & cache invalidation
AC: menu endpoint <200ms; writes from Epic 13 invalidate cache promptly (correctness over TTL convenience).
- [ ] T5.3.1 Add cache layer (`IMemoryCache` or `IDistributedCache`)
- [ ] T5.3.2 Explicit invalidation hook on product/category/modifier writes
- Size: S · Depends on: 5.1, 5.2

---

## Epic 6 — Public Site: Landing + Menu

Goal: pixel-match `Crowd Coffee.dc.html` and `Menu.html` (newer version), mobile-first, Vue 3.

### Story 6.1 — Frontend project scaffold
AC: Vite + Vue 3 + TypeScript; OKLCH tokens preserved as CSS custom properties; both accent colors present and distinct.
- [ ] T6.1.1 Scaffold Vite + Vue 3 + TS project
- [ ] T6.1.2 Design-token CSS file (OKLCH values, not converted to hex)
- [ ] T6.1.3 Base layout matching design typography/spacing
- Size: M · Depends on: 1.1 (can start once Epic 5's contract is stable)

### Story 6.2 — Landing page
AC: matches `Crowd Coffee.dc.html` layout/copy; usable at 375px viewport.
- [ ] T6.2.1 Hero section component
- [ ] T6.2.2 Story section component (brand copy: "A space for every crowd.", etc.)
- [ ] T6.2.3 Menu-teaser section component
- [ ] T6.2.4 Gallery section with lazy-loaded WebP/AVIF images
- [ ] T6.2.5 Visit-info section component
- Size: M · Depends on: 6.1

### Story 6.3 — Menu page with category filter
AC: 19 category chips in specified order; client-side filtering (fetch once).
- [ ] T6.3.1 Menu fetch-on-load with client-side cache
- [ ] T6.3.2 Category chip component (19 categories, display order)
- [ ] T6.3.3 Product card component with lazy image loading
- [ ] T6.3.4 Client-side category filtering logic
- Size: L · Depends on: 6.1, 5.1
- Note (§14.5): if post-checkout order tracking is in scope, this epic's route map needs a tracking view — resolve before finalizing routes.

### Story 6.4 — Performance pass
AC: Lighthouse mobile performance ≥ 85.
- [ ] T6.4.1 Image optimization/lazy-load audit
- [ ] T6.4.2 Bundle size check
- [ ] T6.4.3 Route-level code splitting
- Size: S · Depends on: 6.2, 6.3

---

## Epic 7 — Drink Customization Modal

Goal: reproduce §4a exactly.

### Story 7.1 — Modal UI: milk/shot/syrup groups
AC: Milk required (incl. "No Milk" as valid explicit choice, no null fallthrough); Shot optional single-select; Syrups max 2; independent qty stepper (min 1).
- [ ] T7.1.1 Modal component shell
- [ ] T7.1.2 Milk Type group (single-select, required, Add disabled until chosen)
- [ ] T7.1.3 Espresso Shot group (optional single-select)
- [ ] T7.1.4 Syrups group (max 2 enforced client-side)
- [ ] T7.1.5 Modal-local qty stepper (min 1, independent of cart stepper)
- Size: L · Depends on: 5.2, 6.1

### Story 7.2 — Live pricing
AC: `unitPrice = base + milk + shot + Σsyrups`; `lineTotal = unitPrice × modalQty`, live-updating.
- [ ] T7.2.1 Reactive price computation (Pinia getter/computed)
- [ ] T7.2.2 Display note/comment that this value is display-only (server re-prices)
- Size: S · Depends on: 7.1
- Note (§14.3): confirm surcharges are uniform across drink categories before hardcoding a single price table.

### Story 7.3 — Non-drink categories bypass the modal
AC: food categories add straight to cart at base price.
- [ ] T7.3.1 `DRINK_CATS` membership check gating modal vs. direct-add
- Size: S · Depends on: 7.1

---

## Epic 8 — Cart

Goal: modifier-aware cart identity (§4a), localStorage persistence (§3), server re-pricing.

### Story 8.1 — Client cart with composed line identity
AC: id = `{productId}::{milkId}::{shot|noshot}::{sortedSyrupIds joined by "-"}`, syrups sorted before joining; matching ids increment qty.
- [ ] T8.1.1 Pinia cart store
- [ ] T8.1.2 Id-composition utility (pure function, unit-testable)
- [ ] T8.1.3 Unit tests: sorted-syrup collapsing, matching vs. differing lines
- [ ] T8.1.4 localStorage persistence (`crowdcoffee_cart`, `crowdcoffee_order_mode`)
- Size: M · Depends on: 7.2

### Story 8.2 — Cart page UI
AC: matches `Cart.dc.html`; delivery 30 EGP flat / pickup free, shown per mode.
- [ ] T8.2.1 Cart page component (line items, totals)
- [ ] T8.2.2 Qty stepper component
- [ ] T8.2.3 Delivery/pickup mode toggle wired to Pinia store
- Size: M · Depends on: 8.1

### Story 8.3 — Server-side cart re-pricing endpoint
AC: server recomputes each line from current DB prices/deltas; server-side id composition mirrors client.
- [ ] T8.3.1 Server-side id-composition logic mirroring the client scheme
- [ ] T8.3.2 Re-pricing endpoint recomputing line totals from DB
- [ ] T8.3.3 Validation: exactly one milk, ≤1 shot, ≤2 syrups, options belong to product's attached groups
- [ ] T8.3.4 Reject requests failing validation (extra syrups, foreign option ids)
- Size: M · Depends on: 5.2, 8.1

---

## Epic 9 — Checkout & Order Creation

Goal: validate → reserve stock → persist order, atomically (§12).

### Story 9.1 — Checkout page UI
AC: matches `Checkout.dc.html`; location pin via `postMessage` contract; required fields per mode.
- [ ] T9.1.1 Checkout form component
- [ ] T9.1.2 Location picker integration (`{ type: 'crowd-location', lat, lng }`)
- [ ] T9.1.3 Client-side required-field validation (name/phone always; address if delivery)
- [ ] T9.1.4 Payment method selection UI
- Size: L · Depends on: 8.2
- Note (§14.1, §14.2, §14.4): flat-vs-capped delivery fee, opening-hours enforcement, and minimum delivery order value all affect this form's validation rules — resolve before finalizing.

### Story 9.2 — Order creation endpoint (transactional)
AC: single transaction validate → reserve → persist with snapshots; `OrderNumber` in `CRW-YYMMDD-NNNN` format, daily-resetting sequence; no orphan reservation on partial failure.
- [ ] T9.2.1 `OrderNumber` sequence generator (DB sequence or per-day counter table — avoid `MAX()+1` under concurrency)
- [ ] T9.2.2 Application command using Epic 3.2 factory methods
- [ ] T9.2.3 Wrap validate → reserve → persist in one DB transaction
- [ ] T9.2.4 Integration test: partial failure leaves no orphan reservation
- Size: L · Depends on: 3.2, 4.1, 8.3

### Story 9.3 — Idempotency key on order creation
AC: repeat `IdempotencyKey` never creates a duplicate order.
- [ ] T9.3.1 Unique constraint on `IdempotencyKey`
- [ ] T9.3.2 Return original order on repeat key instead of erroring/duplicating
- Size: S · Depends on: 9.2

### Story 9.4 — Rate limiting on order creation
AC: endpoint rate-limited per §12.
- [ ] T9.4.1 ASP.NET Core rate limiting middleware on order creation (and future OTP endpoints)
- Size: S · Depends on: 9.2

---

## Epic 10 — Payments (Paymob/Fawry)

Goal: card/wallet payment without touching raw card data (§9).

### Story 10.1 — `IPaymentGateway` abstraction + Paymob adapter
AC: interface covers initiate/verify-webhook/parse-webhook; no gateway-specific types leak outside Infrastructure.
- [ ] T10.1.1 `IPaymentGateway` interface in Application
- [ ] T10.1.2 Paymob adapter in Infrastructure
- [ ] T10.1.3 API keys via environment/Key Vault config
- Size: M · Depends on: 9.2

### Story 10.2 — Hosted checkout (no inline card fields)
AC: hosted iframe/redirect replaces the mock's inline card fields — documented deviation.
- [ ] T10.2.1 Paymob hosted-payment-page integration
- [ ] T10.2.2 Return-URL handling back into the SPA
- [ ] T10.2.3 UI copy making clear this deviates intentionally from the mock design
- Size: M · Depends on: 10.1

### Story 10.3 — Webhook endpoint with HMAC verification
AC: rejects unsigned/mismatched callbacks; idempotent; confirmed payment drives `PendingPayment → Confirmed`.
- [ ] T10.3.1 Webhook endpoint
- [ ] T10.3.2 HMAC signature verification against Paymob's scheme
- [ ] T10.3.3 Idempotent handling of repeat webhooks
- [ ] T10.3.4 Wire confirmed payment to state machine transition (Epic 3.1) → triggers reservation (4.1)
- Size: M · Depends on: 10.1, 3.1

### Story 10.4 — Server-side amount computation
AC: charged amount read from DB order total, never from client payload.
- [ ] T10.4.1 Gateway-initiate call reads `Order.Total` from persisted order
- Size: S · Depends on: 10.1, 9.2

### Story 10.5 — Payment timeout job
AC: `PendingPayment` orders older than 30 min auto-cancel and release reservations.
- [ ] T10.5.1 Background job scanning stale `PendingPayment` orders
- [ ] T10.5.2 Invoke state machine cancel transition on timeout
- Size: S · Depends on: 3.1, 3.3, 4.1

### Story 10.6 — Reconciliation view
AC: manager sees gateway-confirmed payments vs. orders.
- [ ] T10.6.1 Query joining payment records to orders
- [ ] T10.6.2 Surface in admin (Epic 14) or dedicated screen
- Size: M · Depends on: 10.3

---

## Epic 11 — Staff Auth

Goal: JWT + cookie refresh, two roles (§11).

### Story 11.1 — Login & JWT issuance
AC: login issues short-lived JWT + httpOnly refresh cookie; password hashed with BCrypt/Argon2.
- [ ] T11.1.1 Login endpoint
- [ ] T11.1.2 Password hashing (BCrypt/Argon2)
- [ ] T11.1.3 JWT issuance with role claim
- Size: M · Depends on: 2.4

### Story 11.2 — Refresh flow & role-based authorization
AC: refresh rotates token via httpOnly cookie; role guards on manager-only vs. shared endpoints.
- [ ] T11.2.1 Refresh endpoint (cookie-based rotation)
- [ ] T11.2.2 Policy-based authorization setup (Manager vs. Barista)
- Size: M · Depends on: 11.1

---

## Epic 12 — Kitchen Board

Goal: live order board, SignalR push (§11).

### Story 12.1 — SignalR hub for order events
AC: new confirmed orders push in <3 seconds; broadcasts on `Confirmed` and subsequent transitions.
- [ ] T12.1.1 `OrderHub` implementation
- [ ] T12.1.2 Broadcast wired to state-machine transition events
- [ ] T12.1.3 Auth on hub connection (staff only)
- Size: M · Depends on: 3.1, 11.2

### Story 12.2 — Kitchen board UI
AC: three columns (New/Preparing/Ready); modifiers spelled out without extra tap; Google Maps deep link; audible alert.
- [ ] T12.2.1 Vue kitchen-board view + SignalR client connection
- [ ] T12.2.2 Column/card components with one-tap state advance
- [ ] T12.2.3 Modifier display reconstructed from `OrderItemModifier` snapshots
- [ ] T12.2.4 Google Maps deep-link construction from pinned lat/lng
- [ ] T12.2.5 Audible alert on new order
- Size: L · Depends on: 12.1, 9.2

---

## Epic 13 — Admin: Menu, Modifier & Stock Management

### Story 13.1 — Menu management CRUD
AC: create/edit products/categories, prices, `IsAvailable`, `DisplayOrder`, image upload; writes invalidate cache.
- [ ] T13.1.1 Manager-role admin endpoints for product/category CRUD
- [ ] T13.1.2 Image upload handling
- [ ] T13.1.3 Vue admin views
- [ ] T13.1.4 Wire writes to invalidate Epic 5.3 cache
- Size: L · Depends on: 5.1, 11.2

### Story 13.2 — Modifier management
AC: edit price deltas/active state without rewriting historical snapshots.
- [ ] T13.2.1 Admin endpoints for `ModifierGroup`/`ModifierOption` CRUD
- Size: M · Depends on: 13.1

### Story 13.3 — Stock management UI
AC: on-hand vs. reserved, record purchase/waste/adjustment, low-stock list, movement ledger per item.
- [ ] T13.3.1 Admin stock views wired to Epic 4 commands/queries
- [ ] T13.3.2 Ledger view per `StockItem`
- Size: L · Depends on: 4.5, 4.6, 11.2

### Story 13.4 — Order history
AC: filter by date/status/mode/payment method; view full `OrderStatusHistory`.
- [ ] T13.4.1 Filtered order-history query endpoint
- [ ] T13.4.2 Admin view with status-history detail
- Size: M · Depends on: 9.2, 11.2

---

## Epic 14 — Admin: Dashboard & Reporting

### Story 14.1 — Daily dashboard metrics
AC: today's revenue, order count, AOV, top-selling items, delivery vs. pickup split.
- [ ] T14.1.1 Aggregation queries (read-optimized, not full aggregate loads)
- [ ] T14.1.2 Dashboard view
- Size: M · Depends on: 9.2, 11.2

### Story 14.2 — Low-stock & reconciliation surfaced on dashboard
AC: Epic 4.5 and Epic 10.6 reachable from dashboard.
- [ ] T14.2.1 Dashboard widgets/links to low-stock and reconciliation views
- Size: S · Depends on: 14.1, 4.5, 10.6
- Note (§14.6): who gets proactively notified of low stock, and how, is still open.

---

## Epic 15 — Notifications

### Story 15.1 — Staff SignalR alerts (polish)
AC: audible-alert polish and additional staff-facing event types beyond "new order".
- [ ] T15.1.1 Add cancelled-order alert type
- Size: S · Depends on: 12.1

### Story 15.2 — Customer notifications via `INotificationService`
AC: confirmation and ready/out-for-delivery events call the service; no-op logging implementation acceptable for v1.
- [ ] T15.2.1 `INotificationService` interface + no-op implementation
- [ ] T15.2.2 Call sites at `Confirmed`, `Ready`, `OutForDelivery` transitions
- Size: M · Depends on: 3.1
- Note (§14.5): whether customer tracking is in scope affects whether this epic needs a customer-facing status page beyond notifications.

---

## Epic 16 — Hardening

### Story 16.1 — Security headers & CORS
- [ ] T16.1.1 HSTS, CSP, X-Content-Type-Options
- [ ] T16.1.2 CORS restricted to known origins
- Size: S

### Story 16.2 — Secrets audit
- [ ] T16.2.1 Audit every prior epic's config for committed secrets
- Size: S

### Story 16.3 — Accessibility pass
- [ ] T16.3.1 Keyboard navigation audit
- [ ] T16.3.2 Visible focus states
- [ ] T16.3.3 AA contrast check
- [ ] T16.3.4 Alt text on menu imagery
- Size: M

### Story 16.4 — Load/perf check
- [ ] T16.4.1 Load test: menu endpoint <200ms
- [ ] T16.4.2 Load test: order creation <500ms
- Size: M

---

## Epic 17 — Deployment

### Story 17.1 — Docker + environment config
- [ ] T17.1.1 Containerize API
- [ ] T17.1.2 Containerize frontend build
- [ ] T17.1.3 Environment-specific config via env vars (no per-env `appsettings.json` secrets)
- Size: M

### Story 17.2 — Migration strategy & backups
- [ ] T17.2.1 Document EF migration process for production
- [ ] T17.2.2 Define DB backup schedule
- Size: S · Depends on: 17.1

---

## Definition of Done (every story)

Acceptance criteria met · unit tests on domain logic · integration test on the endpoint · input validated · errors handled and logged · matches the design where UI is involved · works on a 375px viewport · no secret committed.
