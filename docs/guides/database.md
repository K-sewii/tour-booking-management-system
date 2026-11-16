# Database

Initialization scripts are maintained in `db/init/`:

1. `DongNamTrip_Full.sql`: base schema, roles, languages, development accounts and sample catalog.
2. `V001_account_area.sql`: favorites, activity, notices and preferences.
3. `V002_payment_choice.sql`: payment-method preference.
4. `V003_booking_quotation.sql`: booking and quotation columns, status CHECK constraints, filtered unique indexes. Idempotent; it also normalizes older private-tour rows (`Issued` to `Sent`, `NeedsInfo` to `Verifying`/`Unavailable`).

`db/init.sh` only runs when the database does not exist yet. On an existing database run V003 by hand, for example:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i db\init\V003_booking_quotation.sql
```

Use full initialization only for a new development database. Do not rerun the full script against an existing database or generate schema changes automatically from EF entities.

Public catalog results depend on publication status. Seeded tours may remain `Draft` and therefore not appear publicly.

For an existing database, apply future schema changes through an explicit, reviewed migration procedure. Restarting Docker does not automatically migrate an existing database.
