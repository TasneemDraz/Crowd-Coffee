# CROWD Coffee — Product Requirements Document

**Version:** 1.1
**Owner:** Tasneem (.NET Backend Developer)
**Target:** v1 production launch
**Purpose of this document:** Input for Claude Code to plan the build, decompose into epics → stories → tasks, and implement.

---

## 1. Product Overview

CROWD is a specialty coffee shop on New Mamsha Corniche, Port Said, Egypt — family-owned by three brothers. This project delivers a public-facing ordering website plus a staff back-office that handles the full lifecycle: browse → order → pay → prepare → deliver, with real inventory deduction behind it.

### Goals

| Goal | Success measure |
|---|---|
| Let customers order online for delivery or pickup | Order placed end-to-end without staff intervention |
| Give staff a live order board | New order visible on kitchen screen in < 3 seconds |
| Prevent selling items that can't be made | Zero oversold orders after ingredient stock hits zero |
| Accept cash, card, and mobile wallet | All three payment paths reach a confirmed order |
| Know true ingredient consumption | Every sale produces a traceable stock ledger entry |

### Non-goals for v1

Multi-branch, loyalty/points, table reservations, customer accounts with saved history, Arabic localization, native mobile apps, rider dispatch app. Architect so these are addable — do not build them.

---

## 2. Users

| User | Needs |
|---|---|
| **Customer** | Browse a 161-item menu quickly on mobile, add to cart, choose delivery or pickup, pin an exact location, pay, get an order number and ETA |
| **Barista / kitchen staff** | See incoming orders on a shared screen, move them through prep states, mark ready |
| **Shop manager** | Manage menu and prices, toggle item availability, receive and record stock, see low-stock alerts, view daily sales |

Assume the customer is on a phone, on mobile data, on a hot corniche afternoon. Mobile-first is not optional.

---

## 3. Existing Design Assets

A design package exists (`Coffee_brand_website_redesign__2_.zip`) containing static HTML implementations of every screen. **These are the visual source of truth — match them, do not redesign.**

| File | Screen |
|---|---|
| `Crowd Coffee.dc.html` | Landing page — hero, story, menu teaser, gallery, visit info |
| `Menu.html` (**newer — use this**) | Full menu, category filter, and the drink customization modal (§4a) |
| `Menu.dc.html` | Earlier menu version, no modal. Superseded — kept only for reference |
| `Cart.dc.html` | Line items, qty steppers, delivery/pickup toggle, totals |
| `Checkout.dc.html` | Customer details, location pin, payment method, confirmation state |
| `Location Picker.html` | Draggable map pin, "use my location" |
| `assets/`, `uploads/` | Photography |

### Design tokens extracted from the source

Colors are authored in **OKLCH** — preserve this, do not convert to hex.

- Accent / brand: `oklch(56% 0.16 40)` (terracotta)
- Accent surface: `oklch(94% 0.04 40)`
- Ink / body text: `oklch(18% 0.01 50)`
- Border idle: `oklch(85% 0.01 70)`

Voice and copy from the design (keep it): "A space for every crowd.", "You are in the crowd.", "The good kind of noise."

### Business rules already encoded in the design

These are decisions, not suggestions — the backend must reproduce them exactly:

- Delivery fee is a flat **30 EGP**; pickup is **free**
- Order modes: `delivery` | `pickup`
- Pickup location is fixed: CROWD, New Mamsha Corniche, Port Said
- ETA copy: delivery **30–40 min**, pickup **15–20 min**
- Payment methods: `cash` (COD/on pickup), `card`, `wallet` (Vodafone Cash / InstaPay)
- Cart persists in `localStorage` under key `crowdcoffee_cart`; order mode under `crowdcoffee_order_mode`
- Location Picker communicates by `postMessage` with `{ type: 'crowd-location', lat, lng }`
- Required at checkout: name, phone; address additionally required when mode is delivery
- Order number format in the mock is `CR-####` — **replace** with the production format in §6

---

## 4. Menu Data

