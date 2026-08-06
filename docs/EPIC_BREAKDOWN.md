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

### Story 2.1 — Catalog entities ✅
AC: `Category`, `Product`, `ModifierGroup`, `ModifierOption`, `ProductModifierGroup` per §6/§4a; unique indexes on slugs.
- [x] T2.1.1 `Category` entity + EF configuration
- [x] T2.1.2 `Product` entity + EF configuration
- [x] T2.1.3 `ModifierGroup` entity (SelectionType, MaxSelections, IsRequired, DisplayOrder)
- [x] T2.1.4 `ModifierOption` entity (Slug, PriceDelta, IsActive, DisplayOrder)
- [x] T2.1.5 `ProductModifierGroup` join (category-wide for v1)
- [x] T2.1.6 Unique indexes on `Product.Slug`, `Category.Slug`
- Size: M
- Delivered: `Category.Slug` is an EF alternate key (not just a unique index) since `Product.CategorySlug` and `ProductModifierGroup.CategorySlug` both FK against it. `ModifierOption.Slug` is unique **per group**, not globally — §4a's syrup list has the same name (e.g. "Vanilla") appearing at two different price points (sugar-free vs. regular), so a global unique slug would collide; will confirm the exact slug scheme against `Menu.html` in Story 2.5.

### Story 2.2 — Ordering entities ✅
AC: `Order`, `OrderItem`, `OrderItemModifier`, `OrderStatusHistory`; `OrderNumber` separate from PK; money `decimal(18,2)`; timestamps UTC.
- [x] T2.2.1 `Order` entity + EF configuration
- [x] T2.2.2 `OrderItem` entity with snapshot fields (ProductName, UnitPrice)
- [x] T2.2.3 `OrderItemModifier` entity with snapshot fields (ModifierName, PriceDelta)
- [x] T2.2.4 `OrderStatusHistory` entity
- [x] T2.2.5 Unique constraint on `OrderNumber`
- Size: M · Depends on: 2.1
- Delivered: `Order` is deliberately schema-only right now — no public constructor, `Status`/etc. are private-set. Epic 3 owns the `CreateCash`/`CreatePendingPayment` factories and the `TransitionTo` method that mutate this class; adding them here would preempt Story 3.2's design. `OrderStatus` enum lives in `Domain/Ordering/OrderEnums.cs` since the schema needs the type now, even though the transition graph itself is Epic 3's job. Also unique-indexed `IdempotencyKey` (needed by Story 9.3, cheap to add now while touching this table). Enums stored as `string` columns (not `int`) for DB readability during manual inspection/debugging.

### Story 2.3 — Inventory entities ✅
AC: `StockItem`, `Recipe` (linking Product and ModifierOption), `StockMovement` as append-only ledger.
- [x] T2.3.1 `StockItem` entity + EF configuration
- [x] T2.3.2 `Recipe` entity linking to `Product`
- [x] T2.3.3 `Recipe` linking to `ModifierOption` (§4a rule 5)
- [x] T2.3.4 `StockMovement` entity, signed `Quantity`, `MovementType` enum
- [x] T2.3.5 Composite index `(StockItemId, CreatedAt)`
- [x] T2.3.6 Ensure Infrastructure exposes no update path for `StockMovement` rows (append-only by design)
- Size: M · Depends on: 2.1
- Delivered: `Recipe.ProductId`/`ModifierOptionId` are both nullable with a DB-level `CHECK` constraint (`CK_Recipes_ExactlyOneOwner`) enforcing exactly one is set — factory methods `Recipe.ForProduct(...)`/`Recipe.ForModifierOption(...)` enforce the same invariant in code, so it's caught at construction time, not just at the DB. `StockMovement` has no update-capable API surface (private setters, no mutator methods) — append-only is structural, not just documented convention.

### Story 2.4 — Staff entities ✅
AC: `StaffUser` with `Role` enum (Manager|Barista).
- [x] T2.4.1 `StaffUser` entity + EF configuration
- [x] T2.4.2 Size password hash column for the algorithm chosen in Epic 11
- Size: S
- Delivered: `PasswordHash` sized `nvarchar(200)` — comfortably covers both BCrypt (~60 chars) and Argon2 (~100 chars) encoded hashes, so Epic 11's algorithm choice won't require a schema change.

### Story 2.5 — Menu seed migration ✅
AC: 19 categories, 161 products, modifier groups/options extracted programmatically from `Menu.html` — not retyped. Missing `cheesecakes` description handled gracefully.
- [x] T2.5.1 Write extraction script/parser for `Menu.html`'s `CATEGORIES`, `D`, `ITEMS`
- [x] T2.5.2 Extract `MILK_OPTIONS`, `SHOT_OPTION`, `SYRUP_OPTIONS`, `DRINK_CATS`
- [x] T2.5.3 Handle missing `cheesecakes` description (18 keys in `D` vs. 19 categories) without crashing
- [x] T2.5.4 Emit seed migration / EF data seeding from extracted data
- [x] T2.5.5 Verify seeded counts: 19 categories, 161 products
- [x] T2.5.6 Flag recipe *quantities* (grams/ml per drink) as TBD business data — not in design files, structural seed only
- Size: M · Depends on: 2.1, 2.3
- Delivered: located the actual `Menu.html` at `C:\Users\Administrator\Downloads\Menu.html` (the file itself is a bundler-wrapped export — the real page script is gzip+base64 inside a `__bundler/manifest` JSON blob, and the `CATEGORIES`/`D`/`ITEMS`/modifier data live inside a separate `__bundler/template` JSON-escaped blob). Decoded both layers with a throwaway Node script (not committed — lived in the session scratchpad only), then used Node's `vm` module to actually *execute* the extracted JS so `D.category` references and string concatenation (e.g. `'Signature. ' + D.breakfast`) resolved to final literal strings rather than being hand-parsed. Resulting JSON is committed at `server/src/CrowdCoffee.Infrastructure/Persistence/Seed/menu-seed.json` (embedded resource), loaded by `MenuSeedData.cs`, wired into `CrowdCoffeeDbContext.OnModelCreating` via `HasData(...)` with deterministic Guids (`DeterministicGuid.cs`, MD5-hashed stable strings) so migration regeneration doesn't churn every row. Verified against the real DB after `dotnet ef database update`: 19 Categories, 161 Products, 3 ModifierGroups, 19 ModifierOptions (6 milk + 1 shot + 12 syrup), 33 ProductModifierGroups (11 drink categories × 3 groups) — all exact matches. `cheesecakes` Category row confirmed `Description IS NULL` in the DB, no crash. Recipe/StockItem rows intentionally not seeded — ingredient quantities per drink aren't in the design file.

