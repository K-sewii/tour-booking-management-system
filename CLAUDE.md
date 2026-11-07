# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository.

DongNamTrip is a university tour-discovery/booking project for Southeast Vietnam. A React SPA and a WinForms admin application use one ASP.NET Core (.NET 10) API backed by SQL Server. Most architecture documents are in Vietnamese.

Only the `Development` environment is currently supported; the API refuses to start in other environments.

## Commands

Run .NET commands from the repository root:

```powershell
dotnet restore .\DongNamTrip.sln
dotnet build .\DongNamTrip.sln --no-restore
```

SPA commands, in a separate terminal:

```powershell
Set-Location .\src\Frontend\DongNamTrip.Spa
npm ci
npm run lint
npm test
npm run build
npm run dev
```

- `npm run lint`: ESLint with zero warnings.
- `npm test`: Vitest.
- `npm run build`: TypeScript and Vite; outputs to `src/Frontend/DongNamTrip.Web/wwwroot/spa`.
- Build output is generated and must not be committed.
- `npm run dev`: Vite at `http://127.0.0.1:5173`; requires API and Web running.

Run each application in its own terminal, from the repository root:

```powershell
dotnet run --project .\src\Backend\DongNamTrip.Api --launch-profile http
dotnet run --project .\src\Frontend\DongNamTrip.Web --launch-profile http
dotnet run --project .\src\Frontend\DongNamTrip.Admin
```

- API: `http://localhost:5080`
- Web: `http://localhost:5081`
- WinForms Admin: Windows only.
- Build the SPA before running Web with SPA enabled.

For a host-run API, configure its connection string through User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DongNamTrip" "..." --project .\src\Backend\DongNamTrip.Api
```

Do not print, commit or copy real secrets into documentation.

## Docker local development

Run from the repository root. Prepare `.env` from `.env.example` if it does not already exist, and configure the required local credentials. Do not overwrite an existing `.env`.

```powershell
docker compose up -d --build
docker compose ps -a
```

After changing only runtime configuration, a rebuild is generally unnecessary:

```powershell
docker compose up -d api web
```

Local port mapping:

| Service | Host address | Container address |
| --- | --- | --- |
| Web | http://localhost:5081 | web:8080 |
| API | http://localhost:5080 | api:8080 |
| SQL Server | localhost,14330 | db:1433 |

Keep the SQL host-port mapping in `docker-compose.yml` consistent with this table. The API inside Docker uses `Server=db`; it does not connect through the Windows host port.

- Docker uses environment configuration from Compose and `.env`; host User Secrets are not automatically available inside containers.
- `sqldata` persists SQL Server data.
- `db-init` initializes the database when it does not exist and then exits.
- `db-init` with `Exited (0)` is expected.
- If the database already exists, the current initialization script skips initialization; restarting the stack does not apply new SQL scripts automatically.
- Preserve existing databases and volumes. Do not use `docker compose down -v` or recreate the database as a routine troubleshooting step.
- Keep `db/init.sh` in UTF-8 without BOM and with LF line endings. `.gitattributes` should contain:

```gitattributes
db/init.sh text eol=lf
```

Docker has been run locally. This does not establish that all automated tests or a production deployment have passed.

Do not start host API/Web processes on ports already occupied by Docker.

## Tests

Static architecture checks:

```powershell
python tests/architecture/rules.py
```

Integration and browser runners:

```powershell
python .\tests\api\smoke.py --start
python .\tests\api\auth_screens.py --start
python .\tests\api\content_screens.py --start
python .\tests\e2e\browser_runner.py
python .\tests\spa\spa_runner.py
python .\tests\spa\vite_hmr_runner.py
```

- Python scripts are independent runners; inspect their arguments and prerequisites before running them.
- `--start` runners launch application processes. Check required ports first.
- Run integration suites sequentially unless their databases, ports and fixtures are explicitly isolated.
- Use an isolated development/test SQL Server database, not a shared database containing data that must be preserved.
- Tests create temporary accounts and data; clean up only data created by that test run.
- Browser tests require Playwright and Chromium.
- Do not remove assertions merely to make a test pass.
- Report exactly which builds and tests ran. A successful Docker build or health check is not a full regression-test result.
- Building WinForms does not verify its interactive GUI.

## Architecture

Application flow:

- React SPA → ASP.NET Core Web BFF → API → modules → SQL Server.
- WinForms Admin → API → modules → SQL Server.

Architecture decision:
`docs/architecture/ADR-001-modular-monolith.md`.

### Projects

- `src/Backend/DongNamTrip.Api`: thin host for configuration, authentication/authorization middleware, rate limiting, health checks, OpenAPI and module registration.
- `src/Backend/DongNamTrip.Modules`: one project containing six business modules, each implementing `IModule` through `Register` and `MapEndpoints`.
- `src/Frontend/DongNamTrip.Spa`: React SPA organized by feature.
- `src/Frontend/DongNamTrip.Web`: BFF and SPA host; legacy MVC remains available.
- `src/Frontend/DongNamTrip.Admin`: Windows-only staff application.
- `src/Frontend/DongNamTrip.Client`: typed HTTP API client shared by Web and Admin.
- `src/Shared/DongNamTrip.Contracts`: shared DTOs and vocabulary.

### Current module scope

| Module | Implemented scope | Remaining limitations |
| --- | --- | --- |
| Catalog | SQL catalog, destination/tour search and details, reference estimates, destination administration, activities and activity slots | Shared persistence remains; an estimate does not create an order |
| Itinerary | SQL personal itineraries and previews | Do not describe this as a complete private-tour quotation or booking workflow |
| Booking | Account-owned order/transaction history and saved payment preference | No complete order creation, payment collection, refund or payment-gateway workflow |
| Quotation | Registered module | No endpoints/services/repositories implemented yet |
| Narration | Narration editing, versions, translations, audio upload, publication status, authenticated listening and listen-right management | Do not infer a completed payment-to-listening entitlement workflow from the presence of listen-right endpoints |
| Administration | SQL accounts, roles, account area, session management and audit support | API sessions remain in memory; direct DbContext access in SqlAuthentication is known debt |

Check the source before updating these statements. Distinguish implemented behavior from intended architecture.

### Module boundaries

Inside a module:

`Presentation → BusinessLogic → DataAccess`

- Lower layers must not reference endpoint classes.
- Presentation must not directly access DbContext, EF or DataAccess types.
- Map routes through `MapGroup`.
- Reuse contract vocabulary from `DongNamTrip.Contracts/Vocabulary.cs`.
- Cross-module calls are allowed only through public ports named `I*Api`, such as `ICatalogApi`, `INarrationApi` and `IAdministrationApi`.
- `Persistence/` still contains a shared `DongNamTripDbContext` and shared entities. This is documented architectural debt, not permission to bypass module boundaries.
- `tests/architecture/rules.py` checks selected rules by scanning source text; passing it does not prove complete architectural correctness.
- Backend search, paging, totals, estimates and validation limits must remain authoritative.
- The SPA must obtain region/options/limits from backend metadata such as `GET /api/meta`, rather than hard-coding them or independently filtering server-paged catalog results.

Read these before changing the relevant code:

- Backend: `src/Backend/CLAUDE.md`
- Frontend, BFF and Admin: `src/Frontend/CLAUDE.md`

## Database

The team-maintained schema and initialization scripts live in `db/init/`.

The base initialization includes:

1. `DongNamTrip_Full.sql`
2. `V001_account_area.sql`
3. `V002_payment_choice.sql`

- Never generate or alter the database schema automatically from EF entities.
- Do not rerun the full initialization script against an existing database.
- If a schema change is required, prepare an explicit, reviewable SQL migration and a safe application procedure.
- Do not assume `db-init` applies migrations to an existing database.
- Preserve existing accounts, role assignments and application data.
- Public catalog visibility depends on publication status; rows intended for public display must satisfy the backend's `Published` rules.
- Do not fabricate bookings, transactions, refunds or payment credentials.

## Authentication and BFF

- Public registration creates only the `Customer` role.
- Staff roles are `Admin`, `Operator`, `Editor`, `Finance` and `Publisher`.
- WinForms is for staff; the API remains responsible for authorization.
- Optional development account setup uses `BootstrapAdmin__*` and `DevelopmentTestAccounts__Enabled`.
- API credentials are opaque session tokens, not JWTs.
- Sessions are currently stored in API memory and are lost when the API restarts.
- The Web BFF maintains the authenticated browser session through an HttpOnly cookie and protects mutations with antiforgery.
- Never store API tokens in SPA `localStorage` or `sessionStorage`.
- Keep authorization and data-ownership checks in the API.
- Keep `/api/...` contracts compatible with both Web and WinForms.
- Legacy MVC can be accessed through `?legacy=1` or `Spa__Enabled=false`.

## OpenAPI and Swagger

Development URLs:

- JSON: `http://localhost:5080/openapi/v1.json`
- Swagger UI: `http://localhost:5080/swagger`

