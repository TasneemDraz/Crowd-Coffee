# CROWD Coffee — Deployment

Epic 17 (Story 17.1/17.2). Covers container-based deployment (Docker Compose, this repo's own
`docker-compose.yml`) and the two things Story 17.2 specifically asks for: the EF Core migration
process for production, and a DB backup schedule. Nothing here describes infrastructure that
already exists — there's no cloud environment provisioned for this project yet (same honest-gap
pattern as Epic 1.5's CI workflow, which is written and correct but hasn't run against a real
remote yet either). This is the plan to follow when that infrastructure gets stood up.

---

## 1. Container layout

`docker-compose.yml` (repo root) defines four services, in dependency order:

1. **`db`** — SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`), data on the named
   volume `crowdcoffee-db-data`.
2. **`migrate`** — one-shot: applies pending EF Core migrations, then exits. `api` won't start
   until this container reports `service_completed_successfully`. See §2 for why this is a
   separate step instead of `Database.Migrate()` in `Program.cs`.
3. **`api`** — the ASP.NET Core backend (`server/Dockerfile`), listens on 8080 inside the
   container, mapped to `5076` on the host by default.
4. **`frontend`** — the built Vue SPA served by nginx (`frontend/Dockerfile` +
   `frontend/nginx.conf`), mapped to `8080` on the host by default. nginx reverse-proxies
   `/api`, `/hubs`, and `/uploads` to the `api` service — the same three prefixes
   `vite.config.ts`'s dev-server proxy forwards — so the SPA and API present as one origin, same
   as `npm run dev`. This sidesteps needing CORS for this specific topology; the CORS
   configuration added in Epic 16.1 (`Cors:AllowedOrigins`) still exists and matters for any
   deployment that instead puts the API and SPA on genuinely different origins (e.g. a CDN in
   front of the SPA with the API on its own subdomain).

Copy `.env.example` to `.env` and fill in real values before running `docker compose up --build`
— every secret-shaped setting (SQL `sa` password, JWT signing key, Paymob credentials, the
frontend's real origin) is read from the environment, not committed. `.env` is already covered
by the root `.gitignore`.

**Known limitation of this doc's verification**: this sandbox doesn't have a Docker daemon
available, so `docker build`/`docker compose up` were never actually run end-to-end here. What
*was* verified directly: the exact `dotnet publish` command the API's Dockerfile runs succeeds
standalone (produces `CrowdCoffee.Api.dll` + dependencies), and `npm run build` (the frontend
Dockerfile's build stage) succeeds and produces `dist/`. Run a real `docker compose up --build`
against this compose file before trusting it for an actual deploy.

---

## 2. EF Core migration process (T17.2.1)

### How migrations are created (dev machine, unchanged from earlier epics)

```
cd server
dotnet ef migrations add <DescriptiveName> \
  --project src/CrowdCoffee.Infrastructure \
  --startup-project src/CrowdCoffee.Api
```

The `DbContext` (`CrowdCoffeeDbContext`) lives in Infrastructure; Api is the startup project
because it's the one with a working DI container EF's tooling can resolve `DatabaseOptions`
from. Four migrations exist as of Epic 17: `InitialCreate`, `AddOrderNumberCounters`,
`AddPaymentEvents`, `AddStaffAuth`.

**Before committing a new migration**, check it against pitfalls this project has already hit
once each:
- **`HasData` seed churn** (Epic 14's fix): don't call anything nondeterministic
  (`BCrypt.HashPassword`, `Guid.NewGuid()`, `DateTime.Now`) live inside a property whose result
  feeds `HasData(...)` — EF diffs the *value*, not the code, so a different-but-equally-valid
  result on every `migrations add` run scaffolds a spurious `UpdateData` migration forever.
  Compute once, hardcode the literal (see `StaffSeedData.cs`, `MenuSeedData.cs`).
- **Append-only tables** (`StockMovement`, `OrderStatusHistory`) have no update path in code by
  design (Epic 2.3/2.2) — a migration that adds an `UPDATE`-capable column or index to either
  should get a second look; it's probably fighting the append-only invariant, not extending it.
- **`OrderNumberCounters`** is a hot row under concurrent order creation (Epic 9.2 found a real
  SQL Server deadlock here in tests) — any migration touching this table's indexes/locking
  should be tested under concurrent load, not just for correctness.

### How migrations are applied per environment

| Environment | Mechanism |
|---|---|
| Local dev | `dotnet ef database update` (same flags as above), run manually |
| Docker Compose | The `migrate` service — automatic, gated in front of `api` |
| Production (no Docker, or Docker with more than one `api` replica) | `dotnet ef migrations bundle` (see below), run as its own controlled release step |

**Why not `Database.Migrate()` on startup?** It's not in `Program.cs`, deliberately. With more
than one `api` instance (the realistic production shape — horizontal scaling, or just a rolling
deploy where the old and new revision briefly coexist), every instance calling
`Database.Migrate()` on boot means multiple processes race to apply the same migration
concurrently. EF Core does take a lock for this, but the failure modes when that lock contends
under a real deploy (timeouts, one instance succeeding while another's health check fails
mid-migration) are worse than just... not doing that. Migrations run once, as a separate,
observable step, before the new app revision's containers start receiving traffic. This is the
same "explicit separate step over implicit automatic behavior" call this project already made in
Epic 4.5/14.2 (low-stock alerting is a queryable list, not a background push) and Epic 10.5
(payment timeout is a poll, not a per-order timer) — simpler, and the failure mode is "nothing
happened yet," not "multiple things happened badly at once."

**Production without Docker**: use an EF Core migrations bundle instead of `Dockerfile.migrate`'s
SDK-image approach — it's a single self-contained executable, doesn't need the .NET SDK on the
target machine, and is Microsoft's documented recommendation for exactly this "run once, as part
of a release pipeline" use case:

```
dotnet ef migrations bundle \
  --project src/CrowdCoffee.Infrastructure \
  --startup-project src/CrowdCoffee.Api \
  --output efbundle --self-contained -r linux-x64