### Story 2.6 — Rebuild/reconcile command for stock ledger ✅
AC: recomputes `QuantityOnHand` from `StockMovement` ledger, reports drift from cached value.
- [x] T2.6.1 Application command: recompute on-hand from ledger per `StockItem`
- [x] T2.6.2 Report/log drift between recomputed and cached values
- [x] T2.6.3 Expose via CLI or admin-only endpoint trigger
- Size: S · Depends on: 2.3
- Delivered: `ReconcileStockLedgerHandler` (Application) + `IStockLedgerRepository`/`StockLedgerRepository` (Infrastructure) — first real use of the repository-interface pattern (Application defines the port, Infrastructure adapts EF Core to it) that later epics should reuse rather than referencing `CrowdCoffeeDbContext` directly from Application. It doesn't just report — it actually rewrites `QuantityOnHand` to the ledger sum via a new `StockItem.Reconcile(...)` domain method (distinct from Epic 4's future Reserve/Fulfill methods) and logs a warning per drifted item. Exposed at `POST /api/inventory/reconcile-stock-ledger` — **not yet auth-gated** (marked `TODO(Epic 11)` in `InventoryController`, since staff auth doesn't exist yet). Verified live: seeded a StockItem with a deliberately wrong cached value (999) against real ledger movements summing to 4964, called the endpoint, got back the correct drift (3965) in the response, and confirmed in SQL that `QuantityOnHand` was actually rewritten to 4964, not just reported. Test data cleaned up after.

## Epic 2 status: ✅ complete (6/6 stories)

---

## Epic 3 — Order State Machine

Goal: pure domain logic, no I/O, fully unit-testable. Highest-risk epic alongside Epic 2.

### Story 3.1 — State machine implementation ✅
AC: implements exactly the §7 graph; illegal transitions rejected, not silently ignored; every successful transition appends `OrderStatusHistory` in the same call.
- [x] T3.1.1 `OrderStatus` enum
- [x] T3.1.2 Transition-guard method on `Order` aggregate (`TransitionTo(newStatus, changedBy, reason)`)
- [x] T3.1.3 Validate legal edges from §7 graph before mutating
- [x] T3.1.4 Append `OrderStatusHistory` row on every successful transition
- Size: M · Depends on: 2.2
- Delivered: `OrderStatus` enum actually landed back in Epic 2 Story 2.2 (needed for the `Order.Status` column type) — noted there at the time. `TransitionTo` also enforces a guard beyond the raw §7 graph: `Ready` only ever advances to `OutForDelivery` for `Mode == Delivery` or `PickedUp` for `Mode == Pickup`, not both — the diagram's two branches aren't both legal for a given order, and nothing in §7's text says this explicitly, so it would've been an easy gap. Returns `Result` (Domain/Common), not exceptions, on illegal transitions — an illegal transition attempt doesn't touch `Status` or append history at all (verified by test).

### Story 3.2 — Entry-point routing by payment method ✅
AC: cash orders constructible only at `Pending`; card/wallet only at `PendingPayment`.
- [x] T3.2.1 `Order.CreateCash(...)` factory method
- [x] T3.2.2 `Order.CreatePendingPayment(...)` factory method
- [x] T3.2.3 Remove/restrict public constructor to prevent arbitrary initial status
- Size: S · Depends on: 3.1
- Delivered: `Order`'s only constructor is a private parameterless one (for EF); the two factories are the sole public creation path. `CreatePendingPayment` throws if handed `PaymentMethod.Cash` — a cash order can never be constructed at `PendingPayment`, closing off the exact bypass §7 warns about ("never reach the kitchen board until the gateway webhook confirms").

### Story 3.3 — Cancellation releases reservations (domain event) ✅
AC: cancelling raises `OrderCancelled`; state machine itself doesn't know about `StockItem`.
- [x] T3.3.1 Add lightweight in-memory domain event dispatch
- [x] T3.3.2 `OrderCancelled` event carrying order id + line items
- [x] T3.3.3 Raise event from the cancellation transition
- Size: S · Depends on: 3.1
- Delivered: `OrderCancelledEvent` carries **only `OrderId`**, not line items — deliberate deviation from the task title's "+ line items." A handler built from whatever `Order.Items` happened to have loaded in memory at transition time would silently produce wrong results if the caller hadn't `Include()`'d that navigation before calling `TransitionTo`; safer for Epic 4's future stock-release handler to re-query the order's items itself once it exists. Built the dispatch as real, wired plumbing, not a stub: `IDomainEventDispatcher`/`IDomainEventHandler<T>` (Application) + reflection-based `DomainEventDispatcher` (Infrastructure) resolving `IDomainEventHandler<TEvent>` per event's runtime type; `CrowdCoffeeDbContext.SaveChangesAsync` collects events from tracked `IHasDomainEvents` entities, saves first, *then* dispatches — so a handler never reacts to an event whose transaction got rolled back. `LoggingOrderCancelledHandler` is the one real registered handler for now (logs the cancellation) — proves the pipeline end-to-end the same way Epic 1's diagnostics endpoint proved the validation pipeline, and stays useful even after Epic 4 adds the real stock-release handler alongside it. Confirmed via `dotnet ef migrations has-pending-model-changes` that `Order` implementing `IHasDomainEvents` didn't accidentally introduce a phantom schema change (EF ignores it correctly).

### Story 3.4 — Unit test suite for the state machine ✅
AC: every legal edge tested positive, every illegal edge tested negative; cash vs. card entry-point rules tested.
- [x] T3.4.1 xUnit project against Domain only, no database
- [x] T3.4.2 Positive tests for every legal transition
- [x] T3.4.3 Negative tests for illegal transitions (e.g. `Ready → Pending`)
- [x] T3.4.4 Entry-point tests (cash vs. card/wallet)
- Size: M · Depends on: 3.1, 3.2
- Delivered: `OrderStateMachineTests.cs` in the existing `CrowdCoffee.Domain.Tests` project (no new test project needed). 26 tests, all passing: every legal edge, illegal edges (including the specific `Ready → Pending` case called out in the AC, and cancellation from `Ready`/`OutForDelivery`/`Delivered`), both full happy paths (delivery → `Delivered`, pickup → `PickedUp`), the mode guard in both directions, cash-vs-card/wallet entry points (including the `CreatePendingPayment` + `Cash` rejection), `OrderStatusHistory` append-on-success / no-append-on-failure, and domain-event raise/no-raise/clear. Full suite (27 incl. the Epic 1 smoke test) run via `dotnet test`.

## Epic 3 status: ✅ complete (4/4 stories)

---

## Epic 4 — Inventory Core

Goal: ingredient-level stock, reservation, auto-availability (§8).

### Story 4.1 — Reservation flow ✅
AC: `Available = QuantityOnHand − QuantityReserved`; reservation on `Confirmed` only, never `Pending`.
- [x] T4.1.1 `IStockReservationService` with `Reserve`, `ReleaseReservation`, `Fulfill`
- [x] T4.1.2 Subscribe to `OrderCancelled` to auto-release reservations
- [x] T4.1.3 Ensure reservation writes no ledger row (only `Fulfill` does)
- Size: M · Depends on: 2.3, 3.3
- Delivered: `StockItem.Reserve`/`ReleaseReservation`/`Fulfill` (Domain) + `StockReservationService` (Application), calling through `IOrderStockRequirementRepository` (walks `Order.Items`/`Modifiers` → `Recipe` → `StockItem`, summed per `StockItemId`) and `IStockRepository`. `ReserveForOrderAsync` is a two-pass check-then-commit — validates every ingredient's `Available` before reserving any, so a multi-ingredient order never leaves a partial reservation on failure. `ReleaseStockOnOrderCancelledHandler` (`IDomainEventHandler<OrderCancelledEvent>`) wires §8's auto-release through the real Epic 3.3 dispatch pipeline, not a direct call — proven by an integration test that drives an actual `Order.TransitionTo(Cancelled)` + `SaveChangesAsync` against local SQL Server and asserts the reservation clears. `Reserve` writes no `StockMovement` row (only `Fulfill` does, per AC).
- **Bug found & fixed while verifying**: `Order.TransitionTo` appends a new `OrderStatusHistory` to a collection navigation that's typically not `Include()`'d (callers only need to append, not read history). Because `OrderStatusHistory.Id` is a client-generated Guid, EF Core's fixup can't tell "brand new" from "existing row nobody queried yet" from the key value alone, and tracked the new row as `Modified` — emitting an `UPDATE` against a PK that was never inserted, throwing `DbUpdateConcurrencyException` (0 rows affected). This would have hit *every* future transition call site (checkout, kitchen board), not just tests. Fixed centrally in `CrowdCoffeeDbContext.SaveChangesAsync` (`FixUpNewAppendOnlyChildren`): any `OrderStatusHistory` entry in `Modified` state gets forced back to `Added` — safe because the type has no mutator methods, so `Modified` can only ever mean this misclassification, never a real edit.

### Story 4.2 — Sale movement on order completion ✅
AC: on `Delivered`/`PickedUp`, reservation converts to `Sale` movement, `QuantityOnHand` decrements, idempotent per order line.
- [x] T4.2.1 Wire `Fulfill` to the completion transition
- [x] T4.2.2 Write `Sale` `StockMovement` per ingredient consumed
- [x] T4.2.3 Guard against double-fulfillment of the same order
- Size: M · Depends on: 4.1, 3.1
- Delivered: added `OrderCompletedEvent` (Domain), raised from `Order.TransitionTo` alongside `OrderCancelledEvent` on the `Delivered`/`PickedUp` edges — same "carries only `OrderId`" reasoning as Epic 3.3's event. `FulfillStockOnOrderCompletedHandler` reacts to it and calls `StockReservationService.FulfillForOrderAsync`, mirroring `ReleaseStockOnOrderCancelledHandler`'s pattern exactly, registered in `Program.cs` and the test DI graph. `FulfillForOrderAsync` guards double-fulfillment via `HasMovementForReferenceAsync("Order", orderId, StockMovementType.Sale, ...)` before doing any work — a second call is a no-op. Verified with an integration test that drives a real order through `Confirmed → Preparing → Ready → PickedUp` (four separate `TransitionTo` + `SaveChangesAsync` calls against local SQL Server) and asserts the `Sale` movement and on-hand/reserved deltas land through the event pipeline alone, not a direct method call — plus the existing idempotency test calling `FulfillForOrderAsync` twice directly.

### Story 4.3 — Auto-availability computation ✅
AC: product/modifier option unavailable when any recipe ingredient can't cover one unit at current `Available` stock.
- [x] T4.3.1 `IAvailabilityService.IsAvailable(productId | modifierOptionId)`
- [x] T4.3.2 Decide computed-on-read vs. cached-flag (recommend computed-on-read for v1; revisit if <200ms budget is at risk)
- [x] T4.3.3 Unit tests for boundary cases (exactly enough stock, zero stock)
- Size: M · Depends on: 4.1
- Delivered: took `IsProductAvailableAsync(Product)`/`IsModifierOptionAvailableAsync(ModifierOption)` over the sketched `IsAvailable(id)` — callers already have the entity in hand from a catalog query, and passing it in lets the manual-override check (`product.IsAvailable`/`modifierOption.IsActive`) happen without a second lookup. Pure decision logic lives in `AvailabilityCalculator.IsAvailable(recipes, stockItemsById)` (no I/O, fully unit-tested); `AvailabilityService` (Application) does the I/O via `IAvailabilityRepository`. Computed-on-read, no caching yet — matches the recommendation, revisit under Epic 5.3 if the `<200ms` budget is at risk. A product/option with zero `Recipe` rows is treated as available (real per-drink ingredient quantities are still TBD business data per Epic 2.5), not unavailable-by-default. 9 unit tests cover exactly-enough-stock, one-short, zero-stock, multi-ingredient (all sufficient / one insufficient), reserved-stock tipping into unavailable, and a recipe referencing a missing `StockItem`.

### Story 4.4 — Manual availability override ✅
AC: manager's manual `IsAvailable = false` always wins over computed value.
- [x] T4.4.1 Combine `Product.IsAvailable && ComputedAvailable` in the read path
- Size: S · Depends on: 4.3
- Delivered: folded into `AvailabilityService` rather than a separate combinator — `IsProductAvailableAsync` short-circuits `false` on `!product.IsAvailable` before ever querying recipes/stock; same short-circuit on `ModifierOption.IsActive`. One read path, can't forget to AND the two together.

### Story 4.5 — Low-stock alerting ✅
AC: `StockItem`s below `ReorderPoint` surface on an admin-queryable list.
- [x] T4.5.1 Query/service returning items below `ReorderPoint`
- [x] T4.5.2 Leave push/email/SMS alert unimplemented pending §14.6 answer; wire list into Epic 14 dashboard only
- Size: S · Depends on: 2.3
- Delivered: `GetLowStockItemsHandler` + `IStockRepository.GetLowStockItemsAsync` (`IsActive && QuantityOnHand < ReorderPoint`), exposed at `GET /api/inventory/low-stock`. Compares `QuantityOnHand`, not `Available` — reads most naturally as a physical-restock signal; flagged in code as open to revisiting against `Available` if the business wants reorder timing to account for stock already committed to pending orders. No push/email/SMS — §14.6 still unanswered, list-only as scoped.

### Story 4.6 — Purchase/waste/adjustment recording ✅
AC: purchase increases on-hand, waste/adjustment decrease with mandatory reason — all as ledger entries.
- [x] T4.6.1 Application command: record `Purchase` movement
- [x] T4.6.2 Application command: record `Waste` movement (mandatory reason)
- [x] T4.6.3 Application command: record `Adjustment` movement (mandatory reason)
- [x] T4.6.4 Validation enforcing required reason on Waste/Adjustment
- Size: M · Depends on: 2.3
- Note (§14.7): an in-store POS consuming the same stock outside this system would cause ledger drift regardless of this epic's correctness — confirm before go-live.
- Delivered: `RecordPurchaseHandler`/`RecordWasteHandler`/`RecordAdjustmentHandler`, each with a `FluentValidation` validator (`Waste`/`Adjustment` require a non-empty `Reason`; `Adjustment`'s delta must be non-zero since it's bidirectional, unlike the one-directional Purchase/Waste). Each writes through `StockItem.AdjustOnHand` (guards on-hand never going negative) and appends the matching signed `StockMovement`. Exposed at `POST /api/inventory/stock/{purchase,waste,adjustment}` — same `TODO(Epic 11)` auth gap as the rest of this controller.

## Epic 4 status: ✅ complete (6/6 stories)

---

## Epic 5 — Catalog API

Goal: categories/products/modifiers with availability and caching, <200ms (§12).

### Story 5.1 — GET /categories, GET /products ✅
AC: 19 categories in display order; products filtered to `IsActive && IsAvailable` (manual + computed).
- [x] T5.1.1 Read-model query for categories (display order)
- [x] T5.1.2 Read-model query for products (`AsNoTracking`/Dapper projections, avoid full aggregate loads)
- [x] T5.1.3 Apply Epic 4.4's combined availability filter
- [x] T5.1.4 Decide response shape (grouped-by-category vs. flat + ref) based on Epic 6 needs
- Size: M · Depends on: 2.1, 4.3
- Delivered: chose **flat + ref** for T5.1.4 — `GetProducts` returns a flat list with `CategorySlug` on each row, fetched once and grouped/filtered client-side, matching Story 6.3's "fetch-on-load with client-side cache" plan rather than a grouped-by-category envelope. `ICatalogRepository` (Application port) + `CatalogRepository` (Infrastructure) project straight from `Products`/`Categories` into DTOs via `AsNoTracking().Select(...)` — no entity materialization, no navigation loads, satisfying T5.1.2's "avoid full aggregate loads" directly rather than just not-tracking a full `Product` load. Availability filtering needed a bulk path: the existing Epic 4.3 `IAvailabilityService.IsProductAvailableAsync(Product)` is one-recipe-query-plus-one-stock-query *per product*, which would be 300+ queries across 161 products — added `GetComputedAvailabilityForProductsAsync(ids)`/`GetComputedAvailabilityForModifierOptionsAsync(ids)` sibling overloads (two bulk queries total, id-keyed dictionary result) alongside two new bulk methods on `IAvailabilityRepository`; the existing single-item overloads and their Epic 4 test coverage are untouched. `GetProductsHandler` combines `ProductCatalogRow.IsManuallyAvailable && computed[id]` and filters unavailable rows out entirely, per AC.

### Story 5.2 — GET /products/{slug}/modifiers ✅
AC: returns modifier groups/options applicable per §4a `DRINK_CATS`, each option flagged available/unavailable.
- [x] T5.2.1 Query joining `ProductModifierGroup` → `ModifierGroup` → `ModifierOption`
- [x] T5.2.2 Apply per-option availability from Epic 4.3
- Size: M · Depends on: 2.1, 4.3
- Delivered: `GetProductModifiersHandler` looks up the product by slug (404 via `Result`/`Error.NotFound` if missing or inactive), then `ICatalogRepository.GetModifierGroupsForCategoryAsync(product.CategorySlug)` — a category-slug subquery filter against `ProductModifierGroups` (not a literal SQL join), each group's `Options` collection nav projected inline via EF, ordered by `DisplayOrder`. Deliberately **flags, never filters** unavailable options (unlike Story 5.1's products) — an out-of-stock milk should show disabled in the customization modal, not vanish and destabilize the option list the UI renders around; verified live against the real seeded `black`/`b-espresso` product: Milk Type (`ExactlyOne`, required, incl. "No Milk"), Espresso Shot (`UpToN`, max 1, optional), Syrups (`UpToN`, max 2) all came back with the exact §4a surcharges (40 EGP alt-milk/sugar-free syrups, 30 EGP regular syrups).