Seed data lives in `Menu.html` as `CATEGORIES`, `D` (category descriptions), and `ITEMS`, plus `MILK_OPTIONS`, `SHOT_OPTION`, `SYRUP_OPTIONS`, and `DRINK_CATS` for the modifiers in §4a. Extract all of it into a seed migration — do not retype it.

The item list is **unchanged** between the two menu versions: same 161 items, same ids, same prices. Only the modal is new.

- **19 categories**, in this display order: Black Coffee, Classic Coffee, Iced Coffee, Speciality, Signatures, Non Coffee, Frappé Blended, Matcha, Shakes, Smoothies, Mojito, Sandwiches, Breakfast, Bakery, Sweet Bakeries, Pastries, Signature Desserts, Cheesecakes, Salads
- **161 items**, price range **50–195 EGP**, all integer EGP
- Item shape: `{ id, cat, name, desc, price }` — the string ids (`c-latte`, `sg-sansebastian`) become the product `Slug`

Note: `CATEGORIES` has 19 entries but `D` has 18 keys — `cheesecakes` has no description. Handle a missing description gracefully rather than crashing the seed.

---

## 4a. Product Modifiers (Options)

An updated `Menu.html` adds a **customization modal** for drinks. This is fully specified in the design and is in scope for v1. Item names, ids, and prices below are the source of truth — extract them into seed data.

### Which products get the modal

Only drinks. The design defines `DRINK_CATS` explicitly:

```
black, classic, iced, speciality, sigdrinks,
noncoffee, frappe, matcha, shakes, smoothies, mojito
```

Food categories (sandwiches, breakfast, bakery, sweetbakeries, pastries, sigdesserts, cheesecakes, salads) bypass the modal and add straight to the cart at base price — this is the existing `handleAdd` path.

### The three option groups

| Group | Rule | Required | Options |
|---|---|---|---|
| **Milk Type** | Select exactly 1 | **Yes** — Add button disabled until chosen | No Milk (+0), Full Cream (+0), Skimmed (+0), Oat (+40), Almond (+40), Coconut (+40) |
| **Espresso Shot** | Select up to 1 | No | Espresso Shot (+40) |
| **Syrups** | Select **up to 2** — enforced | No | 4 sugar-free syrups (+40 each): Salted Caramel, Caramel, Vanilla, Hazelnut · 8 regular syrups (+30 each): Vanilla, Honey, Cinnamon, Caramel, Salted Caramel, White Chocolate, Strawberry, Toffee Nut |

The modal also carries its own quantity stepper (minimum 1), separate from the cart's stepper.

### Pricing formula

```
unitPrice = basePrice + milkPrice + shotPrice + Σ(syrupPrices)
lineTotal = unitPrice × modalQty
```

Note that "No Milk" is a **valid explicit selection**, not an absence of one — a customer ordering an espresso must still actively pick it. Do not let a null milk value fall through as valid.

### Cart identity — the important part

A customized drink is **not** the same cart line as the same drink customized differently. The design composes a synthetic cart id:

```
{productId}::{milkId}::{shot|noshot}::{sortedSyrupIds joined by "-"}
```

Example: `c-latte::oat::shot::caramel-vanilla`

Syrup ids are **sorted** before joining, so that vanilla+caramel and caramel+vanilla collapse into one line rather than two. Preserve this. Matching ids increment quantity; differing ids create a new line.

Display name is composed as: `Latte (Oat Milk, + Espresso Shot, + Caramel Syrup)`.

### Backend implications

This changes the domain model from §6:

- **`ModifierGroup`** — Id, Name, SelectionType (ExactlyOne | UpToN), MaxSelections, IsRequired, DisplayOrder
- **`ModifierOption`** — Id, ModifierGroupId, Slug, Name, PriceDelta, IsActive, DisplayOrder
- **`ProductModifierGroup`** — join table mapping which groups apply to which products (or, simpler for v1, to which *categories*, since the rule is currently category-wide)
- **`OrderItemModifier`** — OrderItemId, ModifierOptionId, **ModifierName** (snapshot), **PriceDelta** (snapshot)

