# Troubleshooting

| Symptom | Action |
| --- | --- |
| Docker cannot connect to `dockerDesktopLinuxEngine` | Start Docker Desktop and wait for the Linux engine |
| SQL host port cannot bind | Check that the host mapping uses an available port; this setup uses `14330:1433` |
| `init.sh`: `set: pipefail` invalid option | Check LF line endings and UTF-8 without BOM |
| `db-init` exits with code 0 | Normal: initialization finished or the database already exists |
| `db-init` exits with a nonzero code | Read its latest logs before modifying SQL or rerunning initialization |
| Initialization is skipped | Check whether the database already exists or SQL scripts are missing |
| `/health/db` fails | Check SQL readiness, database name and connection configuration |
| Web reports missing SPA build | Run `npm ci` and `npm run build` in the SPA directory |
| Swagger omits recent changes | Rebuild/restart the API serving the current source |
| DLL/EXE locked during build | Stop the specific process holding the files, then rebuild |
| Customer cannot sign in to WinForms | WinForms requires a staff role |
| New initialization script is not applied | Existing databases are skipped; prepare an explicit migration procedure |