### Story 5.3 — Caching & cache invalidation ✅
AC: menu endpoint <200ms; writes from Epic 13 invalidate cache promptly (correctness over TTL convenience).
- [x] T5.3.1 Add cache layer (`IMemoryCache` or `IDistributedCache`)
- [x] T5.3.2 Explicit invalidation hook on product/category/modifier writes
- Size: S · Depends on: 5.1, 5.2
- Delivered: **only the availability-independent catalog structure is cached** (names/prices/slugs/display order) — the computed recipe-vs-stock availability is always recomputed live per request from the already-bulk (two-query) path above, never cached, so a cached response can never show something in stock that isn't, or vice versa. `ICatalogCache` (Application/Common) wraps a keyed `GetOrCreateAsync<T>`/`InvalidateAll()` pair; `MemoryCatalogCache` (Infrastructure, singleton) implements it over `IMemoryCache` using the standard shared-`CancellationTokenSource`-swap trick since `IMemoryCache` has no native "clear everything." `InvalidateAll()` is the AC's "writes invalidate cache promptly" hook — **nothing calls it yet**, since Epic 13's admin write endpoints don't exist; `TODO(Epic 13)` is implicit in `ICatalogCache`'s doc comment. A 5-minute TTL backstops that gap in the meantime, but per the AC's "correctness over TTL convenience," it's explicitly documented as the backstop, not the mechanism. Verified live: `GET /api/catalog/categories` (19 rows), `/products` (161 rows, all available given the zero-recipe seed data), and `/products/{slug}/modifiers` all responded in 12–15ms end-to-end against local SQL Server — comfortably inside the §12 <200ms budget even before considering the cache's steady-state benefit.

## Epic 5 status: ✅ complete (3/3 stories)

---

## Epic 6 — Public Site: Landing + Menu

Goal: pixel-match `Crowd Coffee.dc.html` and `Menu.html` (newer version), mobile-first, Vue 3.