Rules that follow from this:

1. **Snapshot modifier names and price deltas** onto the order line, exactly as with base product price. Raising oat milk to 50 EGP must not rewrite history.
2. **Server-side validation is mandatory**: exactly one milk, at most one shot, at most two syrups, and every option must genuinely belong to a group attached to that product. A client posting three syrups or a syrup id from another product gets rejected.
3. **Server recomputes `unitPrice`** from base + deltas. The client's number is display only.
4. `OrderItem` needs the composed display name stored, or reconstructable from its snapshotted modifiers.
5. **Stock/BOM interaction:** modifiers consume ingredients too. Oat milk deducts from the oat milk `StockItem`, not the full-cream one; an extra shot deducts another ~18g of beans. Recipes must therefore attach to `ModifierOption` as well as to `Product`. This also means a modifier can be independently unavailable — when oat milk runs out, that single option greys out while the rest of the drink stays orderable.
6. The **cart-id composition scheme should be mirrored server-side** so that re-pricing a submitted cart can match lines deterministically.

### Design note

The updated menu also introduces a category-chip accent of `oklch(32% 0.05 155)` (deep green) for the active state, alongside the terracotta `oklch(56% 0.16 40)` used for selection controls inside the modal. Both are in play — do not collapse them into one accent.

---

## 5. Technical Direction

Backend is fixed. Frontend is Claude Code's call.

**Backend (fixed):**
- ASP.NET Core 8 Web API, C#
- EF Core + SQL Server
- SignalR for live order push
- Clean layering: Domain / Application / Infrastructure / API. Domain has no EF or ASP.NET dependencies.
- Serilog structured logging; FluentValidation; Result-type error handling over exceptions for business rule failures

**Frontend (Claude Code decides):** The design is plain HTML with `{{ }}` interpolation and a small component class per page, which maps naturally onto a Vue 3 SPA — but evaluate and pick. Whatever is chosen must produce a fast mobile experience over a 161-item menu and reproduce the design exactly. State the choice and the reasoning in the plan before writing code.

**Constraint:** the public site must remain usable on a slow 3G connection. Menu images need lazy loading and modern formats.

---

## 6. Domain Model

### Catalog
`Category` (Id, Slug, Label, Description, DisplayOrder, IsActive)
`Product` (Id, CategorySlug, Slug, Name, Description, Price, ImageUrl, IsAvailable, IsActive, DisplayOrder)

### Ordering
`Order` — Id, **OrderNumber**, CustomerName, Phone, Mode (Delivery|Pickup), Address, Latitude, Longitude, Subtotal, DeliveryFee, Total, PaymentMethod, PaymentStatus, Status, IdempotencyKey, CreatedAt, ConfirmedAt, CompletedAt, Notes

`OrderItem` — Id, OrderId, ProductId, **ProductName** (snapshot), **UnitPrice** (snapshot), Quantity, LineTotal

`OrderStatusHistory` — Id, OrderId, FromStatus, ToStatus, ChangedBy, ChangedAt, Reason

### Inventory
`StockItem` — Id, Name, Unit (g|ml|piece), QuantityOnHand, QuantityReserved, ReorderPoint, IsActive
`Recipe` — Id, ProductId, StockItemId, QuantityPerUnit
`StockMovement` — Id, StockItemId, Quantity (**signed**), MovementType (Purchase|Sale|Waste|Adjustment|Return), ReferenceType, ReferenceId, Notes, CreatedBy, CreatedAt

### Staff
`StaffUser` — Id, Username, PasswordHash, Role (Manager|Barista), IsActive

### Critical model rules

1. **Snapshot price and name into `OrderItem`.** A price change next month must not alter last month's orders.
2. **`OrderNumber` is separate from the PK** and human-readable: `CRW-YYMMDD-NNNN`, sequence resetting daily. Never expose the identity Id publicly.
3. **`StockMovement` is an append-only ledger.** Never `UPDATE` a quantity in place. `QuantityOnHand` is a cached projection; the ledger is truth. Provide a rebuild/reconcile command that recomputes on-hand from the ledger.
4. **Money is `decimal(18,2)`**, never `float`/`double`.
5. All timestamps UTC in the database; render in Africa/Cairo.