Implementation:

- `Microsoft.AspNetCore.OpenApi` generates the document.
- Swagger UI renders that document; do not add a second generator.
- Documentation routes are mapped only in Development.
- Obtain a token through login or registration, then paste only the token into Swagger's Authorize dialog.
- Swagger sends `Authorization: Bearer <token>`.
- Do not enable persistent authorization or put real passwords/tokens in examples.
- Public endpoints must not be documented as requiring authentication.
- Derive authentication and role documentation from actual endpoint/group authorization, including policy-based requirements.
- Use module tags consistently for newly added endpoints.
- Response schemas and HTTP status metadata must match actual handlers and service results; do not guess `.Produces<T>()` types.
- Swagger metadata documents behavior; it does not enforce authorization or implement business logic.

## CI

- `build.yml`: SPA lint/test/build and dependency audit, solution build, architecture checks, and API/Web/browser tests.
- Database tests use an isolated Windows LocalDB prepared from repository SQL scripts and a CI catalog fixture.
- The schema is not supplied through a repository secret.
- `docker.yml`: Compose validation, image builds, security scanning, smoke tests and GHCR publishing under its configured triggers.
- Read the actual workflow before describing its triggers or claiming a check passed.

## Git and change handling

- Branch from `main` using `feature/<name>`.
- Read `git status` and existing diffs before editing.
- Preserve unrelated working-tree changes, local configuration and existing stashes.
- Keep changes within one module where practical.
- Stage explicit files; exclude `.env`, secrets, generated SPA output, `bin`, `obj` and `node_modules`.
- After merging shared changes, reconcile documentation with the merged source.
- Separate completed checks from checks that remain unverified.