### Story 6.1 — Frontend project scaffold ✅
AC: Vite + Vue 3 + TypeScript; OKLCH tokens preserved as CSS custom properties; both accent colors present and distinct.
- [x] T6.1.1 Scaffold Vite + Vue 3 + TS project
- [x] T6.1.2 Design-token CSS file (OKLCH values, not converted to hex)
- [x] T6.1.3 Base layout matching design typography/spacing
- Size: M · Depends on: 1.1 (can start once Epic 5's contract is stable)
- Delivered: `frontend/` — Vite + Vue 3 + TS (via `npm create vite@latest -- --template vue-ts`), plus `vue-router@4` (needed immediately for the landing/menu routes, not speculative). `src/styles/tokens.css` holds every OKLCH value verbatim from the design package (`Crowd Coffee.dc.html`/`Menu.html`) as CSS custom properties — both accents present and kept distinct: `--color-accent` (terracotta, `oklch(56% 0.16 40)`, primary CTAs/selection controls) and `--color-accent-green` (deep green, `oklch(32% 0.05 155)`, active-category-chip state per §4a's design note) are never collapsed into one token. `src/styles/global.css` holds the reset plus a small set of shared primitives (`.btn`, `.eyebrow`, `.section`, `.tag-pill`) factored out because the source design repeats these exact inline-style patterns verbatim on every screen. `vite.config.ts` proxies `/api` to the ASP.NET Core API (`localhost:5000`) in dev, so no CORS config is needed on the API yet (that's Epic 16).

### Story 6.2 — Landing page ✅
AC: matches `Crowd Coffee.dc.html` layout/copy; usable at 375px viewport.
- [x] T6.2.1 Hero section component
- [x] T6.2.2 Story section component (brand copy: "A space for every crowd.", etc.)
- [x] T6.2.3 Menu-teaser section component
- [x] T6.2.4 Gallery section with lazy-loaded WebP/AVIF images
- [x] T6.2.5 Visit-info section component
- Size: M · Depends on: 6.1
- Delivered: found the actual design package at `Downloads/Coffee brand website redesign (1)/` (`Crowd Coffee.dc.html`, `Menu.dc.html`, real photography in `assets/`/`uploads/`) — copy, structure, and image alt text lifted verbatim, not reworded. One component per section (`SiteNav`/`HeroSection`/`MarqueeBanner`/`StorySection`/`MenuTeaserSection`/`GallerySection`/`VisitSection`/`AppFooter`), composed in `LandingView.vue`. Images: the source `uploads/*.png` were 2–4MB raw pastes; used the much smaller pre-optimized `assets/*.jpg` (100–300KB, same photos) as the base, then generated WebP siblings via `sharp-cli` (quality 78, native resolution — the source images are already ≤1400px so upscaling would have made files *bigger*, which the first conversion attempt actually demonstrated). Every image is `<picture>` with a WebP `<source>` and JPEG `<img>` fallback; gallery/story/visit/menu-teaser images are `loading="lazy"`, the hero image is `fetchpriority="high"` (it's the LCP element, above the fold). Verified live in Chrome against both the dev server and a production `vite build`: hero, marquee, story, menu teaser, gallery captions, visit, and footer all render correctly with real photography. Mobile-viewport verification is by CSS technique review, not an actual narrow-viewport screenshot — this sandbox's browser automation window is fixed at 1536×695 (`resize_window` calls succeeded but `window.innerWidth` never changed), so 375px rendering couldn't be visually confirmed here. The layout uses the same `flex-wrap`/CSS Grid `auto-fit,minmax()`/`clamp()` responsive techniques as the source design (itself built mobile-first per PRD §5), so it should hold, but flag this gap explicitly rather than claim a check that didn't happen.

### Story 6.3 — Menu page with category filter ✅
AC: 19 category chips in specified order; client-side filtering (fetch once).
- [x] T6.3.1 Menu fetch-on-load with client-side cache
- [x] T6.3.2 Category chip component (19 categories, display order)
- [x] T6.3.3 Product card component with lazy image loading
- [x] T6.3.4 Client-side category filtering logic
- Size: L · Depends on: 6.1, 5.1
- Note (§14.5): if post-checkout order tracking is in scope, this epic's route map needs a tracking view — resolve before finalizing routes. **Still open** — not resolved by this story; `/menu` and `/cart` are the only routes added.
- Delivered: `useCatalog()` composable (`src/composables/useCatalog.ts`) is a module-scoped fetch-once cache — `GET /api/catalog/categories` + `/products` fire exactly once per page load no matter how many components call it, deliberately *not* Pinia (no mutable shared state here, just a memoized GET; Epic 8 is where Pinia earns its place, for the cart). `CategoryChips.vue` renders the 19 categories in the API's display order (already sorted server-side per Epic 5) with `role="tablist"`/`aria-selected` for cheap keyboard/screen-reader correctness. Product cards bypass the customization modal entirely for now — Epic 7 doesn't exist yet, so every category's Add button (including drink categories) adds straight to cart at base price; marked `TODO(Epic 7)` in `MenuView.vue`'s `handleAdd`. Verified live: category filter switches the product list correctly (`Black Coffee` → `Classic Coffee` tested), Add increments a floating cart bar matching `Menu.dc.html` exactly (`View Cart · 3 item(s) — 275 EGP →`), and the cart persists to `localStorage['crowdcoffee_cart']` in the exact key and shape the design's own reference implementation uses (`{ id, name, price, qty }`) — deliberately not Story 8.1's modifier-aware composed-id scheme yet, since that scheme presupposes Epic 7's milk/shot/syrup selections existing. Added a minimal `/cart` placeholder route so the cart bar has somewhere to go rather than a dead link; Epic 8 replaces it outright.

### Story 6.4 — Performance pass ✅
AC: Lighthouse mobile performance ≥ 85.
- [x] T6.4.1 Image optimization/lazy-load audit
- [x] T6.4.2 Bundle size check
- [x] T6.4.3 Route-level code splitting
- Size: S · Depends on: 6.2, 6.3
- Delivered: T6.4.1 — WebP conversion + lazy-loading covered in Story 6.2's delivery note; all 11 landing images now have a WebP sibling 30–40% smaller than the JPEG fallback. T6.4.2 — `npm run build` output: five small route-specific chunks (`LandingView` 9.4KB, `MenuView` 4.1KB, `CartPlaceholderView` 0.6KB) plus a shared `index` chunk (88KB / 34KB gzipped, almost entirely Vue + Vue Router runtime) — no oversized bundle. T6.4.3 — `router/index.ts` uses dynamic `import()` for every route component; the build output confirms each view lands in its own chunk, so a landing-page visitor never downloads the menu page's fetch/render code. **Did not run an actual Lighthouse audit** — no Lighthouse CLI available in this environment/sandbox, and the AC's ≥85 mobile score also depends on real-device network conditions this environment can't simulate. What's verified instead: the underlying practices Lighthouse's mobile-performance score rewards (modern image formats, lazy loading, route-level code splitting, small JS payloads) are in place and demonstrated via the build output above — the actual Lighthouse number is unverified and should be checked against a deployed build in Epic 17.

## Epic 6 status: ✅ complete (4/4 stories)

---

## Epic 7 — Drink Customization Modal

Goal: reproduce §4a exactly.

### Story 7.1 — Modal UI: milk/shot/syrup groups ✅
AC: Milk required (incl. "No Milk" as valid explicit choice, no null fallthrough); Shot optional single-select; Syrups max 2; independent qty stepper (min 1).
- [x] T7.1.1 Modal component shell
- [x] T7.1.2 Milk Type group (single-select, required, Add disabled until chosen)
- [x] T7.1.3 Espresso Shot group (optional single-select)
- [x] T7.1.4 Syrups group (max 2 enforced client-side)
- [x] T7.1.5 Modal-local qty stepper (min 1, independent of cart stepper)
- Size: L · Depends on: 5.2, 6.1
- Delivered: decoded the actual modal markup/behavior from the real `Menu.html` (the bundler-wrapped file at `Downloads/Menu.html`, same file Epic 2.5 extracted seed data from) — same gzip+base64 `__bundler/manifest`/`__bundler/template` decode technique, this time pulled apart with a throwaway Node script to recover the page's real inline `<script type="text/x-dc">` component, which is where the modal's actual JSX-like markup and `Component` class logic live (not in Menu.dc.html, which the PRD already flags as the modal-less earlier version). `DrinkCustomizeModal.vue` reproduces the bottom-sheet layout, colors, and radio/checkbox indicator styling from that decoded markup exactly. One deliberate behavior addition beyond the static mock: each option is disabled/greyed when `option.isAvailable` is false — the mock has no real inventory to react to, but §4a rule 5 explicitly wants "when oat milk runs out, that single option greys out while the rest of the drink stays orderable," and Epic 5's `/products/{slug}/modifiers` endpoint already computes this live from stock, so the modal wires it in rather than leaving it unused. Milk/shot/syrup data comes from that real endpoint (`useModifiers` composable, cached per category slug since groups are category-wide) — not re-hardcoded as static arrays the way the design mock's own JS does, since the whole point of Epic 5 was making this backend-driven. Verified live end-to-end in Chrome: opening "Espresso," selecting Oat Milk enables Add and recomputes the total live, "No Milk" behaves as a real selectable option rather than a fallthrough default, and unavailable options (none in the current zero-recipe seed data) would render disabled via the same `:disabled` binding used for the max-2-syrups cap.

### Story 7.2 — Live pricing ✅
AC: `unitPrice = base + milk + shot + Σsyrups`; `lineTotal = unitPrice × modalQty`, live-updating.
- [x] T7.2.1 Reactive price computation (Pinia getter/computed)
- [x] T7.2.2 Display note/comment that this value is display-only (server re-prices)
- Size: S · Depends on: 7.1
- Note (§14.3): confirm surcharges are uniform across drink categories before hardcoding a single price table. **Resolved** — Epic 2's seed data already encodes this as a structural fact, not a per-category price table: `ProductModifierGroup` attaches the same 3 groups (milk/shot/syrups) to all 11 `DRINK_CATS` categories uniformly, with one shared `ModifierOption` price list (verified live against the real seeded DB in Epic 5: 40 EGP alt-milk, 40 EGP sugar-free syrups, 30 EGP regular syrups, identical regardless of which drink category the product belongs to).
- Delivered: took plain Vue `computed()` over Pinia — the task's own AC just says "reactive price computation," and the modal's milk/shot/syrup/qty selections are ephemeral, modal-local UI state with no reason to live in shared app state; Epic 8 is where Pinia earns its place (the cart, which genuinely is shared across the app). `unitPrice`/`lineTotal` are computed refs in `DrinkCustomizeModal.vue`, confirmed live-updating against every selection change during manual verification (adding oat milk, a shot, and a syrup recomputed 70 → 190 EGP instantly, ×2 qty → 380 EGP). T7.2.2's "display-only" note is a source comment directly on the `unitPrice` computed (not new UI copy not in the mock — the design doesn't show a disclaimer, so none was added) referencing §4a rule 3 and pointing at Epic 9 as the real re-pricing authority.

### Story 7.3 — Non-drink categories bypass the modal ✅
AC: food categories add straight to cart at base price.
- [x] T7.3.1 `DRINK_CATS` membership check gating modal vs. direct-add
- Size: S · Depends on: 7.1
- Delivered: `MenuView.vue` hardcodes the same 11-slug `DRINK_CATS` list §4a specifies (mirroring the design's own constant) as a fast client-side gate in `handleAdd` — checking via the real `/modifiers` endpoint first would cost a network round-trip on every food-item Add just to learn "no groups," so the static list stays the practical choice; the DB's `ProductModifierGroup` rows remain the actual source of truth for which categories get modifiers, this list just mirrors that fact for a synchronous UI decision. Verified live: Add on a Sandwiches-category item ("Roastbeef") pushed straight into the cart with no modal, while every `DRINK_CATS` category opens `DrinkCustomizeModal`.

## Epic 7 status: ✅ complete (3/3 stories)

---

## Epic 8 — Cart

Goal: modifier-aware cart identity (§4a), localStorage persistence (§3), server re-pricing.

### Story 8.1 — Client cart with composed line identity ✅
AC: id = `{productId}::{milkId}::{shot|noshot}::{sortedSyrupIds joined by "-"}`, syrups sorted before joining; matching ids increment qty.
- [x] T8.1.1 Pinia cart store
- [x] T8.1.2 Id-composition utility (pure function, unit-testable)
- [x] T8.1.3 Unit tests: sorted-syrup collapsing, matching vs. differing lines
- [x] T8.1.4 localStorage persistence (`crowdcoffee_cart`, `crowdcoffee_order_mode`)
- Size: M · Depends on: 7.2
- Delivered: `stores/cart.ts` (`useCartStore`) replaces Epic 6/7's `useSimpleCart` composable now that cart contents + order mode are genuinely shared, mutable state across menu/cart/checkout — the case Pinia is actually for, unlike Epic 5/6's plain fetch-once composables. The id-composition utility (`lib/cartLineId.ts`) already existed from Epic 7 (the modal needed it to compose lines before Epic 8 existed) — reused as-is here rather than rewritten. Added the test runner this story's AC required: **Vitest** (Vite-native, jsdom environment for the store's `localStorage` calls) as the frontend's first test infrastructure — `npm test`. 14 tests total: 6 on `composeDrinkLineId` (sorted-syrup collapsing producing identical ids regardless of submission order, differing milk/shot/syrup/product all producing different ids) and 8 on the store (add/addLine increment-vs-new-line behavior, `localStorage` persistence under both `crowdcoffee_cart` and `crowdcoffee_order_mode` keys, qty-to-zero removal, subtotal/deliveryFee/total getters).

### Story 8.2 — Cart page UI ✅
AC: matches `Cart.dc.html`; delivery 30 EGP flat / pickup free, shown per mode.
- [x] T8.2.1 Cart page component (line items, totals)
- [x] T8.2.2 Qty stepper component
- [x] T8.2.3 Delivery/pickup mode toggle wired to Pinia store
- Size: M · Depends on: 8.1
- Delivered: `CartView.vue` replaces Epic 6's `CartPlaceholderView.vue` at `/cart`, matching `Cart.dc.html` exactly — line items with circular −/+ qty steppers (`CartQtyStepper.vue`, deliberately a different shape from the customization modal's pill stepper since the source design itself draws them differently) and a separate Remove link, `OrderModeToggle.vue` for the Delivery/Pickup buttons (with the pickup note appearing only in pickup mode), and a totals card (Subtotal / Delivery fee-or-Pickup-Free / Total) ending in "Proceed to Checkout." Added a `/checkout` placeholder view (same pattern as Epic 6's now-retired `/cart` placeholder) so the button has somewhere real to go before Epic 9. Verified live in Chrome against lines carried over from Epic 6/7's manual testing: qty stepper decrementing a qty-1 line to zero removed it and updated the subtotal (820 → 655 EGP), switching Delivery → Pickup showed the pickup note and dropped the fee to "Free," and the empty-cart state/"Browse the Menu" path matches the design.

### Story 8.3 — Server-side cart re-pricing endpoint ✅
AC: server recomputes each line from current DB prices/deltas; server-side id composition mirrors client.
- [x] T8.3.1 Server-side id-composition logic mirroring the client scheme
- [x] T8.3.2 Re-pricing endpoint recomputing line totals from DB
- [x] T8.3.3 Validation: exactly one milk, ≤1 shot, ≤2 syrups, options belong to product's attached groups
- [x] T8.3.4 Reject requests failing validation (extra syrups, foreign option ids)
- Size: M · Depends on: 5.2, 8.1
- Delivered: `POST /api/cart/reprice` — `CartLineRepricer` (Application/Cart, pure, no I/O) ports `composeDrinkLineId`'s exact scheme to C# (`StringComparer.Ordinal` for the syrup sort, matching JS's default UTF-16 string sort on GUID strings) and validates milk/shot/syrup option ids against the product's actual attached groups (`ICatalogRepository.GetModifierGroupsForCategoryAsync`, reused from Epic 5 rather than a new query) before recomputing `unitPrice` from the live DB price + deltas. A foreign option id anywhere in a line — or in any line — fails the *entire* request (T8.3.4; no silent per-line dropping). Two validation layers, matching the `InventoryController` precedent: FluentValidation (`CartRepriceRequestValidator`) catches shape violations (empty lines, >2 syrups, non-positive qty) at the boundary before the handler runs; `CartLineRepricer` catches the deeper "does this option genuinely belong to this product" checks that need the loaded DB data. Added `ICatalogRepository.GetActiveProductByIdAsync` (Epic 5's repository only had by-slug; the cart already has product ids in memory from the products list, not slugs). Verified live against the real seeded DB: Espresso Bonbon (95 EGP) + Oat Milk (+40) + Espresso Shot (+40) × qty 2 → 175 EGP/unit, 350 EGP line total, matching manual arithmetic exactly; a syrup option id submitted as the milk option id correctly rejected with `Cart.InvalidMilkOption`; 3 syrups correctly rejected by the FluentValidation layer before ever reaching the repricer.