---

## 7. Order Lifecycle

```
PendingPayment ──(webhook confirms)──► Confirmed
   │                                      │
   └──(timeout / failure)──► Cancelled     ▼
                                       Preparing
Pending ──(cash order)──────────────►      │
                                           ▼
                                         Ready
                                        ╱      ╲
                          OutForDelivery        PickedUp
                                 │
                                 ▼
                            Delivered
```

Rules:
- Cash orders enter at `Pending` and a staff member confirms
- Card/wallet orders enter at `PendingPayment` and **never reach the kitchen board** until the gateway webhook confirms
- Cancellation allowed from any state before `Ready`; it releases stock reservations
- Every transition writes an `OrderStatusHistory` row — no exceptions
- Illegal transitions are rejected by the domain, not by UI convention

---

## 8. Stock Model

Two distinct cases, one mechanism:

**Finished goods** (cheesecakes, cookies, pastries) — countable. The product maps to a single `StockItem` with unit `piece`, quantity 1.

**Beverages** — you do not stock lattes, you stock beans, milk, syrup. A latte's recipe deducts ~18g beans + ~200ml milk. This is the part that makes inventory real, and it is the part most implementations skip.

**Reservation flow:**
- `Available = QuantityOnHand − QuantityReserved`
- On order confirmation: **reserve** ingredient quantities
- On order completion (Delivered/PickedUp): convert reservation into a `Sale` movement and decrement on-hand
- On cancellation: release the reservation, write nothing to the ledger
- Deduct at **Confirmed**, not at **Pending** — otherwise abandoned card checkouts eat your inventory

**Availability:**
- When any ingredient in a product's recipe cannot cover one unit, the product auto-flags unavailable and disappears from the public menu
- When a `StockItem` drops below `ReorderPoint`, raise a low-stock alert on the admin dashboard
- Managers can manually override `IsAvailable` on any product (the "we're out of oat milk today" switch)

---

## 9. Payments — Paymob / Fawry

In scope for v1. Both support cards, Vodafone Cash, and InstaPay through one integration; evaluate and pick one, and isolate it behind an `IPaymentGateway` interface so the other can be swapped in.

Requirements:
- **Never handle raw card data.** The design's checkout has inline card-number fields — replace them with a hosted iframe or redirect. PCI scope must stay at zero. This is a deliberate deviation from the mock.
- Webhook endpoint with **HMAC signature verification** — reject unsigned or mismatched callbacks
- Webhook handling must be **idempotent**; gateways retry
- Amount charged is computed **server-side** from the database, never accepted from the client
- Reconciliation view for the manager: gateway-confirmed payments vs. orders
- Timeout job: `PendingPayment` orders older than 30 minutes auto-cancel and release reservations

---

## 10. Notifications

- **Staff:** SignalR pushes new confirmed orders to the kitchen board, with an audible alert. Baristas will not refresh a page during a rush.
- **Customer:** order confirmation and "ready / out for delivery" via WhatsApp Business API or SMS. Abstract behind `INotificationService`; a logging no-op implementation is acceptable for v1 if the provider account isn't ready, but the call sites must exist.

---

## 11. Admin Back-Office

Authenticated (JWT, cookie-based refresh). Two roles.

**Kitchen board** — three columns (New / Preparing / Ready), live via SignalR, one tap to advance state, shows mode, items **with their modifiers spelled out** (a barista must see "oat milk, extra shot" without tapping), and a Google Maps deep link built from the pinned lat/lng.

**Order history** — filter by date, status, mode, payment method; open an order to see its full status history.

**Menu management** — CRUD products and categories, edit prices, toggle availability, reorder display sequence, upload images.

**Stock** — current on-hand vs. reserved, record a purchase/delivery, record waste, adjust with a mandatory reason, low-stock list, movement ledger per item.

**Dashboard** — today's revenue, order count, average order value, top-selling items, delivery vs. pickup split.

