# Build task list

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged)._

Every piece names the item it is for (`for:` in its task; one branch and one pull request per
item, RULES.md "Version control"). Where a piece serves several decisions, the first one is the
item and the branch; the others are listed as related.

- B1 backend-developer-1 (for REQ-001 to REQ-021, the first build; done before version control): Contracts and API skeleton, EF model, migration, Identity/JWT, seed admin.
- B2 backend-developer-1 (for REQ-001 to REQ-021, first build): services, endpoints and unit tests.
- B3 backend-developer-1 (for REQ-017): Dockerfiles and docker-compose.
- F1 frontend-developer-1 (for REQ-001, REQ-011, REQ-021): frontend sign-in, API client, localization, layout.
- F2 frontend-developer-1 (for REQ-002 to REQ-006): CRUD pages Owners, Assets, VAT rates, Expenses, Activities.
- F3 frontend-developer-1 (for REQ-007 to REQ-009): Invoices and Reports pages.
- T1 tester (for REQ-019): integration tests, including owner isolation at API level.

**Upgrade tasks** (architect, 2026-11-06; decisions #12 to #20). They start after the user has
accepted those decisions. U1 and U3 are merged together, because all projects move to .NET 10 at
once (they share `PropertyAdmin.Contracts`); U2 goes with them. U5 comes after U1 to U3, because
PostgreSQL 16 stays supported until 2028-11-09 while .NET 8 ends on 2026-11-10. Total: 12 story
points.
- U1 backend-developer-1 (3 story points; for D-012, related D-013, D-016, D-017): `PropertyAdmin.Contracts` and `PropertyAdmin.Api` to
  `net10.0`; packages as in Technology versions (#12, #13, #17); the new `global.json`; the OpenAPI
  change (#16); `dotnet-ef` 10.0.14. Run `dotnet ef migrations has-pending-model-changes` and add
  a migration if it reports changes (#12). The build shows no warnings for obsolete APIs and no
  NuGet audit warnings.
- U2 backend-developer-1 (1 story point; for D-014): `PropertyAdmin.Api/Dockerfile` to the `10.0-resolute`
  images (#14); an invoice PDF renders in the container (fonts).
- U3 frontend-developer-1 (2 story points; for D-012, related D-013, D-014, D-018, D-020): the admin frontend to `net10.0`;
  System.IdentityModel.Tokens.Jwt 8.23.0, QRCoder 1.8.1 and the container tools 1.23.0 (#13,
  #18); `KnownNetworks` replaced by `KnownIPNetworks` in `Program.cs`, with the content that
  question 24 decides; the frontend Dockerfile to the `10.0-resolute` images (#14); Bootstrap
  5.3.8 (#20).
- U4 frontend-developer-1 (1 story point; for D-020): jQuery and the validation libraries as question 22
  decides (#20). Blocked by question 22.
- U5 backend-developer-1 (2 story points; for D-015): PostgreSQL 18 in `docker-compose.yml` with the `pgdata`
  volume mounted at `/var/lib/postgresql` (#15); the procedure for existing data as question 20
  decides. The data step is blocked by question 20.
- U6 tester (3 story points; for D-019, related D-012 to D-020 for the regression check): the test project to `net10.0` and xUnit v3 (#19). Regression check
  of the upgraded solution with `docker compose up`: migration at start-up on an empty and on an
  existing database; sign-in with and without two-factor sign-in (QR code); every page per role;
  invoice PDF with fonts; reports; Dutch and English formatting; sign-in survives a restart of
  the web container (data-protection keys volume); after U5, the PostgreSQL data is kept.

**Tasks from the 2026-11-09 checks** (each starts only after its decision is accepted)
- B5 backend-developer-1 (3; for D-022): central error handler and cancellation tokens (D-022, accepted;
  task T-029). T2 tester (1; for D-022): a check per status code.
- B6 backend-developer-1 (3) and U8 backend-developer-1 (2) (for D-024, related REQ-022, REQ-023): database roles, production
  migrations, `xmin`, sequence, indexes, paging, `backup` service with 14 daily / 12 monthly
  dumps (D-024, REQ-022, REQ-023).
- U7 backend-developer-1 (2; for D-023, related REQ-025): Caddy proxy, `PUBLIC_HOST`, no published ports for `web`, `api`,
  `db` (D-023, REQ-025).
- T4 tester (3; for D-025): browser tests for the validation script (D-025, task T-030).
- **Stopped (Q-075 b, D-037, 2026-11-10):** B7 to B11 are done (T-032 to T-036, merged); F4
  (T-037) and T3 (T-038) are dropped, 8 story points. T5 (T-057, D-030) stays as regression
  tests for REQ-030 (Q-076, D-037 amended 2026-11-11). The rules below were
  for the D-026 branch only; the working-folder rule no longer holds back T-030 or T-039.
- B7 to B11, F4, T3 (28 in total; all for D-026, one branch `change/D-026-v1-layout`, one pull
  request): restructuring to the code layout (D-026; replaces B4/F4 of D-021). Details and order
  in D-026. Version control for these pieces (RULES.md, checked 2026-11-10):
  - Safety net first: a commit of the starting point on `main` and the characterization tests of
    T-042, green, committed before the first moved file.
  - One commit per feature or use case, not per piece: B8 and B10 (5 points each) are too large
    for one reviewable commit. Every commit builds and keeps the characterization tests green.
  - Moving code (restructuring) and changing behaviour never share a commit. Adding the
    `/api/v1` routes next to the legacy `/api` routes (Q-045) and later removing `/api` are
    behaviour changes: each its own commit. Moved migrations go in their own commit.
  - B7 to B11 are one developer in a row; F4 and T3 change other files (frontend, tests) and
    may run on the same branch after B11, each committing only its own files.
  - No code for another item (for example T-030 for D-025, T-039 for D-024) is changed in the
    same working folder while D-026 work is in progress, unless it uses its own git worktree.
- Proposed with D-027, D-030 and D-032 (T-048 review, 2026-11-10; each only after acceptance):
  - T5 tester (3; for D-026, related D-030; branch `change/D-026-v1-layout`): frontend
    characterization tests (in-memory frontend host against the in-memory API), green and
    committed before T-037's first commit; T-037 waits for it. F5 frontend-developer-1 (0.5,
    within T5's branch): the one line `public partial class Program;` in the frontend.
  - D-032: within T-035 (B10, move) and T-036 (B11, remove the API's PDF references); no new points.
  - D-027: within T-038 (T3), 1 point.
- Anonymization of inactive owners (for REQ-024): eligibility and confirmation are specified from
  Q-041 and Q-044; estimate and split implementation work only after REQ-024 is approved.
- Proposed with D-033 (Q-056, 2026-11-10; only after acceptance): B12 backend-developer-1 (1;
  for REQ-008, related REQ-009, D-033): `RequestValidation.cs` for both report use cases (`from`
  and `to` required, `to` not before `from`, 400 per field); the tester changes
  `Missing_period_currently_returns_200_with_zero_totals` to expect 400 in the same item.
- Accepted D-028, D-029 and D-031 (T-055 review, 2026-11-10):
  - T6 tester (3; for REQ-019, related D-028, REQ-012, REQ-013): authorization matrix tests
    against `/api/v1`, after T-036. The enumeration test allows exactly the endpoints listed
    under "Endpoints without a permission policy"; it fails on the VAT-rate reads until S-22
    (T-045) is fixed. Permission removal and addition (Q-049, D-034): a member's existing token
    gets 403 at the next request after a permission is removed from the role, and 2xx after it
    is added, without signing in again; these cases fail until T-058 is built.
  - T7 tester (2; for D-022, related D-029; replaces T2): error-mapping tests; 502/504 not
    applicable today (Q-051), a database failure through a test double gives 500; 409 after
    T-039, 422 after BUG-1.
  - T8 tester (3; for D-024, related D-031, REQ-022, REQ-018): PostgreSQL collection with
    Testcontainers.PostgreSql on the production image (`postgres:18-trixie`) and the D-024 roles
    (migrations as `propertyadmin_migrator`, tests as `propertyadmin_app`); also checks the
    REQ-022 index criterion with `EXPLAIN` on the seven-year data set. Runs on the laptop with
    Docker Desktop (Q-078, D-039); skipped with a reason when Docker is not running; on xUnit 2
    (Q-070), the skip is the tester's choice. Split per requirement with the parts of T-039
    (T-068 proposal): sequence and `numeric(18,2)` (REQ-007), `xmin` 409 (REQ-007), roles and
    migrations (REQ-023), query plans (REQ-022).
  - T9 tester with the user (1; for REQ-022, related D-031): the one-off timing run on the
    production server before go-live, fictitious data only (Q-052). Before real data is
    entered, the database and backup volumes are recreated empty, with the user's explicit go
    (destructive); signed in through the real login with a temporary fictitious account; no
    configuration change or authentication bypass on production.
- Accepted D-034 (Q-055, 2026-11-10): T-058 backend-developer-1
  (2; for REQ-012): per-request permissions as D-034, with tests; already created. Includes the
  security officer's conditions C1 to C5 (Q-057, Q-058; see Authentication and authorization), with
  tests for each: a renamed role and a new role with the old name (C1), the cache cleared only
  after commit in both role endpoints (C2), no stale permission or owner-id claim (C3), a failed
  read gives 500 (C4), a user without permissions gets 403 on every policy endpoint and 200/204
  on the own-account endpoints, and a locked-out user gets 401 on them (C5, Q-058). C6 goes into
  the deployment notes. Story points unchanged (Q-058 adds none).

**Replan after D-026 stopped, and estimates of the open tasks (T-064, Q-069, 2026-11-10).**
Story points of every unfinished product task; the orchestrator updates the task headers
(`waits-for` is the owner's) and drops T-037, T-038 and T-057.

| Task | Owner | For | Story points | Waits for after D-037 | Note |
|---|---|---|---|---|---|
| T-011 | tester | D-019 | 3 | Q-070 (user) | was T-038; U6: tests to xUnit v3 and the regression check; also covers S-29 of T-047; D-019 serves no approved requirement, so it waits for the user |
| T-030 | tester | D-025 (REQ-021) | 3 | - | was T-038 (working-folder rule of the D-026 branch, now gone) |
| T-039 | backend-developer-1 | D-024 | 5 | - | unchanged |
| T-044 | backend-developer-1 | REQ-001 | 3 | - | six findings (S-18 started); its contract changes (D-036) also go into the `Contracts` types, in T-046 |
| T-045 | backend-developer-1 | REQ-019 | 1 | - | S-22: read policy on both VAT-rate reads (one action serves `/api` and `/api/v1`); S-27: `:-` in compose and the deployment note |
| T-046 | frontend-developer-1 | REQ-001 | 3 | T-044 | was T-037, T-044. In `ApiClient.cs` and the pages, with `Contracts` (D-037): `TwoFactorSetupResponse` key and URI nullable, a new `TwoFactorDisableRequest(Password?, Code?)`, the profile page asks for one of them (S-19); `POST api/auth/logout` before the cookie is deleted (S-26); sign-in rate limit with a 429 page (S-17); security headers (S-25) |
| T-047 | tester | D-019 now; S-30 serves REQ-019 | 1 | - for S-30; S-29 goes with T-011 | was T-038. Proposal: the orchestrator links T-047 to REQ-019 for S-30 (OpenAPI and Swagger give 404 in Production: no extra endpoint without a policy) and moves S-29 into T-011 |
| T-049 | backend-developer-1 | BUG-3 | 2 | - | owner marked inactive (column, migration, lists filter), never deleted |
| T-050 | backend-developer-1 | BUG-1 | 1 | Q-063 (user) | 422 mapping and the tests that expect 400 |
| T-051 | backend-developer-1 | BUG-2 | 1 | - | address, email and phone required on create and update; the owner form already has the fields and shows the 400 per field |
| T-052 | backend-developer-1 | BUG-4 | 2 | - | uninvoiced lines move with the asset, in one transaction |
| T-053 | backend-developer-1 | BUG-5 | 2 | - | `Country` on Asset (migration, V1 request/response, validation) |
| F6 (new, not yet a task) | frontend-developer-1 | BUG-5 | 1 | T-053 | `Country` in `Contracts` `AssetSaveRequest`/`AssetDto` and on the asset forms and list (D-037); without it every asset save from the frontend fails after T-053; same branch as T-053 |
| T-054 | backend-developer-1 | BUG-6 | 1 | - | due date = invoice date + 14 days; how `paymentTermDays` is treated follows the BUG-6 row |
| T-058 | backend-developer-1 | REQ-012 | 2 | - | unchanged |
| T-057 | tester | REQ-030 | 3 | - | kept as regression tests (Q-076, added 2026-11-11); runs without Docker |
| Go-live verification (D-038) | - | - | 0 | - | dropped (Q-079, D-039); its laptop parts are in the T-068 sprint proposal |
| Total | | | 36 | | 31 of the 2026-11-10 replan (T-011 and T-050 are done since), plus T-057 (3) and the go-live verification (2); Q-078 may add 1 to 2 |

T-014 is Done and needs no estimate. Dropped by D-037: T-037 (5), T-038 (3).

**Sprint proposal (T-068, 2026-11-11).** The table above is superseded by the estimates in the
report of T-068: all open work per requirement in 1-day sprints (54 story points, 10 sprints),
including work not estimated before (laptop compose run, B12, T6, T7, the frontend parts of
`xmin` and paging, REQ-024 anonymization). The orchestrator puts it in `plan.md`.

**Owner and asset address, KvK and BTW (T-074, Q-087, D-041, 2026-11-11).** Interface contract,
for `/api/v1` and the unversioned `/api` routes alike (same controllers): the owner
request and response replace `address` with `street`, `houseNumber`, `postalCode`, `city`,
`country` (all required strings) and add `kvkNumber` and `vatNumber` (optional, `null` when
empty; the response returns the normalized BTW number; any EU VAT number format as in
Database architecture, Q-088). The asset request and response
replace `address` with the same five required fields. A missing or invalid field gives 400
Problem Details with the error at that field (`street`, `houseNumber`, …, `kvkNumber`,
`vatNumber`). `PropertyAdmin.Contracts` `OwnerDto`, `OwnerSaveRequest`, `AssetDto` and
`AssetSaveRequest` follow in the same item (D-037). Pieces (backend, screen and tests of one
requirement together; backend and frontend touch different files):

| Sprint | Piece | Owner | For | Story points | Waits for | What |
|---|---|---|---|---|---|---|
| S2 | B13 (extends T-051) | backend-developer-1 | REQ-002 (also closes BUG-2) | 3.5 (T-051 was 1; +0.5 for Q-088) | - | Owner entity, configuration and migration as D-041; V1 Owners Create and Update: `Request`, `RequestValidation` (five address fields, email, phone required, foreign addresses accepted; KvK format; BTW in any EU VAT number format with the 27 prefixes and the Dutch pattern for `NL`), `Response`, `Mapping`; GetAll and GetById responses; Logic models and mapping; unit tests per validation rule. Proposal: one branch and one task for REQ-002 instead of BUG-2 alone, because the same files change |
| S2 | B14 | backend-developer-1 | REQ-002 | 1 | B13 (entity) | Owner block on the invoice PDF in `InvoicePdfRenderer.cs` as in Database architecture; empty KvK and BTW lines left out |
| S2 | F7 | frontend-developer-1 | REQ-002 | 2 | B13 (contract above; may start from it) | `Contracts` owner types and `ApiClient.cs`; `/Owners` create and edit forms with the seven fields (KvK and BTW marked optional), the 400 per field, the list shows street with house number and city; Dutch and English labels (REQ-011) |
| S2 | T10 | tester | REQ-002 | 1 | B13, B14, F7 | API tests: each required field missing gives 400 at that field; KvK and BTW valid, invalid and empty, including a foreign VAT number (for example `DE123456789`, `EL123456789`), an unknown prefix (`XX123456`), a wrong Dutch one (`NL123`) and a foreign address (US-2.1d); the migration keeps the old address as street; the PDF text contains the owner block with and without KvK and BTW, and after a change of the owner the PDF of an existing invoice shows the new details (US-2.1c); the owner form save in the screen regression tests (T-057, REQ-030) uses the new fields |
| S2 | B16 | backend-developer-1 | REQ-002 (D-046, Q-102) | 0.5 | - | `docker-compose.yml`: `TZ: ${TZ:?Set TZ ...}` on `api` and `web`; `.env.example`: `TZ` with a comment (IANA name of the system's time zone, for example `Europe/Amsterdam`); check that every "today" in `PropertyAdmin.Logic` uses `TimeProvider.GetLocalNow()` (now only `OwnerService` InactiveSince); no Dockerfile change (`tzdata` is in the `10.0-resolute` images). The README line for `TZ` follows in REQ-031 (T-091) |
| S2 | T12 | tester | REQ-002 (D-046, Q-102) | 0.5 | B16 | Unit test: with a test `TimeProvider` whose local zone is `Europe/Amsterdam`, marking an owner inactive at 23:30 UTC (00:30 local) records the next day, and with local zone UTC the same day; `FixedTime` in `OwnerServiceTests.cs` sets `LocalTimeZone` so the tests do not depend on the machine's zone |
| S3 | B15 (extends T-053) | backend-developer-1 | REQ-003 (also closes BUG-5) | 3 (T-053 was 2) | - | Asset entity, configuration and migration as D-041 (`Street`, `HouseNumber`, `Country`); V1 Assets Create and Update request, validation, response and mapping; GetAll and GetById responses; unit tests. Same proposal: one task for REQ-003 |
| S3 | F6 (extended) | frontend-developer-1 | REQ-003 (also closes BUG-5) | 2 (was 1) | B15 (contract above; may start from it) | `Contracts` asset types; `/Assets` create and edit forms with the five address fields and the 400 per field; the list shows the address; Dutch and English labels; extends T-066 |
| S3 | T11 | tester | REQ-003 | 1 | B15, F6 | API tests per required asset address field; the migration keeps the old address as street; the asset form save in the screen regression tests uses the new fields |
| S2 | B17 | backend-developer-1 | REQ-002 (D-049) | 1 (unchanged; 64fb655 is most of it) | T-114 (same set-up) | Built in 64fb655: the owner-with-assets refusal (`OwnerService.HasAssets` English key, Dutch in `Resources/BusinessRuleMessages.nl.resx`, looked up in `ApiExceptionHandler` from `IRequestCultureFeature`, D-050): keeps as it is. Still to do after Q-108: `Program.cs` request culture from the query parameter `lang` only (`QueryStringRequestCultureProvider`, `QueryStringKey` and `UIQueryStringKey` = `lang`; the `AcceptLanguageHeaderRequestCultureProvider` goes; comment updated); `OwnerMessagesLanguageTests` (T-114's KvK/BTW test and the 64fb655 delete test) send `?lang=` instead of `Accept-Language`, cases `nl-NL`, `en-US`, `EN-us`, none, `de-DE` (Dutch), and `Accept-Language: en-US` without `lang` (Dutch: the header is ignored). New commit, no history rewrite. Branch `feature/REQ-002-manage-owners`, `.worktrees/REQ-002` |
| S2 | F8 | frontend-developer-1 | REQ-002 (D-049) | 1 | T-114, B17, #14 merged | A `DelegatingHandler` on the `ApiClient` HTTP client adds `lang=<CultureInfo.CurrentUICulture.Name>` to the query of every request (appended with `&` when the request already has a query, URL-encoded; no `Accept-Language`); `ApiPageModel.AddApiError` shows `r.Error` as received (the D-048 lookup `L[r.Error]` and the resource entry "Eigenaar heeft nog objecten…" go); `OwnerDeleteTests` stub answers in the language of the `lang` it receives, plus a unit test of the handler (no query, existing query, both cultures); the owner form shows the KvK and BTW messages in English |
| S2 | T13 | tester | REQ-002 (D-049) | 0 | F8 | No new task: the S2 demo test (T-112) already switches to English; it checks that the KvK, BTW and owner-with-assets messages show in English there |
| S2 | B18 | backend-developer-1 | REQ-002 (D-049) | 0.5 | - | From the #16 reviews (T-124, T-125): the owner Create and Update `[Required]`, `[EmailAddress]` and `[MaxLength]` messages follow `lang` as the D-049 contract says (they are the framework's English defaults now, also for `nl-NL`): an `ErrorMessage` key (English text) per attribute, Dutch in `Resources/V1/Owners/OwnerMessages.nl.resx`, data annotations localized through `OwnerMessages`; a test per culture for one required field and the email format on both routes. Also fix the doc comment of `OwnerMessages.cs` (says Accept-Language; it is `lang`). Branch `feature/REQ-002-manage-owners`; not needed for the S2 demo (the frontend checks these fields first) |
Added: 6 story points in S2 (6 to 12) and 3 in S3 (5 to 8); plan total 56 to 65. Q-088 (T-075)
adds 0.5 to B13: S2 12.5, plan total 65.5. Q-089 (a) adds nothing (no invoice copy). D-046 (Q-102) adds B16 and T12: 1 story point in S2.
Q-101 (a) adds nothing. D-049 (T-117) adds B17 and F8: 2 story points in S2. Q-108 (the
`lang` query parameter instead of `Accept-Language`) keeps B17 and F8 at 1 story point each:
the change is the culture provider and the tests, the rest of 64fb655 stays. The #16 reviews
(T-125) add B18: 0.5 story point in S2. The sanity
agent checks the new totals against the 40-hour boundary.

**Demo tests (rule 15) and the unplanned S2 work (Q-106, 2026-11-20).** Story points of the work
that `plan.md` did not have yet:

| Sprint | Piece | Owner | For | Story points | Note |
|---|---|---|---|---|---|
| S2 | T-112 | tester | REQ-002 (D-047) | 2 | first demo test: the `DemoStack` harness (PostgreSQL 18 container, migrations, seed admin from test configuration, sign-in through `/Account/Login`) plus the S2 steps in Dutch and English; waits for Q-104 |
| S2 | T-114 | backend-developer-1 | REQ-002 | 1 | done (API request localization, KvK and BTW messages) |
| S2 | T-119 | frontend-developer-1 | BUG-11 | 0.5 | done (19 success messages in the resource files) |
| not planned yet (orchestrator: BUG-13 is against REQ-013 and REQ-001, which are in no current sprint; plan it or ask the user) | B24 | backend-developer-1 | BUG-13 (D-058) | 0.5 | `PropertyAdmin.Logic/V1/Auth/AuthService.cs` `GetTwoFactorAsync`: create a missing authenticator key without changing the security stamp; unit test that the stamp is unchanged after the first GET auth/2fa; branch bug/BUG-13-profile-sign-out |
| same as B24 | T20 | tester | BUG-13 (D-058) | 0.25 | waits for B24; `SignInScreenTests`: after /Profile the user stays signed in; the screen access test no longer has to visit /Profile last |
| S3, S4, S5, S6, S8, S9, S10 | demo test S<n> | tester | the sprint's first requirement (D-047) | 0.5 each | extends the harness with the sprint's demo steps; with the sprint's first requirement |
| S7 | demo test S7 | tester | REQ-023 (D-047) | 1 | empty-volume start, migrations as a step and restore are steps on the database container, more than a page walk |

Added: 2 story points still to do in S2 (T-112; T-114 and T-119 are done and already in the
time used) and 4.5 in S3 to S10. A demo test is never on the cut list; when S6 or S9 is cut, its
demo test (0.5) goes with it.

**Code checks with the .NET SDK only (D-051, T-111, 2026-11-20).** REQ-028 is not in a sprint of
`plan.md` yet: the orchestrator plans B19 and T14 (1 story point); B20 waits for the user's answer
to Q-110 (rule 14) and is not built before that. T13 was taken already (D-049), so the tester piece
of D-051 is T14.

**Warning clean-up (D-054, 2026-11-20), capped at 2 story points (Q-111).** B20 (T-136) found 318
warnings: CA1707 261 (260 underscore test names per D-027, 1 the frontend namespace), CA1711 3 and
CA5350 1 in the tests, 53 others. CA1707 and CA1711 are allowed in PropertyAdmin.Tests only (its
`.editorconfig` section); CA5350 gets one justified line suppression; the 53 others are fixed in
B21, F9 and T15 (1.75 story points). Known warning, listed in `testing.md` by the tester: the
frontend namespace `test_application_for_development_team` (CA1707), because renaming it touches
every frontend file. Each piece touches its own files: B21 the root `.editorconfig` and the Api,
Logic and Data projects; F9 the frontend project; T15 the test project and `testing.md`. All on
branch `change/D-051-code-checks` (`.worktrees/D-051`).

| Sprint | Piece | Owner | For | Story points | Waits for | What |
|---|---|---|---|---|---|---|
| not planned yet | B19 | backend-developer-1 | REQ-028 (D-051 part 1) | 0.5 | - | New `Directory.Build.props` in the repository root with `<NuGetAudit>true</NuGetAudit>` and `<NuGetAuditMode>all</NuGetAuditMode>`; restore and build all projects; list every package warning (NU1901 to NU1904) in the report, never suppress one; each warning found becomes a question to the architect for an upgrade |
| S3 (done, T-136: 318 warnings) | B20 | backend-developer-1 | REQ-032 (D-051 part 2) | 0.5; the clean-up is B21, F9, T15 (1.75, D-054) | Q-110, B19 | Only after the user approves Q-110: the same `Directory.Build.props` gets `<AnalysisLevel>latest-recommended</AnalysisLevel>`, `<AnalysisModeSecurity>All</AnalysisModeSecurity>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`, `<Nullable>enable</Nullable>`; a new `.editorconfig` with the code style the code already uses; no warning hidden or suppressed, no `TreatWarningsAsErrors`; the report lists the warnings per project and rule id, so the architect estimates the cleanup task |
| S3 | B21 | backend-developer-1 | REQ-032 (D-054) | 0.75 | T-136 (B20, done) | Root `.editorconfig`: add the section `[PropertyAdmin.Tests/**.cs]` with `dotnet_diagnostic.CA1707.severity = none` and `dotnet_diagnostic.CA1711.severity = none` (nothing else in it). Fix the 11 warnings in PropertyAdmin.Api (CA1305 1, CA1848 2 and CA1873 1 with source-generated `[LoggerMessage]` methods, CA1725 1), PropertyAdmin.Logic (CA1716 5: rename the interface parameters `to`/`date`, for example `toDate`, `onDate`, together with their implementations and named-argument callers) and PropertyAdmin.Data (CA1725 1). Behaviour unchanged: build 0 warnings in these three projects, all tests green |
| S3 | F9 | frontend-developer-1 | REQ-032 (D-054) | 0.5 | - | Fix 11 of the 12 frontend warnings: CA1305 2 (the culture the screen needs: current culture for display, invariant for values sent to the API), CA1848 3 (`[LoggerMessage]`), CA1725 2, IDE0161 2 (file-scoped namespace in `Error.cshtml.cs`, `Index.cshtml.cs`), CA1310 1 (`StringComparison.Ordinal`), CA1859 1. The namespace CA1707 stays (D-054). Behaviour unchanged, frontend tests green |
| S3 | T15 | tester | REQ-032 (D-054) | 0.5 | B21 (the `.editorconfig` section) | Fix the PropertyAdmin.Tests warnings: CA1861 20 (`static readonly` arrays in AuthUsersTests), CA1305 10, CA1001 1 (`DemoStack` disposes its fields: `IAsyncDisposable`), CA5350 1 (`#pragma warning disable CA5350` / `restore` around the HMACSHA1 line only, with the comment "RFC 6238 TOTP uses HMAC-SHA1"). Then build the solution once: expect 1 warning (the frontend namespace CA1707) and list it in `testing.md` under known warnings with D-054; any other warning left is listed there too, not fixed above the cap (Q-111). All tests green |
| not planned yet | T14 | tester | REQ-028 (D-051 part 3) | 0.5 | - | Remove `coverlet.collector` from `PropertyAdmin.Tests/PropertyAdmin.Tests.csproj`; coverage with Microsoft's collector from `Microsoft.NET.Test.Sdk`: `dotnet test --collect "Code Coverage" --results-directory .tmp/TestResults --blame-hang-timeout 5m`; all tests green and a coverage file produced; the command in `testing.md` |

**Sprint 4, in parallel with sprint 3 (T-155, D-055, D-056, 2026-11-20).** S4 builds REQ-007,
REQ-008, REQ-009 and the delete rule of REQ-024 (Q-083 c) with backend-developer-2,
frontend-developer-2 and the tester. One branch and worktree per item: REQ-007 on
`feature/REQ-007-invoices` (`.worktrees/REQ-007`, exists; T-054 runs there), REQ-008 on
`feature/REQ-008-report-period` and REQ-009 on `feature/REQ-009-owner-overview` (the orchestrator
creates both, each in `.worktrees/REQ-00n`). The REQ-024 delete rule goes on the REQ-007 branch
(`for: REQ-007`), because it is the same service method and keeps the REQ-007 numbers without
gaps. Backend pieces include their unit tests; the tester adds the API, PostgreSQL, screen and
demo tests.

Interface contract changes (`/api/v1` and `/api` alike, same controllers):
- `POST invoices` (D-057, which settles BUG-6 instead of the first version of D-056): the request
  stays `ownerId`, `date`, `paymentTermDays`; any `paymentTermDays` other than 14 gives 400 Problem
  Details "De betalingstermijn is 14 dagen." / "The payment term is 14 days." (by `lang`); the
  response's `dueDate` is always `date` + 14 days. Built in 17596d4 (T-054).
- `DELETE invoices/{id}`: 204 for the most recent open invoice; 400 Problem Details with the
  localized reason for any other open invoice and, as now, for a paid one; 404 when it does not exist.
- `GET reports/asset-results` and `GET reports/owner-overview`: `from` and `to` required (`yyyy-MM-dd`);
  missing, invalid or `to` before `from` gives 400 with the error at `from` or `to`, localized by
  `lang` (D-033, D-049). Responses unchanged.
- `GET reports/owner-overview` (T-152): same fields; activity and expense amounts count under the
  invoice's owner when invoiced, else under the asset's current owner, so one asset can count for
  two owners after a move. With Q-115 (a), see B23.

| Order | Piece | Owner | For | Story points | Waits for | Files | What |
|---|---|---|---|---|---|---|---|
| 1 | T-054 (done, 17596d4) | backend-developer-2 | BUG-6 (REQ-007) | 1 | - | `PropertyAdmin.Logic/V1/Invoices/InvoiceService.cs`, `PropertyAdmin.Api/Resources/BusinessRuleMessages.nl.resx`; tests in `V1/Invoices/InvoiceServiceTests.cs`, `InvoicesApiTests.cs`, `V1/Reports/ReportsApiTests.cs`, `Characterization/InvoicesReportsTests.cs` | As D-057: due date = date + 14, any other term refused with the localized 400; the request field stays. Kept as built: no change, story points unchanged |
| 2 | B22 | backend-developer-2 | REQ-007 (Q-083 c, REQ-024 criterion 1, D-056) | 1 | T-054 (same file) | `PropertyAdmin.Logic/V1/Invoices/InvoiceService.cs` (`DeleteAsync`), `PropertyAdmin.Api/Resources/BusinessRuleMessages.nl.resx`; `PropertyAdmin.Tests/V1/Invoices/InvoiceServiceTests.cs` | Refuse deleting an open invoice that is not the most recent (highest number of the latest year) with an English key, Dutch in the resx (D-050); the check and the delete in one transaction; numbering stays as it is (D-056 point 1: no sequence, the invoice-number part of T-039 is done by this decision); unit tests: most recent open deleted and its number reused by the next invoice, an older open one refused, a paid one refused |
| 2 | F10 | frontend-developer-2 | REQ-007 (BUG-6, D-057) | 0.25 | - (T-054 is done) | `test application for development team/Pages/Invoices/Invoices.cs` (Create model), `Pages/Invoices/Create.cshtml`, `Resources/SharedResource.nl-NL.resx` and `.en-US.resx`; `PropertyAdmin.Contracts` unchanged | The editable 0..365 payment term input goes. In its place read-only text, no input: "Betalingstermijn: 14 dagen (vervaldatum = factuurdatum + 14 dagen)" / "Payment term: 14 days (due date = invoice date + 14 days)". The page model sends `PaymentTermDays = 14` from one constant (no bound property, so a posted value cannot change it); an API 400 still shows as received. The invoice list and details keep showing the due date; a refused delete shows the API's reason as received (D-049, already through `ApiPageModel`), no new delete logic in the screen |
| 3 | T16 | tester | REQ-007 (D-056, Q-083) | 1 | T-054, B22, F10 | `PropertyAdmin.Tests/V1/Invoices/InvoicesApiTests.cs`, new `PropertyAdmin.Tests/V1/Invoices/InvoiceNumbersPostgreSqlTests.cs` (Testcontainers PostgreSQL 18, D-031, the existing container set-up), `Characterization/InvoicesReportsTests.cs`, new `PropertyAdmin.Tests/Frontend/Invoices/InvoiceCreateTests.cs` (invoice form), `testing.md` (BUG-6 retest) | API: BUG-6 retest on top of T-054's tests: the screen's invoice saves with due date + 14, the API refuses 30 on both routes (D-057); delete rule 204/400/404 on both routes, reason in `nl-NL` and `en-US`. PostgreSQL: five invoices created at the same time get five consecutive numbers without a gap; delete the most recent, the next invoice gets its number; amounts stored as `numeric(18,2)` (REQ-018). Invoice form save without a term in the screen tests |
| 3 | B12 (exists in D-033, no task yet) | backend-developer-2 | REQ-008 (D-033) | 1 | - (own branch; after B22 for the developer's order) | `PropertyAdmin.Api/V1/Reports/GetAssetResults/Request.cs`, new `RequestValidation.cs`; the same two in `V1/Reports/GetOwnerOverview/`; the shared `ValidationMessages` resource of the Api project (D-052, Dutch entries) | `from` and `to` required (nullable in the request, so a missing one is detected), `to` not before `from`, 400 per field, localized by `lang`; Logic unchanged; unit tests of the validation |
| 4 | F11 | frontend-developer-2 | REQ-008 (D-033) | 0.5 | B12 (the contract above; may code at once) | `test application for development team/Pages/Reports.cshtml.cs`, `Pages/Reports.cshtml`, `Resources/SharedResource.nl-NL.resx` and `.en-US.resx` | No silent swap of the dates any more: "to" before "from" gives a field error at "to" without calling the API; an API 400 shows at its field; defaults (1 January to 31 December) stay |
| 5 | T17 | tester | REQ-008 (D-033) | 0.25 | B12, F11 | `PropertyAdmin.Tests/V1/Reports/ReportsApiTests.cs`, `Characterization/InvoicesReportsTests.cs` (`Missing_period_currently_returns_200_with_zero_totals` becomes 400), new `PropertyAdmin.Tests/Frontend/Reports/ReportPeriodTests.cs` | Both report endpoints: `from` missing, `to` missing, invalid, reversed: 400 at the field, in both languages; the screen shows the field error for a reversed period |
| 4 | T-152 (exists; owner to backend-developer-2) | backend-developer-2 | REQ-009 (Q-112 b, Q-113) | 1 | - (own branch) | `PropertyAdmin.Logic/V1/Reports/ReportService.cs` (owner overview only), `PropertyAdmin.Tests/V1/Reports/ReportServiceTests.cs` | As the approach in T-152; touches no file of B12 (B12 changes only the Api project), so B12 and T-152 may also run in parallel if a second backend developer takes one |
| 6 | demo test S4 (T18) | tester | REQ-007 (D-047; rule 15) | 0.5 | T16 for the invoice steps, T17 for the report step | new `PropertyAdmin.Tests/Demo/S4DemoTests.cs`; no change to `DemoStack.cs` (T15 changes it on `change/D-051-code-checks`; when S4 needs a harness change, after #20 is merged) | The S4 demo in `plan.md`, Dutch and English: sign in; `/Invoices/Create` twice: consecutive numbers, due date + 14; deleting the older open invoice is refused with the reason, the most recent is deleted; `/Reports` with "to" before "from": field error. Starts on the REQ-007 branch with the invoice steps (0.25); the report step is added on the REQ-008 branch after REQ-007 is merged (0.25) |
| after Q-115 (a) | B23 | backend-developer-2 | REQ-009 (US-9.1, US-9.4, US-9.5) | 1 | T-152 (same method), Q-115 | `ReportService.cs`, `Models/OwnerOverviewModel.cs`, `PropertyAdmin.Logic/V1/Reports/GetOwnerOverview/Result.cs`, `PropertyAdmin.Api/V1/Reports/GetOwnerOverview/Response.cs`, `Mapping.cs` | Per owner the open invoices (id, number, date, due date, total incl. VAT, overdue = due date before today, D-046); only owners with open invoices; unit tests |
| after Q-115 (a) | F12 | frontend-developer-2 | REQ-009 | 1 | B23 (contract), Q-115 | `PropertyAdmin.Contracts/Dtos.cs` (`OwnerOverviewDto`), `Pages/Reports.cshtml`, resx | The open invoices under each owner with due date and an overdue mark, links to the invoice |
| after Q-115 (a) | T19 | tester | REQ-009 | 0.5 | B23, F12 | `ReportsApiTests.cs`, `Frontend/Reports/ReportPeriodTests.cs` | US-9.1, US-9.4 (overdue with a test clock), US-9.5 |

**S4 total: 6.5 story points** (T-054 1, B22 1, F10 0.25, T16 1, B12 1, F11 0.5, T17 0.25,
T-152 1, demo test 0.5) against 7.5 in `plan.md`: the invoice-number sequence (T-039 part a, 1) is
not built (D-056) and the PostgreSQL set-up exists (T16 1 instead of 2), while F10, F11 and T17
(1) were not estimated before. BUG-6 is settled by D-057 (as built in 17596d4); D-056 point 3 was
amended to follow it (17:40), so F10 needs no `Contracts` change. With Q-115 (a): +2.5, S4 = 9 (question to the user). T-039
drops its invoice-number part: 4 story points left for S6 to S8 (orchestrator: title).

**Parallel work.** The three people run at the same time on different files:
- backend-developer-2: T-054, then B22 (REQ-007), then B12 (REQ-008), then T-152 (REQ-009). B12
  and T-152 touch different projects and may swap order.
- frontend-developer-2: F10 (REQ-007) and F11 (REQ-008) touch no backend or test file. Both add
  entries to `SharedResource.*.resx` on different branches: do them one after the other, and
  before the second pull request merge `main` into its branch (an ordinary merge conflict in the
  resx, no rewrite).
- tester: T16, T17 and the demo test touch only `PropertyAdmin.Tests` and `testing.md`. They
  follow their backend and screen pieces per item. Not on the S3 branch's files: T15
  (`change/D-051-code-checks`) changes `DemoStack.cs` and analyzer findings across the test
  project, so S4 tests leave `DemoStack.cs` alone and expect a merge from `main` after #20.
- Shared with S3: `PropertyAdmin.Tests/Characterization/InvoicesReportsTests.cs` is touched by
  T-054, T16 and T17 in S4 only; no S3 piece touches the invoice or report code. The S3 screen
  regression tests (T-057) work in `Characterization/Screens/`, so the S4 screen tests go in new
  files under `PropertyAdmin.Tests/Frontend/Invoices/` and `Frontend/Reports/`.

**Replaced along the way**
- First architecture: one Razor Pages project with EF Core inside it, Staff pages and an owner
  portal under `/Portal` with cookie sign-in and roles Staff and Owner. Replaced by decisions #1,
  #2, #4 and #5 after the user asked for an API layer and a separate admin frontend (question
  10), multiple roles (question 12) and optional two-factor sign-in (question 13).
- First task list (B1–B3, F1–F3, T1 for the single project): replaced by the task list above.
