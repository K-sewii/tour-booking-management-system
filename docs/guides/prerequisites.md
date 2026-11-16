# Prerequisites

For Docker:

- Docker Desktop with the Linux engine running, or a compatible Docker Engine.
- Docker Compose.
- Repository SQL scripts and a configured `.env`.

For running applications directly on Windows:

- .NET SDK matching `global.json`.
- Node.js/npm compatible with the SPA package configuration; the Docker frontend build uses Node 22.
- SQL Server and an initialized development database.
- Python for integration tests.
- Playwright and Chromium for browser tests.

WinForms runs directly on Windows, including when API/Web/SQL run in Docker.
