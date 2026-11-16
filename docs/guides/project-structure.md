# Project structure

| Directory | Purpose |
| --- | --- |
| `src/Backend/DongNamTrip.Api` | API host, configuration, health and OpenAPI |
| `src/Backend/DongNamTrip.Modules` | Six business modules and shared persistence |
| `src/Frontend/DongNamTrip.Spa` | React SPA organized by feature |
| `src/Frontend/DongNamTrip.Web` | BFF, SPA hosting and legacy MVC |
| `src/Frontend/DongNamTrip.Admin` | WinForms staff application |
| `src/Frontend/DongNamTrip.Client` | Shared .NET HTTP client |
| `src/Shared/DongNamTrip.Contracts` | Shared DTOs and vocabulary |
| `tests/architecture` | Static module/layer checks |
| `tests/api` | HTTP/SQL integration tests |
| `tests/e2e` | Browser tests |
| `tests/spa` | SPA, fault and HMR tests |
| `tests/ci` | CI helpers |
| `db` | Initialization script and SQL files |
| `docs` | Architecture decisions and design documents |

Generated directories such as `bin`, `obj`, `node_modules` and the SPA build output must not be committed.
