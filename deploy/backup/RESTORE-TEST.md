# Quarterly restore test (D-024, REQ-023: RTO 2 days)

Do this once per quarter (January, April, July, October) and after every change to the backup
service. It restores the newest dump into a throw-away database container, never into the
production database.

1. List the dumps and pick the newest daily one:
   `docker compose run --rm --entrypoint ls backup -l /backups/daily /backups/monthly`
2. Start a throw-away PostgreSQL on the backup volume (no published port, separate network):
   `docker run -d --name restore-test -e POSTGRES_PASSWORD=<temporary password> -v propertyadmin_pgbackup:/backups:ro postgres:18-trixie`
3. Wait until it is ready: `docker exec restore-test pg_isready -U postgres`
4. Restore: `docker exec restore-test sh -c "createdb -U postgres restoretest && pg_restore -U postgres -d restoretest --no-owner /backups/daily/<file>.dump"`
5. Check the data: `docker exec restore-test psql -U postgres -d restoretest -c "select count(*) from \"Owners\"; select count(*) from \"Invoices\"; select max(\"Date\") from \"Activities\";"`
   The counts must match production within one day of changes, and the newest activity date
   must be at most one day old.
6. Remove the container: `docker rm -f restore-test`
7. Record the date, the dump file, the counts, the time it took and the result in the operations
   log. A failed restore is an incident: fix the backup before anything else.

Notes
- The temporary password is typed at the prompt, not stored.
- The backups stay on the production host (REQ-026 off-host copy is later; risk accepted in Q-039).
