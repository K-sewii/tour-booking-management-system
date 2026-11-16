# Contributing

Thank you for your interest in DongNamTrip. By participating you agree to the [Code of Conduct](CODE_OF_CONDUCT.md).

## Getting started

1. Fork the repository and create a `feature/<short-name>` branch from the current base (`main`).
2. Set up the project with the [Docker guide](docs/guides/running-with-docker.md) or the [Windows guide](docs/guides/running-on-windows.md).
3. Read [ADR-001](docs/architecture/ADR-001-modular-monolith.md) and respect module boundaries: `Presentation → BusinessLogic → DataAccess`, with cross-module calls only through public `I*Api` ports.

## Making changes

- Keep API contracts compatible with both the Web BFF and WinForms.
- Do not generate schema changes from EF entities or rerun the full SQL script against an existing database; add reviewed, idempotent scripts under `db/init/`.
- Never commit `.env`, credentials, session tokens, or generated output (`bin`, `obj`, `node_modules`, SPA build output).
- Update documentation when behavior changes.

## Before opening a pull request

Run the checks relevant to your change (see [Testing and CI](docs/guides/testing-and-ci.md)):

```powershell
dotnet build .\DongNamTrip.sln
python tests/architecture/rules.py
# frontend, from src/Frontend/DongNamTrip.Spa
npm run lint; npm test; npm run build
```

Stage explicit files, write a clear commit message, open a pull request describing what and why, and check that CI passes before requesting review.

## Reporting bugs and ideas

Open a GitHub issue with steps to reproduce, expected and actual behavior, and your environment. For security problems, follow [SECURITY.md](SECURITY.md) instead.
