# Configuration

| Setting | Application | Purpose |
| --- | --- | --- |
| `ConnectionStrings:DongNamTrip` | API | SQL connection; User Secrets for host execution or `ConnectionStrings__DongNamTrip` in environment configuration |
| `ApiBaseUrl` | Web | API address; Docker uses `http://api:8080/` |
| `Spa:Enabled` | Web | Set to `false` for legacy MVC |
| `BootstrapAdmin:Enabled/Name/Email/Password` | API | Optional development admin bootstrap |
| `DevelopmentTestAccounts:Enabled` | API | Optional development account seeding |
| `MSSQL_SA_PASSWORD` | Docker `.env` | SQL Server administrator password |
| `DONGNAMTRIP_API_URL` | WinForms | Override the API address |

Host User Secrets are not automatically available inside Docker containers.

Never commit `.env`, actual connection credentials or session tokens.

## Development accounts

The base SQL script seeds these accounts:

| Email | Role |
| --- | --- |
| admin@dongnamtrip.test | Admin |
| operator@dongnamtrip.test | Operator |
| editor@dongnamtrip.test | Editor |
| finance@dongnamtrip.test | Finance |
| publisher@dongnamtrip.test | Publisher |
| customer1@dongnamtrip.test | Customer |
| customer2@dongnamtrip.test | Customer |

Their initial development-only password in the supplied seed script is `DntTest@2026!`. Existing databases may have different passwords if they were changed.

Public registration creates only `Customer`. WinForms accepts staff roles only.