---

## 12. Non-Functional Requirements

**Security**
- Server-side price recalculation on every order — client-supplied prices are advisory only, never trusted
- Rate limiting on order creation and any OTP/phone endpoint
- Idempotency key on `POST /orders` — a double-tapped Place Order button must not create two orders
- Parameterized queries throughout (EF Core handles this; no raw string concatenation)
- Secrets in environment/Key Vault, never in `appsettings.json` committed to git
- CORS restricted to known origins
- Security headers: HSTS, CSP, X-Content-Type-Options

**Performance**
- Menu endpoint response < 200ms, cached
- Order creation < 500ms
- Lighthouse mobile performance ≥ 85 on the public site

**Reliability**
- Order creation is a single transaction: validate → reserve stock → persist order. Partial failure leaves no orphan reservation.
- Health check endpoint
- Structured logs with a correlation id per request

**Accessibility** — keyboard navigable, visible focus states, AA contrast, alt text on menu imagery.

---

## 13. Suggested Epic Breakdown

Claude Code should refine this, not follow it blindly — but the **order matters**: the domain model and order state machine carry all the real design risk and must be settled before UI work begins.

1. **Foundations** — solution structure, layering, DI, logging, health checks, CI
2. **Domain model & migrations** — entities, EF configuration, menu seed from the design files
3. **Catalog API** — categories, products, modifier groups and options, availability filtering, caching
4. **Public site: landing + menu** — match the design, mobile-first, lazy images
5. **Drink customization modal** — milk/shot/syrup groups, selection limits, live price, composed cart id (§4a)
6. **Cart** — client-side with localStorage, modifier-aware line identity, server-side re-pricing and stock validation
7. **Order state machine** — domain-level transitions, history, guards, unit tests
8. **Checkout & order creation** — validation, idempotency, transactional creation, location pin
9. **Payments** — gateway abstraction, hosted checkout, webhook + HMAC, timeout job
10. **Inventory** — stock items, recipes (including per-modifier recipes), reservations, ledger, auto-availability, low-stock alerts
11. **Staff auth** — JWT, roles, refresh
12. **Kitchen board** — SignalR hub, live board, state transitions
13. **Admin: menu, modifier & stock management**
14. **Admin: dashboard & reporting**
15. **Notifications** — SignalR staff alerts, customer WhatsApp/SMS
16. **Hardening** — rate limiting, headers, load check, accessibility pass
17. **Deployment** — Docker, environment config, migration strategy, backups

### Definition of done, per story

Acceptance criteria met · unit tests on domain logic · integration test on the endpoint · input validated · errors handled and logged · matches the design where UI is involved · works on a 375px viewport · no secret committed.

---

## 14. Open Questions

Claude Code should surface these before implementing the affected epic rather than guessing:

1. Delivery zones — is 30 EGP flat everywhere, or should distance from the shop cap the delivery radius?
2. Opening hours — should ordering be blocked outside them, and what happens to an order placed at closing time?
3. ~~Item options~~ — **Resolved.** See §4a; modifiers are in scope for v1. Remaining sub-question: are the 40 EGP alt-milk and 30–40 EGP syrup surcharges uniform across every drink, or do some categories price them differently?
4. Minimum order value for delivery?
5. Does the customer need order tracking after checkout, or is the confirmation screen the end of their journey in v1?
6. Who receives low-stock alerts, and through what channel?
7. Is there an existing POS at the shop that in-store sales flow through? If so, stock is being consumed by a system this project can't see, and the ledger will drift.

---

## 15. Instructions for Claude Code

1. Read the design files in the zip before planning — they contain the real menu data, the fee rules, and the visual system.
2. Produce the epic → story → task breakdown **before** writing code, and pause for review.
3. Each story: user story format, explicit acceptance criteria, dependencies, rough size.
4. Flag every open question from §14 that touches the story you're about to implement.
5. Build the domain model and order state machine first. Everything else is CRUD; those two are where the real decisions live.
6. Where the design conflicts with security (inline card fields), follow the security requirement and note the deviation.
