# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

DongNamTrip: a university tour-discovery/booking project for Southeast Vietnam. React SPA + WinForms admin, both talking to one ASP.NET Core (.NET 10) API on SQL Server. Docs/ADRs are mostly in Vietnamese. Only the `Development` environment is supported (API refuses to start otherwise).

## Commands

```powershell
dotnet restore .\DongNamTrip.sln
dotnet build .\DongNamTrip.sln --no-restore

# SPA (run these in a separate terminal at src\Frontend\DongNamTrip.Spa; output is not committed)
cd .\src\Frontend\DongNamTrip.Spa
npm ci
npm run lint       # ESLint, zero warnings
npm test           # Vitest
npm run build      # tsc -b && vite build; Web needs the output
npm run dev        # Vite HMR at http://127.0.0.1:5173; needs API + Web running

# Run from the solution root; use a terminal per process (API first)
dotnet run --project .\src\Backend\DongNamTrip.Api --launch-profile http    # :5080
dotnet run --project .\src\Frontend\DongNamTrip.Web --launch-profile http   # :5081
dotnet run --project .\src\Frontend\DongNamTrip.Admin                       # WinForms, Windows only

# Docker (needs .env from .env.example and schema .sql files in db/init/)
docker compose up --build  # from repository root; local Docker is reported running by the user
```

API connection string: `dotnet user-secrets set "ConnectionStrings:DongNamTrip" "..." --project .\src\Backend\DongNamTrip.Api`.

### Tests

```powershell
python tests/architecture/rules.py          # static module/layer rules; no DB or build needed
python .\tests\api\smoke.py --start         # --start launches the API itself
python .\tests\api\auth_screens.py --start
python .\tests\api\content_screens.py --start
python .\tests\e2e\browser_runner.py        # Playwright + Chromium
python .\tests\spa\spa_runner.py
python .\tests\spa\vite_hmr_runner.py
```

API/e2e/SPA tests hit a real SQL Server **development** DB and create/clean temp accounts; never point them at real data. Each script is its own runner — run a single one directly (there is no per-test filter).

## Architecture

Flow: `SPA (React) → Web (ASP.NET BFF) → API → modules → SQL Server`; `WinForms Admin → API`. Decision record: `docs/architecture/ADR-001-modular-monolith.md`.

- **`src/Backend/DongNamTrip.Api`** is a thin host (`Program.cs`: config, rate limit, health, OpenAPI registration, registers/maps modules). **`src/Backend/DongNamTrip.Modules`** has six modules, each implementing `IModule`. Catalog (SQL catalog), Itinerary (SQL personal itineraries), Administration (SQL accounts/account area plus in-memory sessions) and Booking (read-only order/transaction history plus saved payment preference) have endpoints/services/repositories. Quotation and Narration currently only register empty modules; no endpoint is exposed for either.
- Inside a module: `Presentation` (`*Endpoints`) → `BusinessLogic` → `DataAccess`. Lower layers must not reference `*Endpoints`; Presentation must not touch DbContext/EF or DataAccess types; routes use `MapGroup`; contract strings come from `Contracts/Vocabulary.cs`. **Full backend rules, endpoint conventions and checklists: [`src/Backend/CLAUDE.md`](src/Backend/CLAUDE.md)** — read it before changing backend code. Frontend (SPA, Web BFF, Admin) rules: [`src/Frontend/CLAUDE.md`](src/Frontend/CLAUDE.md).
- Cross-module use is allowed **only through public ports named `I*Api`** (e.g. `ICatalogApi`). `tests/architecture/rules.py` enforces this by scanning source text — it also fails if the SPA contains hard-coded region names or does `tours/destinations.data.filter/sort/slice/...`. Search, paging, totals, estimates and validation limits (`GET /api/meta`) must come from the backend.
- `Persistence/` (shared `DongNamTripDbContext` + entities) is still a single shared context (known debt per ADR).
- **Database schema lives in `db/init/*.sql`, supplied by the team.** Never generate/alter schema from EF entities; catalog data comes from the same team script (rows need `TrangThai = 'Published'`).
- **`DongNamTrip.Web`** is a BFF: serves the built SPA, keeps login in an HttpOnly cookie, antiforgery on writes, forwards to the API. The SPA must never store API tokens in local/sessionStorage. Legacy MVC views remain via `?legacy=1` or `Spa__Enabled=false`.
- **`DongNamTrip.Client`** = typed HTTP client for the API (used by Web and Admin); **`DongNamTrip.Contracts`** = shared DTOs. Keep `/api/...` URLs and contracts stable.
- Auth: public registration always creates `Customer`; staff roles `Admin, Operator, Editor, Finance, Publisher` (WinForms is staff only). Optional `BootstrapAdmin__*` and `DevelopmentTestAccounts__Enabled` settings seed accounts in dev.
- API docs exist only in Development: `http://localhost:5080/openapi/v1.json` and `http://localhost:5080/swagger`. They use an opaque Bearer session token from login/register, **not JWT**. Swagger UI does not persist authorization; never put real passwords or tokens in examples.

## CI

`build.yml` (SPA lint/test/build + `npm audit`, solution build, architecture rules, API/Web/browser tests on an isolated Windows LocalDB prepared from `db/init/*.sql` plus the CI catalog fixture) and `docker.yml` (compose validation, image build, Trivy, smoke test, GHCR push on `main`). CI does not obtain the schema from a secret. Branch from `main` as `feature/<name>`; keep changes within one module where possible.