# On the target, against the real connection string:
./efbundle --connection "Server=...;Database=CrowdCoffee;..."
```

### Rollback

```
dotnet ef database update <PreviousMigrationName> --project ... --startup-project ...
```

EF generates a `Down()` for every migration, but **test the rollback against a staging copy of
production data first** — a `Down()` that drops a column silently discards whatever data lived in
it; EF can't know whether that's safe for your actual data, only that it's syntactically the
inverse of `Up()`. For this project specifically, `AddOrderNumberCounters` and `AddPaymentEvents`
both add tables with rows other tables' business logic depends on existing (order-number
generation, payment idempotency) — rolling either back on a database with live orders in flight
needs a maintenance window, not a live rollback.

---

## 3. Database backup schedule (T17.2.2)

Nothing here is provisioned yet — this is the target policy for whatever managed SQL Server
instance (Azure SQL, RDS for SQL Server, or a self-managed instance) ends up hosting production.

- **Full backup**: daily, off-peak (Cairo time, per `CairoClock`'s existing day-boundary
  convention — matches when order volume is lowest).
- **Transaction log backups**: every 15 minutes, requires the database in `FULL` recovery model
  (not `SIMPLE`) — this is what makes point-in-time restore possible at all. Losing a log backup
  interval on a live commerce system means losing up to 15 minutes of real orders, payments, and
  stock movements, which is the real cost that sizes this interval — not disk space.
- **Retention**: 30 days of daily fulls, 7 days of log backups, in line with typical
  small-business compliance/dispute-window needs (a customer or card-network chargeback inquiry
  weeks after an order is the realistic case this protects against).
- **Off-instance storage**: backups land somewhere other than the DB host/volume itself — for the
  Docker Compose topology in this repo, `crowdcoffee-db-data` (the named volume `db` writes to)
  is **not a backup** by itself. It survives `docker compose down`, but not host disk loss, not
  `docker compose down -v`, and not a bad migration that corrupts data in place. Point backups at
  object storage (S3/Azure Blob/equivalent) or a managed provider's built-in automated-backup
  feature if using one — don't rely on the volume.
- **Restore drills**: actually restore a backup to a scratch instance on a recurring basis (e.g.
  monthly) and confirm the app boots against it. An untested backup is a hypothesis, not a backup
  — this project's own precedent for "verify, don't assume" runs through nearly every epic's
  delivery notes (e.g. Epic 9.2's insufficient-stock rollback test, Epic 10.3's hand-signed
  webhook payload) and applies just as much here.

---

## 4. Environment configuration reference

Every value below is read from the environment at container/process startup — see
`.env.example` for the Docker Compose mapping. Outside Docker, the same settings bind via
ASP.NET Core's standard `Section__Key` double-underscore convention (e.g.
`Jwt__SigningKey`, `Cors__AllowedOrigins__0`).

| Setting | Committed default | Must override in production |
|---|---|---|
| `Database:ConnectionString` | Windows integrated auth, local SQL Server (no secret — Epic 1.2) | Yes — real server/credentials |
| `Jwt:SigningKey` | Local-dev-only placeholder (Epic 11.1) | Yes — rotate before go-live |
| `Cors:AllowedOrigins` | `http://localhost:5173` (Vite dev) | Yes — the real deployed frontend origin(s) |
| `Paymob:*` | All empty — no live credentials exist for this project (Epic 10) | Yes, before accepting real card/wallet payments |

See `docs/EPIC_BREAKDOWN.md`'s Epic 16.2 note for the full secrets audit this project went
through before writing this doc — nothing beyond the above was found committed.