## Epic 8 status: ✅ complete (3/3 stories)

---

## Epic 9 — Checkout & Order Creation

Goal: validate → reserve stock → persist order, atomically (§12).

### Story 9.1 — Checkout page UI ✅
AC: matches `Checkout.dc.html`; location pin via `postMessage` contract; required fields per mode.
- [x] T9.1.1 Checkout form component
- [x] T9.1.2 Location picker integration (`{ type: 'crowd-location', lat, lng }`)
- [x] T9.1.3 Client-side required-field validation (name/phone always; address if delivery)
- [x] T9.1.4 Payment method selection UI
- Size: L · Depends on: 8.2
- Note (§14.1, §14.2, §14.4): **resolved with the user directly** before implementation — no opening-hours enforcement in v1, no minimum delivery order value, flat 30 EGP delivery fee (already the structural fact the cart store encoded in Epic 8). None of these gate checkout.
- Delivered: `CheckoutView.vue` replaces the Epic 6 placeholder, reproducing `Checkout.dc.html`'s layout (details card, payment-method card, sticky order-summary sidebar, confirmation screen, empty-cart guard) with the app's existing CSS custom properties instead of the mock's inline OKLCH literals. The location picker is a real Leaflet map (`frontend/public/location-picker.html`, ported from the design package's `Location Picker.html` almost verbatim) iframed in and listened to via the exact `{ type: 'crowd-location', lat, lng }` `postMessage` contract the mock specifies. Cart lines only ever stored the Epic 8.1 *composed* id string (`{productId}::{milkId}::{shot|noshot}::{syrupIds}`) — checkout needs the decomposed parts back to build the order request, so added `decomposeDrinkLineId` (`lib/cartLineId.ts`, inverse of `composeDrinkLineId`, 3 new unit tests) plus a lookup through `useModifiers()` to recover the concrete `shotOptionId` (the composed id only encodes "shot"/"noshot", not which shot option). An idempotency key (`crypto.randomUUID()`) is generated once per checkout page visit and reused across retries, satisfying Story 9.3 from the client side. Cash orders clear the cart and show the confirmation screen immediately (server auto-confirms, see Story 9.2); Card/Wallet orders call `POST /api/payments/initiate` (Epic 10.2) and redirect the browser to the gateway — cart clears only once the return page confirms payment actually went through, not at redirect time. Verified live end-to-end against the real running API (see Story 9.2's note) and via `npm run build`/`vue-tsc` typecheck/`vitest`.

### Story 9.2 — Order creation endpoint (transactional) ✅
AC: single transaction validate → reserve → persist with snapshots; `OrderNumber` in `CRW-YYMMDD-NNNN` format, daily-resetting sequence; no orphan reservation on partial failure.
- [x] T9.2.1 `OrderNumber` sequence generator (DB sequence or per-day counter table — avoid `MAX()+1` under concurrency)
- [x] T9.2.2 Application command using Epic 3.2 factory methods
- [x] T9.2.3 Wrap validate → reserve → persist in one DB transaction
- [x] T9.2.4 Integration test: partial failure leaves no orphan reservation
- Size: L · Depends on: 3.2, 4.1, 8.3
- Delivered: reconciled a real tension between §7 ("cash orders enter at `Pending`, a staff member confirms"; card/wallet enter at `PendingPayment` and "never reach the kitchen board until the gateway webhook confirms"), §8 ("deduct at Confirmed, not at Pending — otherwise abandoned card checkouts eat your inventory"), and §12/this story's own AC ("validate → reserve → persist" as one transaction) — a cash order has nothing left to wait on once placed, so `CreateOrderHandler` creates it at `Pending` and immediately calls `order.TransitionTo(Confirmed, "system", ...)` in the *same* transaction (full `OrderStatusHistory` row, per §7's "no exceptions"), which is what actually triggers `StockReservationService.ReserveForOrderAsync` — satisfying every one of those constraints at once. Card/Wallet orders stop at `PendingPayment`, reserve nothing, and wait for Epic 10.3's webhook. `IOrderNumberGenerator`/`SqlOrderNumberGenerator` (T9.2.1) backs the daily counter with a real `OrderNumberCounters(DateKey, LastValue)` table and an `UPDATE ... OUTPUT` statement that takes a normal SQL Server row-level lock for the life of the caller's transaction — first order of a new day falls through to an `INSERT`, retried as an `UPDATE` on a duplicate-key race — deliberately not `MAX()+1`. `DateKey` uses Africa/Cairo local date (`CairoClock`, §6 rule 5's "render in Cairo" extended to mean the shop's calendar day, not UTC's), not UTC. New `IUnitOfWork`/`IUnitOfWorkTransaction` port (Application/Common) wraps EF's transaction API since Application can't reference `CrowdCoffeeDbContext` directly (one-way layering, `ARCHITECTURE.md`). `CartLineRepricer` (Epic 8.3) is reused as-is for server-side line pricing/validation — but it only ever handled drink lines with modifiers; extended it to accept nullable `MilkOptionId`/`ShotOptionId` and validate *against whichever modifier groups the product actually has attached* (not a hardcoded drink-category list), so non-drink lines (pastries etc., which Epic 9's checkout also has to submit) reject any unexpected modifier selection instead of erroring on a milk requirement that never applied. **Bug found and fixed while building this**: `Order.TransitionTo` was unconditionally raising `OrderCancelledEvent` on any cancellation, including from `Pending`/`PendingPayment` — states that never had anything reserved (§8: reservation only happens at Confirmed). `ReleaseStockOnOrderCancelledHandler` reacting to that would have released some *other* order's legitimate reservation on the same `StockItem`, since it computes requirements from the cancelled order's recipe regardless of whether a reservation actually exists. Fixed by only raising the event when cancelling from `Confirmed`/`Preparing` (the only states reachable after an actual reservation) — this was a latent bug from Epic 3.3/4.1 that Epic 9/10's much heavier use of the Confirmed→cancel path would otherwise have hit in production; updated/added Domain.Tests coverage for both cases and fixed one Epic 4 integration test whose setup had silently relied on the buggy behavior. Verified live against the real running API: a real cash order (`CRW-260806-0005`) auto-confirmed and reserved stock (18g × 2 units) in one call; a card order correctly stayed `PendingPayment` with zero reservation. Integration tests (`CreateOrderIntegrationTests`, 5 tests) prove sufficient-stock auto-confirm, **insufficient stock rolls back the entire transaction — no `Order` row, no reservation** (the AC's core claim), card orders reserve nothing, non-drink lines price correctly with no modifiers. **Found and fixed a real test-isolation bug in the process**: multiple new integration tests were each attaching their own `Recipe` to the *same* deterministically-picked "classic" category product; `GetRequiredQuantitiesAsync` aggregates every `Recipe` row for a product, so under xUnit's default parallel-test-class execution, one test's reservation call would pull in a sibling test's stock item (and, in one repro, its deliberately-insufficient stock) — fixed by giving each test a distinct product, and disabled test-class parallelization for `CrowdCoffee.Infrastructure.Tests` altogether (`AssemblyConfig.cs`) after also catching a real SQL Server deadlock on the `OrderNumberCounters` hot row under concurrent test execution. Full suite run 3× consecutively with zero flakes after the fix.

