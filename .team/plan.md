# Delivery plan

_Owner: orchestrator; the architect estimates the story points, the sanity agent checks the
plan against the project boundaries. Lock this file before you change it (see `RULES.md`)._

**Approved by the user:** 2026-11-20, 16:47 (S2 review: plan change for the S2 additions, the demo test per sprint (rule 15) and the code checks of D-051; Q-110 yes). Earlier: 2026-11-20 (Q-093: D-041 added to S2 and S3; owner screen regression tests T-057 moved from S2 to S3; all dates shifted to start from 2026-11-20); 2026-11-11 (sprints on working days; cut list agreed; README added in S10, Q-084)

The user sees results early: the functionality is delivered in sprints (of the length in
`project.md`), per requirement or use case, the most valuable first. **Sprint 1 always ends with
a working first screen**: a thin slice through every layer that the user opens and uses (without
a user interface: the first working call or report). Every sprint ends with a sprint review in
which the user sees the working result and accepts it or asks for changes. Nothing is built
that is in no sprint; a change of this plan needs the user's approval again.

## Sprints

| Sprint | Dates | Goal: what the user can see or do at the end | Requirements | Story points | Demo: how the user sees it | Status |
|---|---|---|---|---|---|---|
| S1 | 2026-11-12 to 2026-11-20 | Start the app on the laptop, sign in safely, and walk through the main workflow | REQ-017, REQ-001 | 8 | `docker compose up -d --build` (done first), open `https://localhost/Account/Login`: sign in as the seed admin; add an asset, an expense, an activity, an invoice with PDF, the income-versus-expenses report, switch Dutch/English; 5 wrong passwords lock the account (the sign-in page shows the same message as for a wrong password, on purpose, D-036; the Admin sees "Geblokkeerd" in `/Admin/Users`); more than 10 sign-in attempts within a minute give the 429 page (D-044); open any page in a private window without signing in: the sign-in page; `/Profile`: turn two-factor sign-in on and off with your own authenticator app (shown live at the sprint review, Q-099); `docker compose down` and `up`: data kept. On overrun: demo the running app, T-046 moves to S2 | Done |
| S2 | 2026-11-20 | Owners are complete (foreign addresses, any EU VAT number), never deleted, and their messages follow the user's language | REQ-002, REQ-011 | 19.5 | `/Owners`: save with only a name gives field errors; a foreign address and DE123456789 are accepted, XX123 is refused; delete an owner without assets: gone from the list, still on its invoices; owner with assets: refused with the reason; in Dutch and English; `dotnet test` all green, including the S2 demo test | Done |
| S3 | 2026-11-23 | Assets have a country and can move to another owner; owner screens covered by regression tests; every build checks packages and code | REQ-003, REQ-030 (owner screen tests, T-057), REQ-028 (B19, T14), REQ-032 (B20, warning clean-up D-054) | 14.75 | `/Assets/Edit`: country required and shown in `/Assets`; change the owner of an asset with an uninvoiced activity; `/Invoices/Create` for the new owner lists it; `dotnet test` with the owner screen tests and the S3 demo test green; `dotnet build` shows the package audit and the SDK code checks | Done (accepted by the user 2026-11-20 18:13) |
| S4 | 2026-11-24 | Invoices numbered without gaps, 14-day term, reports need a period; the per-owner overview shows each line under the owner at the time (Q-112); it lists each open invoice with due date and overdue mark, only for owners with open invoices (Q-115 a) | REQ-007, REQ-008, REQ-009, REQ-024, REQ-013 (BUG-13, D-058), REQ-030 and REQ-032 (moved from S3: T-192, T-179), REQ-011 (BUG-14, T-194) | 11 (incl. B23, F12, T19 for Q-115 a, 2.5; Q-116; Q-118 b lock 0.5; D-058 BUG-13 fix 0.75; moved from S3: T-179 and the #22 Should fix, 0.5; BUG-14 0.25, Q-123) | `/Invoices/Create` twice: consecutive numbers, due date + 14 days; only the most recent unpaid invoice can be deleted (Q-083); `/Reports` with "to" before "from": field error; `/Reports` overview per owner: each open invoice with number, date, total, due date and overdue mark, owners without open invoices left out; open `/Profile`, then `/Owners`: still signed in (BUG-13); PostgreSQL tests pass with Docker running | Current |
| S5 | 2026-11-25 | A removed permission is refused at once; every endpoint checked per role | REQ-012, REQ-019 | 6.5 | `/Admin/Roles`: remove a permission while a member is signed in elsewhere; their next save is refused without signing out; matrix tests green | Planned |
| S6 | 2026-11-26 | Two people editing the same record cannot overwrite each other | REQ-007 | 4.5 | `/Owners/Edit` in two tabs: save the first, then the second: "changed by someone else, reload" | Planned |
| S7 | 2026-11-27 | Separate database accounts, migrations as a step, backup and restore proven | REQ-023, REQ-017, REQ-025 | 5 | `docker compose up` on an empty volume starts by itself; only the proxy publishes ports; `http://localhost` redirects to HTTPS; restore a backup and see the same owners | Planned |
| S8 | 2026-11-30 | Lists stay fast with seven years of data | REQ-022 | 5.5 | `/Owners`, `/Activities`: 50 per page with next and previous; query-plan tests use the indexes; one timing run on the laptop with seven years of sample data (Q-082) | Planned |
| S9 | 2026-12-01 | Inactive owners proposed for anonymization, an Admin confirms | REQ-024 | 5.5 | Admin screen "Anonymization proposals": an owner inactive for 8 years is listed; confirm: personal data replaced, invoice amounts kept | Planned |
| S10 | 2026-12-02 | Remaining checks, the final "built, not verified" list and a deployment README | REQ-029, REQ-021, REQ-031 | 2.5 (T-030 and T7 cut, D-053) | `dotnet test` all green; in Production `/openapi/v1.json` gives 404; a wrong date in a form gives the error before posting; testing.md lists what is built, not verified; README explains set-up, `.env`, start, backup and restore | Planned |

**How to sign in, for every demo:** start the app with `docker compose up -d --build --wait` in
the folder it runs from, open `https://localhost/Account/Login` (on the laptop the browser warns
about Caddy's local certificate: continue), and sign in as the seed admin: the user name is
`SEED_ADMIN_USERNAME` (default `admin`) and the password is `SEED_ADMIN_PASSWORD`, both in `.env`
of that folder. The password is never written in the team files, the chat or a commit. The S5
demo also needs a second account without Admin rights: the seed admin creates it in
`/Admin/Users` during the demo, with a password of the user's choice (Q-094).

Total: 82.75 story points (Q-123: plus the #22 Should fix 0.25 and BUG-14 0.25 in S4; T-179 CA1305 clean-up 0.25 in S3, within the D-054 cap, Q-122; D-058 BUG-13 fix 0.75 in S4, user "fix now" 2026-11-20 18:00; Q-118 b: invoice number lock 0.5 in S4, user 2026-11-20; before: 80.75: 79.25, plus S4 from 7.5 to 9 (corrected from 9.25 by the build list, Q-116) for the REQ-009 owner overview, approved by the user 2026-11-20, Q-115 a): 76.5 after the user cut T-030 (3) and T7 (2) on 2026-11-20 (D-053; was 81.5), plus the B20 warning clean-up 1.75 (D-054, within the 2-point cap of Q-111) and the per-owner overview by owner at the time 1 (Q-112 b, S4). Per sprint: S2 19.5 (D-041, D-045, D-046, D-049 with B17, F8, B18, T-112, T-114, T-119); S3 to S10 each include their demo test (0.5, S7 1; rule 15); S3 also B19 and T14 (REQ-028, 1), B20 (REQ-032, 0.5) and its clean-up (1.75). Estimates: architecture/build-tasks.md; sanity reviews: T-069, T-078, T-128, T-139. Velocity after S1 and S2: about 2.7 points per active hour. Cut list, in this
order, when it does not fit the 40 hours: ~~T-030 browser tests (3), T7 error-mapping tests (2)~~
(cut by the user, D-053, 2026-11-20), S9 anonymization (5.5 with its demo test), S6 concurrency (4.5 with its demo test). Demo tests are never cut, except with their sprint.

Status: `Planned`, `Current` (one sprint at a time), `Review` (waiting for the user's sprint
review), `Done`.

## Sprint reviews

Per sprint, newest first: the date, what the user saw, the user's verdict (accepted, or the
changes asked for), and what moved to a later sprint.

- **S3, 2026-11-20: accepted by the user** (in the chat, 18:13). Shown: the app on main 1cdbadf
  with `docker compose` from `.worktrees/s1-test` (also holds S4's #23, approved by the user);
  REQ-003 assets (country, move with uninvoiced lines, BUG-4, BUG-5), REQ-030 screen regression
  tests (sign-in with 2FA, access per role, save, edit and invalid post per form), REQ-028 and
  REQ-032 package audit and SDK code checks. Merged: #19, #20, #21, #22 (571 tests on main).
  Moved to S4 (team-internal, within REQ-032 and REQ-030): T-179 CA1305 clean-up (0.25) and the
  #22 review's Should fix (expected message in the six summary-only forms, 0.25). Run in
  parallel with S4 (D-055).

- **S2, 2026-11-20: accepted by the user** (in the chat, 16:47), with the plan change. Shown: the
  app on main 06a87d5 with `docker compose` from `.worktrees/s1-test` on the S1 data (both owner
  migrations ran on PostgreSQL, old addresses kept as street; T-109), and the automated S2 demo
  test in Dutch and English on PostgreSQL 18 (T-112). Merged: #12, #13, #14, #15, #16, then #17
  and #18 after the review (456 tests on main 69e2c20). Fixed: BUG-2, BUG-3, BUG-10, BUG-11,
  BUG-12. Not seen in a real browser by the team (curl and the demo test). Moved to a later
  sprint: T-057 owner screen tests (S3, Q-093). Q-110: yes, the SDK code checks become REQ-032.

- **S1, 2026-11-20: accepted by the user** (in the chat, 14:58). Shown: the app running with
  `docker compose` from `.worktrees/s1-test` (main f3542bc), sign-in as the seed admin, the main
  workflow, euro in English, lock-out, the 429 page, data kept after a restart (T-094, T-097).
  Covered by automated tests only: two-factor on and off on `/Profile`. Delivered 9.5 story
  points (8 planned, 1.5 unplanned) plus BUG-7, BUG-8 and BUG-9; about 2.7 points per active hour
  (T-099). Moved to a later sprint: nothing. Open, planned: BUG-3 (High) in S2, S-20 (Medium) in
  S7 (T-039).
