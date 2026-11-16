# Testing and CI

Frontend checks:

```powershell
Push-Location .\src\Frontend\DongNamTrip.Spa
try {
    npm run lint
    npm test
    npm run build
}
finally {
    Pop-Location
}
```

Architecture and integration runners, from the repository root:

```powershell
python tests/architecture/rules.py
python .\tests\api\smoke.py --start
python .\tests\api\auth_screens.py --start
python .\tests\api\content_screens.py --start
python .\tests\e2e\browser_runner.py
python .\tests\spa\spa_runner.py
python .\tests\spa\vite_hmr_runner.py
```

- Inspect each runner's prerequisites and arguments.
- Integration tests use real SQL Server and may create temporary accounts and records.
- Use an isolated development/test database.
- Run suites sequentially unless database and port isolation is explicitly configured.
- Check for occupied ports before using runners that start API/Web processes.
- Browser tests require Playwright and Chromium.
- Clean up only data created by the test.
- A successful build or health check is not a full regression test.
- WinForms build validation does not establish that its GUI has been tested.


## CI/CD

| Workflow | Purpose |
| --- | --- |
| `build.yml` | Frontend checks/build/audit, .NET build, architecture rules and integration/browser tests |
| `docker.yml` | Compose validation, image builds, security scanning, smoke tests and configured GHCR publishing |
| Dependabot | Dependency update proposals |

CI database tests use repository SQL scripts and a CI catalog fixture to prepare an isolated Windows LocalDB database. The schema is not supplied through a repository secret.

See `.github/workflows/` for exact triggers, publishing conditions and current checks. A badge or previous run does not establish that a new commit passed.
