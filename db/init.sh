#!/bin/bash
# Creates the DongNamTrip database from db/init/*.sql, only if it does not exist yet.
# Never re-runs on an existing database (see README: "do not re-run the init script").
set -euo pipefail
SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -C -S db -U sa -P "$MSSQL_SA_PASSWORD" -b)

shopt -s nullglob
files=(/init/*.sql)
seeds=(/seed/*.sql)
if [ ${#files[@]} -eq 0 ]; then
  echo "db-init: no *.sql in db/init; skipping. Add the team's schema scripts, then 'docker compose down -v && docker compose up'."
  exit 0
fi

exists=$("${SQLCMD[@]}" -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'DongNamTrip'" | tr -d '[:space:]')
if [ "$exists" != "0" ]; then
  echo "db-init: database DongNamTrip already exists; nothing to do."
  exit 0
fi

"${SQLCMD[@]}" -Q "CREATE DATABASE [DongNamTrip]"
# Do not leave a half-initialised database behind: the next run would skip it.
trap '"${SQLCMD[@]}" -Q "DROP DATABASE IF EXISTS [DongNamTrip]"' ERR
for f in "${files[@]}" "${seeds[@]}"; do
  echo "db-init: applying $f"
  "${SQLCMD[@]}" -d DongNamTrip -f 65001 -i "$f"
done
trap - ERR
echo "db-init: done."
