# Running with Docker

Run from the repository root in PowerShell:

```powershell
git clone https://github.com/K-sewii/tour-booking-management-system.git
Set-Location .\tour-booking-management-system

if (-not (Test-Path .env)) {
    Copy-Item .env.example .env
}
```

Edit `.env` and configure `MSSQL_SA_PASSWORD` before starting:

```powershell
docker compose up -d --build
docker compose ps -a
```

| Service | Host address | Internal Docker address |
| --- | --- | --- |
| Web | http://localhost:5081 | web:8080 |
| API | http://localhost:5080 | api:8080 |
| SQL Server | localhost,14330 | db:1433 |

The SQL mapping in `docker-compose.yml` must match:

```yaml
ports:
  - "127.0.0.1:14330:1433"
```

The API connection string inside Docker uses `Server=db`; keep the internal SQL port at 1433.

## Startup behavior

- `db` stores SQL data in the persistent `sqldata` volume.
- `db-init` runs initialization scripts when the database does not exist.
- If the database already exists, initialization is skipped.
- `db-init` ending with `Exited (0)` is normal.
- API starts after successful initialization.
- Web starts after API becomes healthy.

Keep `db/init.sh` in UTF-8 without BOM and with LF line endings. Add this rule to `.gitattributes`:

```gitattributes
db/init.sh text eol=lf
```

Check SQL connectivity:

```powershell
Invoke-RestMethod http://localhost:5080/health/db
```

View recent logs:

```powershell
docker compose logs --no-color --tail=100 api web db-init
```

After source changes:

```powershell
docker compose up -d --build api web
```

After runtime configuration changes only:

```powershell
docker compose up -d api web
```

Do not use `docker compose down -v` as a routine troubleshooting step: it removes the Compose volumes, including database storage.

Local Docker startup has been verified. This is not a claim that every regression test or production deployment has passed.