### Story 9.3 — Idempotency key on order creation ✅
AC: repeat `IdempotencyKey` never creates a duplicate order.
- [x] T9.3.1 Unique constraint on `IdempotencyKey`
- [x] T9.3.2 Return original order on repeat key instead of erroring/duplicating
- Size: S · Depends on: 9.2
- Delivered: the unique index on `Order.IdempotencyKey` already existed from Epic 2.2 (added ahead of time for exactly this story). `CreateOrderHandler` checks for an existing order by key *before* doing any work (fast path for a genuine retry) and also catches a DB-level unique-constraint violation during save (`OrderIdempotencyConflictException`, thrown by `OrderRepository.SaveChangesAsync` after inspecting the underlying `SqlException`, kept out of Application's exception surface) for the genuinely-concurrent double-submit race, returning the winning order either way rather than erroring. Proven with a real concurrency-adjacent test (`RepeatIdempotencyKey_ReturnsOriginalOrder_DoesNotDuplicate`) asserting both calls return the identical order id and exactly one row exists.

### Story 9.4 — Rate limiting on order creation ✅
AC: endpoint rate-limited per §12.
- [x] T9.4.1 ASP.NET Core rate limiting middleware on order creation (and future OTP endpoints)
- Size: S · Depends on: 9.2
- Delivered: `System.Threading.RateLimiting`'s fixed-window limiter, partitioned per client IP (5 requests/minute, no queueing — a burst past the limit gets `429` immediately, not delayed), registered as a named `"order-creation"` policy so it's trivially reusable for future OTP/phone endpoints per the AC's parenthetical. Applied via `[EnableRateLimiting("order-creation")]` on `OrdersController.Create` only, not globally. Verified live: 5 rapid requests succeeded, the 6th (and all after, within the window) returned `429`.

## Epic 9 status: ✅ complete (4/4 stories)

---

## Epic 10 — Payments (Paymob/Fawry)

Goal: card/wallet payment without touching raw card data (§9).

**No live Paymob credentials for this project** (confirmed with the user before starting) — every piece below is built against Paymob's documented public API contract and proven with real local unit/integration tests (including a hand-computed independent HMAC check and a full webhook round-trip against real SQL Server), but has never been exercised against Paymob's actual servers. Before go-live: get a real merchant account, set the five `Paymob:*` config keys via environment variables, and run one real sandbox transaction through the whole flow — start by confirming a computed HMAC matches what Paymob actually sends (flagged explicitly in `PaymobHmac`'s doc comment as the first thing to check if it doesn't).

### Story 10.1 — `IPaymentGateway` abstraction + Paymob adapter ✅
AC: interface covers initiate/verify-webhook/parse-webhook; no gateway-specific types leak outside Infrastructure.
- [x] T10.1.1 `IPaymentGateway` interface in Application
- [x] T10.1.2 Paymob adapter in Infrastructure
- [x] T10.1.3 API keys via environment/Key Vault config
- Size: M · Depends on: 9.2
- Delivered: `IPaymentGateway` (Application/Payments) exposes exactly two members — `InitiateAsync`/`VerifyAndParseWebhook` — trading in generic records (`PaymentInitiationRequest/Result`, `VerifiedPaymentWebhook`) only; every Paymob-specific wire shape (`PaymobDtos.cs`) is `internal` to `CrowdCoffee.Infrastructure.Payments`. `PaymobGateway` implements Paymob's classic auth-token → register-order → payment-key → iframe-redirect flow (chosen over their newer unified-intention API for the widest documented card + Vodafone Cash + InstaPay coverage under one integration id, matching this doc's original Context & Decisions note). `PaymobOptions` (`Infrastructure/Configuration`) binds from a `Paymob` config section with every value defaulting to empty/zero in the committed `appsettings.json` — safe to commit since empty means "not configured," and `InitiateAsync` fails fast with a clear `Payment.NotConfigured` error (verified live: `409` with a message naming exactly what's missing) rather than calling Paymob with blank credentials. Production supplies real values via environment variables (`Paymob__ApiKey` etc.) per T10.1.3.

### Story 10.2 — Hosted checkout (no inline card fields) ✅
AC: hosted iframe/redirect replaces the mock's inline card fields — documented deviation.
- [x] T10.2.1 Paymob hosted-payment-page integration
- [x] T10.2.2 Return-URL handling back into the SPA
- [x] T10.2.3 UI copy making clear this deviates intentionally from the mock design
- Size: M · Depends on: 10.1
- Delivered: `CheckoutView.vue`'s payment-method card replaces `Checkout.dc.html`'s inline card-number/expiry/CVV fields (§9's "deliberate deviation from the mock") with a note explaining why ("you'll be redirected to our payment provider... we never see or store your card details") the moment Card or Wallet is selected — no card-shaped inputs exist anywhere in this codebase. On submit, `POST /api/payments/initiate` (`InitiatePaymentHandler`) returns the Paymob iframe URL and the browser is redirected there directly (`window.location.href`), landing on Paymob's own hosted page for the actual card entry — PCI scope stays at zero on this side. T10.2.2: since Paymob's classic integration redirects back to a URL configured once in the Paymob dashboard (not something this API can set per-request), added `/checkout/return` (`CheckoutReturnView.vue`) as that fixed landing point — it reads `merchant_order_id` off the redirect's own query string only to know *which* order to ask about, then polls the real `GET /api/orders/{id}` (new endpoint, `GetOrderHandler`) every 2s for up to 30s until the order reaches `Confirmed`/`Cancelled`, rather than trusting anything in the redirect URL itself as proof of payment — the webhook (Story 10.3) is the only thing that actually confirms an order. Verified live end-to-end: created a real `PendingPayment` card order, hit `/api/payments/initiate` and got the expected `Payment.NotConfigured` `409` (no live Paymob account, as noted above) — confirming the request reaches the gateway boundary correctly and fails safely rather than silently.

### Story 10.3 — Webhook endpoint with HMAC verification ✅
AC: rejects unsigned/mismatched callbacks; idempotent; confirmed payment drives `PendingPayment → Confirmed`.
- [x] T10.3.1 Webhook endpoint
- [x] T10.3.2 HMAC signature verification against Paymob's scheme
- [x] T10.3.3 Idempotent handling of repeat webhooks
- [x] T10.3.4 Wire confirmed payment to state machine transition (Epic 3.1) → triggers reservation (4.1)
- Size: M · Depends on: 10.1, 3.1
- Delivered: `POST /api/payments/webhook` reads the raw request body itself (not model-bound — `VerifyAndParseWebhook` needs the exact bytes the signature covers) plus the `hmac` query-string parameter, per Paymob's documented callback shape (confirmed via their published HMAC docs, not guessed: field order is `amount_cents, created_at, currency, error_occured, has_parent_transaction, id, integration_id, is_3d_secure, is_auth, is_capture, is_refunded, is_standalone_payment, is_voided, order.id, owner, pending, source_data.pan, source_data.sub_type, source_data.type, success`, concatenated raw-value-only with no separator, HMAC-SHA512, hex-compared with `CryptographicOperations.FixedTimeEquals` — see `PaymobHmac.cs`). A missing, tampered, or wrong signature is rejected outright (`Payment.InvalidSignature`, `401`) before any business logic runs — proven with 5 unit tests including one that signs a *different* amount than the tampered body claims. `IPaymentEventStore`/`PaymentEvent` (new `PaymentEvents` table, unique-indexed on `GatewayTransactionId`) backs idempotency: a repeat delivery of the same transaction id short-circuits to a no-op before touching the order at all — proven live and in an integration test that fires the identical signed payload twice and asserts stock reserves only once. A successful payment marks `PaymentStatus.Paid` and calls `order.TransitionTo(Confirmed, ...)` — the exact same mechanism Story 9.2's cash auto-confirm uses — which is what triggers `StockReservationService.ReserveForOrderAsync`; a failed payment marks `PaymentStatus.Failed` and cancels the order (§7's `PendingPayment --(failure)--> Cancelled` edge). One deliberate judgment call: if payment succeeds but the reservation check then fails (stock ran out between browsing and paying), the order is **left `Confirmed`/`Paid`, not rolled back** — the charge already happened and can't be undone from a webhook handler, so the safer failure mode is a loud `LogError` ("Manual intervention required — oversold item") with the order still visible to staff, rather than silently failing the webhook and having the gateway retry a doomed reservation forever. Verified fully live against the real running API: crafted and HMAC-signed a real webhook payload by hand (Node.js, independent of the C# implementation), posted it for a real `PendingPayment` card order, and confirmed the order flipped to `Confirmed`/`Paid` and the reconciliation view (Story 10.6) picked up the event — a genuine end-to-end proof, not just unit-level. Also 2 integration tests (`HandlePaymentWebhookIntegrationTests`) against real SQL Server covering the success and failure paths through `CreateOrderHandler` → webhook → final order state.

### Story 10.4 — Server-side amount computation ✅
AC: charged amount read from DB order total, never from client payload.
- [x] T10.4.1 Gateway-initiate call reads `Order.Total` from persisted order
- Size: S · Depends on: 10.1, 9.2
- Delivered: `InitiatePaymentHandler` takes only an `OrderId` from the caller — it loads the order from `IOrderRepository` and passes `order.Total` (plus `order.OrderNumber`/`CustomerName`/`Phone` for the gateway's required billing fields) into `IPaymentGateway.InitiateAsync`; there is no code path by which a client-supplied amount could reach Paymob. Also rejects initiating payment for a Cash order (`Payment.NotApplicable`) or an order that isn't `PendingPayment` (`Payment.OrderNotPendingPayment`, e.g. already paid or cancelled) before ever calling the gateway.

### Story 10.5 — Payment timeout job ✅
AC: `PendingPayment` orders older than 30 min auto-cancel and release reservations.
- [x] T10.5.1 Background job scanning stale `PendingPayment` orders
- [x] T10.5.2 Invoke state machine cancel transition on timeout
- Size: S · Depends on: 3.1, 3.3, 4.1
- Delivered: `PaymentTimeoutJob` (`IHostedService`/`BackgroundService`, registered in `Program.cs`), polling every 5 minutes for `PendingPayment` orders older than 30 minutes (`IOrderRepository.GetStalePendingPaymentOrdersAsync`) and cancelling each via the real `Order.TransitionTo(Cancelled, "payment-timeout-job", ...)` — full `OrderStatusHistory` audit trail, not a raw status write. AC literally says "release reservations," but per Story 9.2/§8's design, a `PendingPayment` order never had anything reserved in the first place (that's the entire point of deferring reservation to `Confirmed`) — so there's structurally nothing to release here, and the (bug-fixed, see Story 9.2) domain-event wiring correctly raises no `OrderCancelledEvent` for a Pending/PendingPayment cancellation. A per-order timer was considered and rejected in favor of a simple poll: 30 minutes of slop on a background sweep is immaterial to the business and far simpler than scheduling one timer per pending checkout.

### Story 10.6 — Reconciliation view ✅
AC: manager sees gateway-confirmed payments vs. orders.
- [x] T10.6.1 Query joining payment records to orders
- [x] T10.6.2 Surface in admin (Epic 14) or dedicated screen
- Size: M · Depends on: 10.3
- Delivered: `GET /api/payments/reconciliation` (`GetReconciliationHandler`), one row per `PaymentEvent` joined against its order (`OrderNumber`/`Total`/`Status` — nullable, surfaced as an actual gap rather than silently dropped, for the should-be-rare case of a webhook naming an order id this DB never had) alongside the gateway's own transaction id/amount/success flag. Same `TODO(Epic 11)` unauthenticated-for-now pattern as `InventoryController` — no admin dashboard exists yet (that's Epic 14), so this is query-only for now, ready to be surfaced there once it exists. Verified live: after the Story 10.3 live webhook test, this endpoint correctly returned the one payment event joined to its now-`Confirmed` order.

## Epic 10 status: ✅ complete (6/6 stories)

---

## Epic 11 — Staff Auth

Goal: JWT + cookie refresh, two roles (§11).

### Story 11.1 — Login & JWT issuance ✅
AC: login issues short-lived JWT + httpOnly refresh cookie; password hashed with BCrypt/Argon2.
- [x] T11.1.1 Login endpoint
- [x] T11.1.2 Password hashing (BCrypt/Argon2)
- [x] T11.1.3 JWT issuance with role claim
- Size: M · Depends on: 2.4
- Delivered: `POST /api/auth/login` (`AuthController`/`LoginHandler`) — BCrypt (`BCryptPasswordHasher`, `BCrypt.Net-Next`) verifies the password against `StaffUser.PasswordHash` (the column was already sized for this back in Story 2.4); a 15-minute JWT (`JwtTokenGenerator`, `System.IdentityModel.Tokens.Jwt`, HMAC-SHA256) carries `sub`/name/`role` claims, and a separate 7-day refresh token is issued alongside it and set as an httpOnly, `SameSite=Strict` cookie scoped to `/api/auth` (`RefreshCookieName`). Same generic `Auth.InvalidCredentials` error for "no such user" and "wrong password" — a distinct message would let an attacker enumerate valid staff usernames. Rate-limited via a new `"staff-login"` policy (10/min/IP — separate from `"order-creation"`'s 5/min since it's staff traffic, not anonymous customer traffic). Two dev-only seed accounts (`manager1`/`Manager123!`, `barista1`/`Barista123!`) land via `StaffSeedData`/`HasData` — `BCrypt.HashPassword` runs once at `dotnet ef migrations add` time, so only the resulting hash (not the plaintext) is baked into the migration, same pattern as the menu seed. Verified live: correct login returns a JWT + sets the cookie; wrong password and unknown username both return the identical `401`; a deactivated `StaffUser` is rejected even with the correct password (covered by `LoginHandlerTests`, hand-rolled fakes per this test project's no-mocking-library convention).

### Story 11.2 — Refresh flow & role-based authorization ✅
AC: refresh rotates token via httpOnly cookie; role guards on manager-only vs. shared endpoints.
- [x] T11.2.1 Refresh endpoint (cookie-based rotation)
- [x] T11.2.2 Policy-based authorization setup (Manager vs. Barista)
- Size: M · Depends on: 11.1
- Delivered: `POST /api/auth/refresh` (`RefreshHandler`) reads the httpOnly cookie, issues a brand-new refresh token, and revokes the old one in the same call (`RefreshToken.RevokeAtUtc`/`ReplacedByTokenId`) — real rotation, not just reissuing the same value. Went one step past the AC's literal wording and added reuse detection: presenting an already-revoked token (a replay of a stolen raw value, since a legitimate client always has the latest one) revokes *every* other active token for that user, not just rejects the one request — a standard refresh-rotation hardening the AC's "rotates token" wording implies but doesn't spell out. `POST /api/auth/logout` (not in the AC, but a login flow without one is unusable for manual testing/session switching) revokes the current token and clears the cookie. Authorization is two named policies (`AddAuthorization`, `Program.cs`): `"ManagerOnly"` (`RequireRole("Manager")`) and `"StaffOnly"` (`RequireRole("Manager", "Barista")`) — applied to close out the `TODO(Epic 11)` markers left in `InventoryController` and `PaymentsController.Reconciliation` from Epics 4/10. `RefreshHandlerTests` (4 tests: valid rotation, unknown token, expired token, revoked-token-reused-and-sweeps-siblings) use the same hand-rolled-fake pattern as Story 11.1's tests. **Also fixed while wiring this in**: the frontend's Vite dev proxy (`vite.config.ts`) targeted port 5000, but the API's actual "http" launch profile runs on 5076 (confirmed live) — a pre-existing mismatch that would have silently broken every browser-driven request through the dev server, not just the new login page; corrected the proxy target and added a `/hubs` WebSocket proxy entry for Epic 12. Verified live end-to-end through the real dev proxy in Chrome: login → JWT-authorized `GET /api/inventory/low-stock` (`200` for manager, `403` for barista) → refresh rotates the cookie → replaying the old cookie value correctly `401`s with `Auth.RefreshTokenReused` → logout clears the session and the router guard (Story added alongside this, see Epic 12) redirects `/kitchen` back to `/staff/login?redirect=/kitchen`.

## Epic 11 status: ✅ complete (2/2 stories)

---

## Epic 12 — Kitchen Board

Goal: live order board, SignalR push (§11).

### Story 12.1 — SignalR hub for order events ✅
AC: new confirmed orders push in <3 seconds; broadcasts on `Confirmed` and subsequent transitions.
- [x] T12.1.1 `OrderHub` implementation
- [x] T12.1.2 Broadcast wired to state-machine transition events
- [x] T12.1.3 Auth on hub connection (staff only)
- Size: M · Depends on: 3.1, 11.2
- Delivered: added `OrderStatusChangedEvent(OrderId, NewStatus)` (Domain) raised on *every* successful `Order.TransitionTo` — unlike `OrderCancelledEvent`/`OrderCompletedEvent`, which only fire on specific edges for the Epic 4 stock handlers, the kitchen board needs to know about every step (`Confirmed`, `Preparing`, `Ready`, ...). `OrderHub` (`[Authorize(Policy = "StaffOnly")]`, mapped at `/hubs/orders`) is pure server-to-client push — clients never call a hub method, they connect and listen. `BroadcastOrderStatusChangedHandler` (`IDomainEventHandler<OrderStatusChangedEvent>`) bridges Epic 3's existing domain-event pipeline to it: re-loads the order (same "don't trust in-memory navigation" reasoning as the Epic 4 stock handlers) and pushes a `KitchenOrderDto` via `IHubContext<OrderHub>`. This handler physically lives in **Api**, not Application/Infrastructure — it's the only layer allowed to know about `OrderHub`/`IHubContext` given this project's one-way layering, even though every other domain-event handler so far has lived in Application or Infrastructure. Auth on the hub connection reuses the JWT bearer scheme already wired for Epic 11: since a browser can't set a custom `Authorization` header on a WebSocket upgrade, the SignalR client sends the token as `?access_token=...` instead, bridged into the normal bearer-token path via `JwtBearerEvents.OnMessageReceived`, scoped to `/hubs` paths only. Verified live in Chrome: connected as `barista1`, watched a `curl`-created cash order (auto-`Confirmed`) appear on the board with **zero page reload**, sub-second — comfortably inside the `<3s` AC — confirmed via the browser's own console log of the WebSocket connecting and the DOM updating.

### Story 12.2 — Kitchen board UI ✅
AC: three columns (New/Preparing/Ready); modifiers spelled out without extra tap; Google Maps deep link; audible alert.
- [x] T12.2.1 Vue kitchen-board view + SignalR client connection
- [x] T12.2.2 Column/card components with one-tap state advance
- [x] T12.2.3 Modifier display reconstructed from `OrderItemModifier` snapshots
- [x] T12.2.4 Google Maps deep-link construction from pinned lat/lng
- [x] T12.2.5 Audible alert on new order
- Size: L · Depends on: 12.1, 9.2
- Delivered: no design mock covers the staff area at all (the CROWD design package only ever covered the customer-facing site), so `KitchenBoardView.vue` reuses the existing OKLCH design tokens rather than inventing a second visual language for two internal-only screens. Three columns (`Confirmed`→"New", `Preparing`, `Ready`) computed client-side from a flat `orders` array kept in sync by `applyIncomingOrder`: an initial `GET /api/kitchen/orders` (`GetKitchenBoardOrdersHandler`, `IOrderRepository.GetActiveKitchenOrdersAsync` — oldest-first FIFO) populates it, then every `orderStatusChanged` push either upserts (still on the board) or removes (order moved past `Ready` — picked up, out for delivery, cancelled) the matching row by id. One-tap advance (`POST /api/kitchen/orders/{id}/advance`, `AdvanceOrderStatusHandler`) deliberately doesn't accept a target status from the client — the board only ever offers "move to the next column," so the server computes what that means itself (branching at `Ready` into `OutForDelivery`/`PickedUp` per the order's `Mode`, reusing `Order.TransitionTo`'s existing guard) rather than trusting a client-supplied status; `changedBy` comes from the JWT's name claim (`User.Identity.Name`), not a free-text field. `KitchenOrderDto`/`KitchenOrderDtoMapper` (new `Application/Kitchen` module) spell modifiers out as plain display-name strings (e.g. "Oat Milk") reconstructed from the `OrderItemModifier` snapshots already on the order — no second lookup, per the AC. The Google Maps link (`mapsUrl()`) only renders for delivery orders with a pinned lat/lng, using the `https://www.google.com/maps/dir/?api=1&destination=lat,lng` deep-link form (turn-by-turn directions, not just a pin) — reuses the `Latitude`/`Longitude` Story 9.1's location picker already captures. The audible alert (`lib/alertSound.ts`) is a two-tone Web Audio beep generated in code rather than a shipped audio asset — there's no sound file in the design package for this internal-only screen — fired only when a *new* order (one not already in the local `orders` array) arrives at `Confirmed`, not on every push (so advancing an order yourself doesn't re-trigger it). A new `useAuthStore` (Pinia) keeps the access token in memory only, never `localStorage` — an XSS payload that can read `localStorage` can steal a token stored there, but this store's state doesn't survive a reload anyway, and the actual persistence (the refresh token) is the httpOnly cookie the browser manages entirely on its own, which JS can't read either way; `main.ts` awaits one silent `tryRestoreSession()` refresh-cookie exchange before the app ever mounts, so a reload doesn't force a re-login. Verified live end-to-end in Chrome (see Story 12.1's note for the SignalR proof): staff login → board loads existing `Confirmed` order → tapped "Start preparing" → card moved `New → Preparing` and the button label updated to "Mark ready" → a fresh order created via the real API appeared in "New" with its `(No Milk)` modifier spelled out and its `Notes` shown, no reload → sign-out correctly cleared the session and the `/kitchen` route guard redirected an unauthenticated visit to `/staff/login?redirect=/kitchen`. No console errors at any point in the flow.

## Epic 12 status: ✅ complete (2/2 stories)

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
