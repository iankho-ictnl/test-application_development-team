# Development and test on the laptop

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged)._

(D-039, proposed, 2026-11-11, replaces D-038: Docker Desktop on the laptop, Q-078; going live is
not part of the project, Q-079.) The architecture does not change; only where it is checked does.

| Part | Needs | Where it is checked |
|---|---|---|
| `dotnet build`, fast test suite (SQLite, in-memory API host) | .NET 10 SDK | Every change, no Docker |
| Screen regression tests (REQ-030, D-030, T-057) | .NET 10 SDK | Every change, no Docker |
| Browser tests of the validation script (D-025, T-030) | Playwright browsers, a frontend on a local port | Every change, once the browsers are installed |
| Running the app by hand, `docker compose up`, data kept (REQ-017) | Docker Desktop, a local `.env` with fictitious secrets and `PUBLIC_HOST=localhost` | Laptop: `https://localhost` (Caddy's local certificate; the browser warns once) |
| PostgreSQL tests (D-031, T8: migrations, `xmin` 409, invoice sequence, `numeric(18,2)`, query plans) | Docker Desktop running (Testcontainers) | Laptop; skipped with a reason, never failed, when Docker is not running |
| Migrations as `propertyadmin_migrator`, app as `propertyadmin_app` (D-024) | Docker Desktop | Laptop, in compose and in T8 |
| Only 80/443 published, HTTP to HTTPS, forwarded headers only from the proxy (REQ-025, D-023) | Docker Desktop | Laptop |
| Daily backup run and the restore test (REQ-023, `deploy/backup/`) | Docker Desktop | Laptop: restore into an empty compose project |
| Let's Encrypt certificate for a public host name (REQ-025) | A public host name and DNS | Not in this project: built, not verified |
| Timing run with the seven-year data set (REQ-022, T9) | The production server (Q-052) | Q-082: laptop (indicative) or built, not verified |
| Hosting costs (REQ-027) | A hosting contract | Not in this project: built, not verified |

- Until a part has run where the table says, testing.md and the report list it as "built, not
  verified" (RULES rule 11). The go-live verification of D-038 is dropped (Q-079).
- No compose override that publishes `web` or `api`: the laptop uses the proxy like production.
- xUnit 2 stays (Q-070, D-019 rejected): xUnit 2 has no built-in skip at run time, so the
  tester chooses how the PostgreSQL tests are skipped without Docker (D-031).
- T-039 moves migrations out of the API start-up; `docker compose up` must still start an empty
  database by itself (a migration step in compose before `api`), or REQ-017 breaks.
