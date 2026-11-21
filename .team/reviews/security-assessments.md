## Security assessments

_Owner: security-officer._ Per assessment: date, type (design review, advisory check, full
assessment), scope, and per finding the severity (Critical, High, Medium, Low), location, risk
and required fix. An advisory check is recorded only when it finds an issue that affects this
solution.

**2026-11-20 18:15, code check of the BUG-13 fix against K1-K6 (T-184; T-182, commit 1cf0868 on
fix/BUG-13-profile-signout).** Scope: the diff of 1cf0868 (`AuthService`, `AuthApiTests`,
`AuthServiceTests`), read with `git show` after `git fetch`; `ApiExceptionHandler`, `UserService`
reset-2fa and the reset-2fa tests read in `.worktrees/BUG-13`. Tests not run by me; the commit
reports 507 of 508 passed, the failing one is the characterization test of the old behaviour
(`Characterization/AuthUsersTests.cs`, Q-121 to the tester).
**Verdict: acceptable from a security point of view. K1, K2, K3, K5, K6 met; K4 met in the code,
its test only half (new finding S-37, Low). Nothing Critical or High; nothing blocks the pull
request.**
- Difference from D-058, acceptable: `IUserStore<ApplicationUser>` cast to
  `IUserAuthenticatorKeyStore<ApplicationUser>` is the same scoped store as `UserManager`'s; it
  only writes the key token and `UpdateAsync` leaves `SecurityStamp` alone; still own-account.
- K1, K2, K3, K5, K6 met (Identity key generator; S-18 check first, existing key unchanged; key
  stored and `IdentityResult` checked, parallel first visits get the generic 500; tests; no
  logging). K4 met in the code (`ResetAuthenticatorKeyAsync` in `2fa/disable` line 106 and Admin
  reset-2fa, UserService line 66); `2fa/disable` test asserts the stamp, reset-2fa not (S-37).
- **S-37 (Low), new.** Location: `PropertyAdmin.Tests/V1/Users/UserServiceTests.cs` around line
  104 and `UsersApiTests.cs` line 48. Risk: the stamp assertion after reset-2fa comes only after a
  forced sign-out, which renews the stamp itself, so a later change that moves reset-2fa off
  `ResetAuthenticatorKeyAsync` (as BUG-13 did for the first key) would keep old sessions alive
  after an Admin reset without any test failing. Fix (tester): assert that the stamp differs (or
  the user's token is refused with 401) directly after reset-2fa, before any sign-out, on
  `/api/v1/users/{id}/reset-2fa`.

**2026-11-20, design review of D-058 (BUG-13: GET auth/2fa creates a missing authenticator key
without changing the security stamp; T-184).** Scope: D-058, `AuthService.GetTwoFactorAsync`,
`EnableTwoFactorAsync`, `DisableTwoFactorAsync`, `UserService` reset-2fa, and the stamp check in
`PropertyAdmin.Api/Program.cs`, read in `.worktrees/BUG-13` at 4a2f453 (= origin/main). No T-182
commits or uncommitted changes there yet, so the code check of the pull request is still to come.
**Verdict: D-058 is acceptable from a security point of view, with conditions K1-K6. No new
finding; nothing Critical or High.**
- Why: the API compares only `SecurityStamp` with the token's `sstamp` claim (Program.cs line
  57); a first key while two-factor sign-in is off is no credential yet, so nothing to revoke.
  Turning two-factor on, the Admin reset and `2fa/disable` keep their stamp change. The endpoint
  stays own-account (Q-058); no authorization change.
- K1-K6 (Identity key generator; only the empty-key branch, S-18 check first; key stored and
  result checked before it is returned; `ResetAuthenticatorKeyAsync` kept in reset-2fa and
  `2fa/disable` with stamp tests; stamp, storage and second-GET tests; key never logged): checked
  in the 18:15 entry above, all met except the reset-2fa stamp test (S-37).
- Note for the orchestrator (not security): the worktree branch is `fix/BUG-13-profile-signout`,
  while D-058 says `bug/BUG-13-profile-sign-out` and project.md `bug/BUG-n-...`.

**2026-11-20, review of pull request #19 (REQ-003), asset owner change with lines (T-143, BUG-4,
b233cf5).** Scope: b233cf5 and, read only on origin/feature/REQ-003-manage-assets, the owner scope
of `AssetService`, `ActivityService`, `ExpenseService`, `InvoiceService` (list, by id,
`GetPdf/Handler.cs`), `ReportService` and the policies of the Activities, Expenses and Reports
controllers. Question: after a move, a ReadOwn user of the new owner sees the asset's invoiced
history, and the per-owner overview credits earlier income to the new owner; acceptable under
REQ-019 and REQ-009? Q-112 (to the user) is judged only on its security side.
**Verdict: acceptable from a security point of view. No new finding, nothing blocks #19; no open
Critical or High.**
- Access control unchanged by b233cf5 (it only removes the refusal when uninvoiced lines exist).
  Every read stays filtered server-side by owner: assets and their activities and expenses by
  `Asset.OwnerId`, invoices by `Invoice.OwnerId` on list, by id and PDF (REQ-019).
- The new owner's ReadOwn user sees the asset's earlier lines (work history, no personal data of
  the old owner); the old owner's invoice stays out of reach (404 by id and PDF). The per-owner
  overview needs the Reports permission, which ReadOwn cannot hold (S-28), so the crediting of
  earlier income is a functional question (Q-112), not a disclosure. S-32 unchanged.
- Condition when the user picks Q-112 option b (owner at the time per line): the grouping must
  not widen any owner scope: ReadOwn reads keep filtering by the current `Asset.OwnerId` (or are
  changed only by a reviewed decision), and a test shows the old and the new owner each get only
  their own data. Note for the REQ-010 design review (later): decide then whether an owner may see
  the lines of a period in which the asset was not theirs.

**2026-11-20, design review of D-047 under rule 17 (T-115): sprint demo test with
Testcontainers.PostgreSql.** Scope: D-047, D-031, Q-104, `PropertyAdmin.Tests.csproj` (no
Testcontainers reference yet), `docker-compose.yml`; package facts from nuget.org (the `.nuspec`
files of 4.17.0 and its dependencies), the GitHub release page and the GitHub advisory database,
read on 2026-11-20. No code exists yet, nothing was installed; `dotnet list package --vulnerable`
cannot show the new package before it is added (condition C2).
**Verdict: D-047 is acceptable from a security point of view, with conditions C1-C7. No finding;
nothing blocks.** The user's OK (Q-104) is still needed before the package is added.
- Package: Testcontainers.PostgreSql 4.17.0 (released 2026-11-16) on Testcontainers 4.17.0, MIT,
  testcontainers-dotnet on GitHub (active monthly releases, reserved prefix, signed release with
  provenance); free (REQ-016). Dependencies: Docker.DotNet.Enhanced(.X509) 4.3.3, SSH.NET
  2026.0.0, SharpZipLib 1.4.2, BouncyCastle.Cryptography 2.7.0,
  Microsoft.Extensions.Logging.Abstractions 8.0.3.
- Known vulnerabilities: none in 4.17.0's tree. SSH.NET GHSA-mggc-4xg6-vcxf (CVE-2026-85756) and
  GHSA-q939-rpr3-3284 (CVE-2026-48798), both High in `ScpClient`, are fixed in 2026.0.0; the
  SharpZipLib path-traversal advisories are fixed in 1.3.3 or earlier. Note: SharpZipLib 1.4.2
  (2023) is slow-moving; acceptable, it only builds tar archives for test containers.
- Exposure: test-only on the laptop (D-039); drives Docker Desktop through the local engine pipe
  (same trust as `docker compose`), starts `testcontainers/ryuk` with the engine socket, pulls
  from Docker Hub; nothing reaches the production images.
- Conditions:
  - C1: reference `Testcontainers.PostgreSql` with the exact version 4.17.0 only in
    `PropertyAdmin.Tests.csproj`; never in the API, Logic, Data, Contracts or frontend projects.
  - C2: after adding it, run `dotnet list package --vulnerable --include-transitive` on the test
    project and record the result in the task report; any vulnerable package blocks the change.
  - C3: the PostgreSQL image is the same tag as `docker-compose.yml` (`postgres:18-trixie`, D-015),
    never `latest`.
  - C4: no fixed password anywhere: the database password and the seed admin password are
    generated per run in the test (for example `RandomNumberGenerator`), handed to the hosts
    through in-memory configuration, never written to `appsettings*.json`, `.env` or a log.
  - C5: the demo test uses only its own throwaway container with fictitious data; never the
    `propertyadmin` volume or the user's running compose stack.
  - C6 (rule 12): the demo test signs in through `/Account/Login` with the real API token
    validation; no test authentication handler, no bypass, no broader permission in the demo
    harness. The seed admin is the only account, as in production.
  - C7: keep the Testcontainers resource reaper on (do not set `TESTCONTAINERS_RYUK_DISABLED`),
    so no database container with test data stays behind after a run.

**2026-11-20, S2 code review of the REQ-002 changes merged in #13 (T-108).** Scope: commits
`8772675`, `9d12abb`, `237444f`, `fdf2c27`, `ee9901f` (merge `c00cc51` on `origin/main`), read only
with `git show` and `git grep`: V1 Owners requests, validation, mappings and responses,
`OwnerNumbers`, `OwnerService`, `Entities.cs`, `AppDbContext.cs`, the migration
`20261120140829_OwnerAddressFields`, `OwnersController.cs`, the invoice PDF owner block, the
frontend `ApiClient`, `ApiPageModel`, owner pages and Contracts DTOs, `docker-compose.yml` and
`.env.example`. Tests and the package audit were not run (no package change in these commits).
**Verdict: no new finding; no open Critical or High; the S2 release is not blocked by security.**
- Checked and correct: `OwnersController` policies unchanged on `/api/v1/owners` and `/api/owners`
  (reads `Policies.Read` with `ReadScopeOwnerId()`, writes `Permissions.OwnersAssets`); requests
  separate from `OwnerSaveModel` (`Id`, `InactiveSince` not postable); KvK and BTW validated on
  the normalized value with anchored, linear `GeneratedRegex`; 400 Problem Details, frontend shows
  only `OwnerInput` field errors through encoded `ModelState`; pages and PDF encode output; the
  migration's raw SQL is static; `TZ` required, no secret; no logging added.
- Personal data (REQ-024): street, house number, country, KvK and BTW are new personal data of
  owners (a BTW number of a sole proprietor identifies a person). Not a finding now; the REQ-024
  anonymization design must include all these fields.
- Still open from earlier assessments: S-20 (Medium, T-039); Lows S-22, S-27 (T-045), S-30
  (T-047), S-32 (T-079/T-080), S-36 (T-091). None is Critical or High.

**2026-11-20, design and code review of D-045 and BUG-3 (T-105).** Scope: D-045, REQ-024, and
commits `a835eb3` and `949d149` on `origin/bug/BUG-3-owner-soft-delete`, read only with `git show`
and `git grep` (`OwnerService.cs`, `Entities.cs`, `DependencyInjection.cs`, the migration
`20261111075401_OwnerInactiveSince`, and `V1/Owners/OwnersController.cs` with its request and
response models on the branch). Tests and the package audit were not run (no package change).
**Verdict: D-045 is acceptable from a security point of view; no new finding, no Critical or
High; this change does not block a release.**
- Checked and correct: no new endpoint or policy change (delete on both routes needs
  `Permissions.OwnersAssets`; reads keep `ReadScopeOwnerId()`, also for inactive owners);
  `InactiveSince` not postable, set only by `DeleteAsync` via `TimeProvider`, not in responses;
  the refusal for an owner with assets is a fixed-text 400 Problem Details; the migration only
  adds a nullable `date` column; no logging added. Keeping the row is what REQ-024 asks; inactive
  owners stay readable by id for staff until REQ-024 anonymization is built (not a finding).
- Observations for the architect and tester (functional, not security): the code does not stop an
  inactive owner from being edited, from getting a new asset (asset create or move), or from being
  invoiced; and the date is the server's local date (UTC in the container), so a delete just after
  midnight Dutch time records the day before. When a later anonymization relies on "inactive and
  no assets", an asset added to an inactive owner would be anonymized with it; REQ-024's design
  should decide this.

**2026-11-20, full assessment before the S1 release (T-100, weekly; previous full one T-020).**
Scope: all code on `origin/main` `f3542bc`, read only with `git show` and `git grep` (the local
working folder is on an older branch and was not used for reading): API `Program.cs`, `Policies`,
token validation, every controller and its policy (V1 Owners, Assets, Expenses, Activities,
Invoices, Reports, VatRates, Users, Roles, Auth, with their legacy `/api` aliases), the owner scope
through the Logic layer (including the Expenses and Activities slices that T-020 left out), the
frontend `Program.cs` (cookie, antiforgery, forwarded headers, security headers, folder
authorization), `docker-compose.yml`, `Caddyfile`, `.env.example`, `.dockerignore`, the API
Dockerfile and the configuration files, and a search of the code and of the git history since
2026-11-10 for secrets. The Logic and Data handlers were not reread line by line in this run:
every change to them since T-020 was reviewed in T-077, T-083, T-086, T-090 and T-098. Package
audit, read only, `dotnet list <project> package --vulnerable --include-transitive` per project:
no vulnerable package in any of the six projects (Api, Logic, Data, Contracts, frontend, Tests;
the restore matches `f3542bc`: the only project-file difference is comments in the test
project). Tests were not run. **Verdict: no open Critical or High finding; this assessment does
not block the S1 release. No new finding.**
- Checked and correct: fallback policy in API and frontend, only `auth/login`, `auth/login-2fa`
  and `/Account` anonymous; matrix policies on writes, Users and Roles, Reports; every read passes
  `ReadScopeOwnerId()` from the token down to the Logic queries; standard JSON Web Token
  validation plus purpose, stamp and lockout on every request; cookie HttpOnly, SameSite Strict,
  Secure, no sliding; token never in the browser; request records with limits and
  `[EnumDataType]`; no internal fields in responses; one Problem Details handler, path-only logs;
  no raw SQL; no secrets in code, configuration or history since T-020; `.env` ignored; only the
  proxy publishes ports; forwarded headers trusted only from 172.30.0.10; non-root API image.
- Supported versions (REQ-028): .NET 10 (long-term support), PostgreSQL 18, Caddy 2, Ubuntu
  26.04 images are all in support for the expected life of the solution. Note, no finding: the
  `postgres:18-trixie` and `caddy:2` tags float, so a rebuild picks up patches (wanted) but is
  not reproducible.
- Still open: S-20 (Medium; D-024, T-039: the API connects as the PostgreSQL superuser
  `propertyadmin`, the `POSTGRES_USER` of the container, and migrates at start-up; checked again
  in `docker-compose.yml` and API `Program.cs` on `f3542bc`). Medium does not block a release, but
  T-020 asked for it before the first production use; T-039 is planned for a later sprint, and
  the T-098 list of open findings left S-20 out by mistake. Low, none blocks release: S-21
  (T-039, lists not paged), S-22 and S-27 (T-045), S-30 (T-047), S-32 (T-079, T-080), S-36
  (T-091). S-10 stays accepted until T-058 (D-034) is built.

**2026-11-20, S1 release review of the changes on main since T-090 (T-098).** Scope: commits
`2aec022` (S-33), `407930b` (S-34), `63f469d` (S-35), `2e098bd` (BUG-8), `dcc52c6` (BUG-9) and the
merge `f3542bc` (conflict in the frontend `Program.cs`), plus the frontend `Program.cs` and the enum
types on `origin/main`; read only with `git show`/`git grep`. Tests were read, not run (the commits
report 216 to 219 passed). No package or project file changed, so no new dependency to audit; no
audit command was run. This is a review of the changes, not a new full assessment: the last full
assessment is T-020 (2026-11-10). **Verdict for the sprint 1 release: open Critical or High: no.
S-33, S-34 and S-35 are closed. No new finding. Nothing here blocks the release.**
- S-33, S-34, S-35 fixed and re-checked (headers set in `Response.OnStarting`, 500 test in a
  Production test host; `[EnumDataType]` on all four API enums with tests on both routes; host
  test of the forwarded-header chain with the real login form). BUG-8 and BUG-9: no security
  impact (read-only cultures from the cookie and the configured list; middleware order unchanged).
- Still open, all Low, none blocks release: S-22 and S-27 (T-045), S-30 (T-047), S-32 (T-079, T-080)
  and S-36 (T-091, deployment, reported as built, not verified, D-039).

**2026-11-20, review of D-044 (sign-in rate limit) and re-check of S-17 (T-090).** Scope: commit
`fd39207` on `origin/feature/REQ-001-signin-security` (`Services/SignInRateLimiter.cs`, `Login` and
`LoginTwoFactor` page models, `Program.cs`, resources, `SignInRateLimitTests`), plus `Program.cs`,
`docker-compose.yml` and `Caddyfile` on that branch; read only with `git show`/`git grep`. Tests were
read, not run (the commit reports 172 passed). **Verdict: D-044 is acceptable; S-17 is closed. No
Critical or High finding; nothing here blocks release. Two new Low findings (S-35, S-36).**
- S-17 fixed and re-checked (built-in fixed-window limiter, 10 per 60 s per client IP and step,
  checked before validation and the API call, generic localized 429, API lock-out stays; 4
  tests). Client IP behind Caddy correct: forwarded headers trusted only from 172.30.0.10, `web`
  unpublished, `ForwardLimit` 1, Caddy replaces client-sent `X-Forwarded-For`.
- S-35 (Low, new; fixed and re-checked in T-098): no test of the forwarded-header chain.
- S-36 (Low, new, deployment; production-only, D-039) Docker published ports 80/443 of `proxy`:
  when Docker's userland proxy carries a connection (IPv6 clients on a host without Docker IPv6, or
  Docker Desktop on the laptop), Caddy sees the Docker gateway address for every such client, so
  they all share one bucket and one attacker can keep the sign-in at 429 for all of them. Risk:
  denial of sign-in only, no bypass. Fix: the deployment README (REQ-031) says to check after
  start-up that the Caddy access log shows the real client addresses, and to enable Docker IPv6 or
  serve IPv4 only when it does not. Reported as built, not verified.
- Residual risk accepted by D-044 (no finding): a per-IP limit does not stop spraying from many
  addresses (botnet, IPv6 address rotation within one /64); the per-account lock-out remains the
  second line.

**2026-11-20, design review of D-043 (BUG-7: enums as numbers; unreadable bodies without
internal details) (T-086).** Scope: D-043; `PropertyAdmin.Api/Program.cs`, `V1/Enums.cs`, the V1
Assets and Activities requests and mappings, `ActivityService.CalculateAmount` and the frontend
`Services/ApiClient.cs` on main; read only, without git commands, the work in progress in
`.worktrees/BUG-7` (`Program.cs` line 75 `AddJsonOptions(o => o.AllowInputFormatterExceptionMessages = false)`,
`PropertyAdmin.Tests/V1/Assets/AssetsUnreadableBodyTests.cs`; there `ApiClient` still has
`JsonStringEnumConverter`, so the frontend part is not done yet). Nothing was run. **Verdict:
D-043 is acceptable; no Critical or High finding; nothing here blocks release. One new Low
finding (S-34), from before D-043, that D-043 makes more relevant.**
- D-043 checked and correct: with `AllowInputFormatterExceptionMessages` off the 400 Problem
  Details shows only generic texts under the JSON path, no type name, exception text or input
  value, on `/api` and `/api/v1` (worktree test); the formatter logs at Debug without values;
  numeric enums keep the contract; no authorization change.
- S-34 (Low, new; fixed and re-checked in T-098): undefined enum numbers were accepted for
  `AssetType` and `ActivityKind`.

**2026-11-20, review of D-042 and re-check of the T-046 UI fixes for S-19, S-26 and S-25 (T-083).**
Scope: commits `09f740c` (S-19 UI), `bad80b2` (S-26 UI) and `3bb3f62` (S-25, D-042) on
`origin/feature/REQ-001-signin-security`, read only with `git show`, plus `_Layout.cshtml` on that
branch. Tests were read, not run (the commits report 168 passed). S-17 (sign-in rate limit) is not
done yet and was not checked. **Verdict: no Critical or High finding; nothing here blocks release.
S-19 and S-26 are closed; S-25 is closed; D-042 is acceptable with one condition (S-33, Low).**
- S-19 (UI part, L9), S-26 (UI part) and S-25 fixed and re-checked: the profile page asks for
  the password or a code, one generic error, success signs out (4 tests); sign-out calls
  `POST api/auth/logout` before the cookie goes, also on errors and timeouts, logs no user data
  (4 tests); `SecurityHeadersMiddleware` sets a strict Content-Security-Policy without
  `unsafe-inline`, `nosniff`, `Referrer-Policy` and `X-Frame-Options: DENY`, all inline script
  moved to `site.js`, a test scans every page for inline script. D-042 acceptable.
- S-33 (Low, new; fixed and re-checked in T-098): the production 500 page lost the security
  headers (`Response.Clear()` of the exception handler).

**2026-11-20, re-check of the T-044 fixes (T-077).** Scope: commits `03b89f3`, `57f3f83`,
`1f18f26`, `8d77523`, `ea5d5bf`, `f72d7b5` on `origin/main` (merged in `ba46941`), read only with
`git show`, plus `UserService.cs` on `origin/main`, against S-18, S-19, S-23, S-24, S-26, S-28 and
conditions L1 to L9 of the D-036 review. Tests were read, not run (the commits report 153 passed).
**Verdict: no Critical or High finding; nothing here blocks release. S-18, S-23, S-24 and S-28
(role level) are closed; the API parts of S-19 and S-26 are correct, both findings stay open
until the frontend part (T-046) is done; one new Low finding S-32.**
- S-18, S-23, S-24 and S-28 (role level) fixed and re-checked; the API parts of S-19 and S-26
  correct (closed in T-083 with the UI). Conditions L1-L7 met (logout on the fallback policy
  renews the stamp, both tokens 401 afterwards; disable with password or authenticator code, one
  message, failures counted; dummy-hash check for unknown and locked-out users; `MaxLength` on
  both routes; role create and update refuse `ReadOwn` with other or unknown permissions).
- L8 not met: there is no D-028 endpoint enumeration test on `origin/main` yet, so nothing lists
  `auth/logout` as an own-account endpoint. Not a T-044 defect; it belongs to the tester's D-028
  work, which must include it.
- S-32 (Low, new) `PropertyAdmin.Logic/V1/Users/UserService.cs` (`CreateAsync`, `UpdateAsync`,
  `ValidateRolesAndOwnerAsync`). Risk: a user may hold several roles (REQ-012), so an admin can
  give one user a `ReadOwn` role and a role with write, `Invoices` or `ReadAll` permissions; that
  user then escapes the owner isolation that S-28 meant to keep. Needs an admin's mistake and the
  owner portal is Won't (REQ-010), so the risk is low. Fix: refuse (400) a user whose roles
  together hold `ReadOwn` with any other permission, on user create and update, and on role
  update when a user holding that role also holds a `ReadOwn` role; a test per case. Owner:
  backend-developer-1.

**2026-11-11, design and code review, D-040 (T-073).** Scope: D-040 against REQ-025 and D-023;
commit `d61d3f4` on `feature/REQ-017-laptop-compose` (`.worktrees/REQ-017`; changes only
`docker-compose.yml` and `.env.example`, checked with `git show --stat`), read with the
unchanged `Caddyfile`. **Verdict: D-040 is acceptable from a security point of view; no
Critical, High or Medium finding; it does not block release.**
- REQ-025 criterion 1 holds: the proxy is still the only service with `ports:`; `web`, `api`,
  `db` and `backup` publish none. `${HTTP_PORT:-80}` also falls back to 80 when the variable is
  set but empty (`HTTP_PORT=` as in `.env.example`), so production gets 80 and 443 unchanged.
- Criterion 2 holds: Caddy still listens on 80 inside the container and redirects every HTTP
  request to `https://{PUBLIC_HOST}` on 443, whatever the host port; only the published host
  port changes. Criteria 3 and 4 are unaffected: no network, `KnownProxies` or proxy-address
  change (`172.30.0.10`).
- No secrets in the commit, its message or `.env.example`; `HTTP_PORT` is operator
  configuration in `.env`, not user input.
- S-31 (Low) `.env.example`, `docker-compose.yml` comment. Risk: an operator copies the laptop
  value `HTTP_PORT=8080` to production. Then `http://<host>` (port 80) is not served, so
  criterion 2 (redirect) fails, and the Let's Encrypt HTTP-01 challenge fails (Caddy falls back
  to TLS-ALPN-01 on 443, so the certificate itself still works). Fix: in `.env.example` say
  that production leaves `HTTP_PORT` empty (port 80 is required there for REQ-025), and put the
  same line in the deployment README (REQ-031, S10). No separate task needed now: add it to the
  README task.
- Note (informational, not a finding): on the laptop Docker Desktop publishes 8080 and 443 on
  all interfaces, so others on the same network can reach the sign-in page while it runs. This
  was already so for 80 and 443 before D-040; the laptop holds only fictitious data (D-039).

**2026-11-10, design review D-036 and code review of T-044 so far (Q-060).** Scope: D-036,
and `.worktrees/REQ-001` on `feature/REQ-001-signin-security` as found at 17:10 (read only).
**State of the code:** the branch has no T-044 commit (HEAD `be69655`, the T-044 report is
empty); only the S-18 change is staged, uncommitted. S-19, S-23, S-24, S-26 and S-28, and
`POST auth/logout`, are not in the code yet, so they could not be reviewed: they stay open.
**Verdict: D-036 is acceptable from a security point of view with conditions L1 to L9 below;
`POST auth/logout` may stay on the fallback policy as an own-account endpoint (same reasoning
as the Q-058 addendum). No Critical or High finding.**
- S-18 (staged then): correct; closed in T-077.
- L1-L7 and L9 (logout, 2fa/disable, dummy hash, length limits, `ReadOwn` alone in a role, UI
  until T-046): met and re-checked in T-077 and T-083.
- L8 The D-028 endpoint enumeration test lists `auth/logout` (both routes) as an own-account
  endpoint (fallback policy), so a later change of its policy is noticed.

**2026-11-10, design review, D-034 (Q-057).** Scope: D-034, the authentication part of
`architecture.md`, REQ-012, and the code it changes as checked out on `change/D-026-v1-layout`
(read only): `PropertyAdmin.Api/Program.cs` (`OnTokenValidated`), `Auth/Policies.cs`,
`Auth/TokenService.cs`, the old `RolesController` in `Controllers/UsersController.cs`,
`Logic/V1/Users/UserService.cs`. **Verdict: D-034 is acceptable from a security point of view,
with the conditions C1 to C6 below; no Critical or High finding.** It closes the 8-hour window
of S-10 for role permissions and fails closed. The four points of Q-057 hold: a cache only with
invalidation on every role create, update and delete and no time window; fail closed; the menu
stays on the token claims (display only; the API enforces); S-28 still applies to role create
and update (V1 Roles slice, D-035, and the old `api/roles` until T-036 removes it).
Conditions for T-058 (owner backend-developer-1; the tests prove each one):
- C1 (Medium if missed) Read the user's roles from the database by user id (`AspNetUserRoles`,
  role ids), never from the token's role-name claims. Otherwise a role renamed after sign-in
  and a new role created with the old name gives the old token the new role's permissions.
  Test: rename a role, create a new role with the old name and other permissions; the old
  token gets none of them.
- C2 (Medium if missed) Clear the cache after the transaction commits, not before: cleared
  before the commit, a request in between fills the cache again with the old permissions and
  keeps them with no end. Key the cache by role id (a rename keeps its entry correct). Both
  the old `RolesController` and the V1 Roles slice (D-035) clear it; the seeder runs before
  any request.
- C3 Remove every permission claim of the token (all of type `permission`) before adding the
  current ones, on the principal the policies and `ReadScopeOwnerId()` use. Replace the
  owner-id claim with `OwnerId` of the loaded user as well (today an owner change updates the
  stamp, so this is defence in depth).
- C4 Fail closed: a failed read is not caught to carry on with the token claims; it ends in
  500 (D-034; 401 is also acceptable), with a general Problem Details message and no exception
  detail in the response or log text with personal data.
- C5 A user whose roles give no permissions gets 403 on every permission endpoint, also when
  the token still lists permissions.
- C6 Changes made straight in the database bypass the cache: write in the deployment notes that
  the API is restarted after such a change (or use no cache). Scaling out to more API
  instances needs a new decision (D-034 already says so).
S-10 is updated below. No new task needed: T-058 covers C1 to C5 and C6 is a line in its
deployment note.
C5 scope (Q-058, 2026-11-10): C5 covers only endpoints with a permission policy. The
own-account endpoints (`auth/2fa`, `auth/2fa/enable`, `auth/2fa/disable`, `auth/password`, old
and `/api/v1` routes) stay on the fallback policy, also for a user without permissions: they
act only on the caller's own account (user id from the token, checked in `AuthController`),
expose no business data, and refusing them would stop a user from securing their own account.
Cutting a user off completely is a lockout or forced sign-out (security stamp, 401 on every
endpoint). Kept: the stamp, lockout and D-034 permission reload run on these endpoints too (no
skip path; C4 applies); the user id only from the token; `auth/password` requires the current
password; S-19 (re-authentication for `2fa/disable`) stays open. Any new endpoint without a
permission policy needs a listing in architecture.md and a security review (absolute rule 12).
Tests in T-058: a user without permissions gets 403 on every policy endpoint (the D-028
enumeration) and 200/204 on the own-account endpoints; a locked-out user gets 401 on them.

**2026-11-10, full assessment (T-020, skill update 2026-11-09).** Scope: all code as checked
out on `change/D-026-v1-layout` at 2026-11-10 (T-034 in progress at the same time, so the
Expenses and Activities V1 slices are not part of it): `PropertyAdmin.Api` (old `Controllers/`
and the V1 Owners, Assets, VatRates slices), `PropertyAdmin.Logic`, `PropertyAdmin.Data`,
`PropertyAdmin.Contracts`, the admin frontend, `docker-compose.yml`, `Caddyfile`, both
Dockerfiles, `deploy/backup/backup.sh`, configuration files and the git history. Audits run
read-only: `dotnet list package --vulnerable --include-transitive`: no vulnerable package in any
of the six projects; `--deprecated`: only xunit 2.9.3 in the test project (S-29). Git history
(`git log -G` on secret settings): no secrets committed; only fictitious test credentials.
**Verdict: no Critical or High finding; this assessment does not block a release.** S-17 to
S-20 (Medium) should be fixed before the first production use.

Authentication and authorization (checked first): every endpoint requires authentication
(fallback policy in API and frontend); only `login` and `login-2fa` and the frontend
`/Account` folder are anonymous, as designed. Writes use the matrix policies; reads use
`Read` with `ReadScopeOwnerId()` from the token claim, never from the route (S-1), in old and
V1 code, with owner-isolation tests (`MasterDataTests`, `TransactionTests`,
`InvoicesReportsTests`, `V1/MasterDataApiTests`). Tokens are validated with the standard
library (issuer, audience, signature, lifetime, 30 s skew), a `purpose` claim and a separate
audience keep challenge tokens out, and every request re-checks security stamp and lockout.
No client reaches the database except through the API. Requests bind to DTOs, never to
entities; responses expose no hashes or secrets. Errors: one `IExceptionHandler` with Problem
Details; a 500 carries a general message only; no raw SQL, no sensitive-data logging, logs
hold the path only.

Earlier findings re-checked: S-1, S-2, S-3, S-5, S-6, S-7, S-8, S-9, S-11, S-14 (jQuery 3.7.1,
Validation 1.22.1, Bootstrap 5.3.8, all from `wwwroot/lib`), S-15 (`KnownProxies` from
configuration, 172.30.0.10) and S-16 (Caddy, D-023) are fixed. S-10 stays accepted. S-4 is
fixed except S-27. S-12 is fixed in the code; its test is missing (S-30). S-13: no advisory
for PDFsharp-MigraDoc 6.2.4 in the NuGet audit; keep watching.

- S-17, S-18, S-19 (Medium) and S-23, S-24, S-25, S-26, S-28 (Low): fixed and re-checked in
  T-077, T-083 and T-090 (sign-in rate limit; 2FA key hidden once on; re-authentication for
  `2fa/disable`; timing-safe login; length limits; security headers; logout revokes the token;
  `ReadOwn` alone in a role). S-29 (Low, xunit 2.9.3 deprecated): dropped by the user (Q-062).
- S-20 (Medium) The API connects as the PostgreSQL superuser and migrates at start-up.
  `docker-compose.yml` (`ConnectionStrings__Default` uses `POSTGRES_USER` `propertyadmin`, the
  image's superuser; `backup` uses the same account and password) and
  `PropertyAdmin.Api/Program.cs` (`MigrateAsync` at start-up). Risk: any flaw in the API gives
  full control of the database server (DDL, roles, files through `COPY`). Fix: D-024 as
  designed (`propertyadmin_migrator`, `propertyadmin_app` with DML only, migrations as a
  separate step, own password per role). Already planned in T-039; no new task needed.
- S-21 (Low) Lists and reports are not paged and the report period is unbounded (all list
  endpoints, `ReportService`). Risk: an authenticated user can make the API load all rows; small
  at the expected volumes (REQ-022). Fix: paging as D-024 (T-039) and a maximum report period
  (for example 5 years, 400 otherwise). Owner: backend-developer-1 (T-039).
- S-22 (Low) VAT rate reads have no read policy: `GET api/vatrates` and `api/vatrates/{id}` in
  `Controllers/MasterDataControllers.cs` and `V1/VatRates/VatRatesController.cs` only get the
  fallback policy, so a signed-in user without any permission can read them. Risk: low (no
  personal data), but it breaks deny by default and the matrix. Fix:
  `[Authorize(Policy = Policies.Read)]` on both reads in V1 (the old controller goes in T-036).
  Owner: backend-developer-1.
- S-27 (Low) `SEED_ADMIN_PASSWORD` is required on every start (`${SEED_ADMIN_PASSWORD:?...}` in
  `docker-compose.yml`), so the first admin password stays in `.env` and in the container
  environment (`docker inspect`) for good, and nothing forces it to be changed. Fix: make it
  optional once users exist (`:-`; the seeder already needs it only when no user exists),
  and state in the deployment notes: remove it from `.env` and change the password after the
  first sign-in. Owner: backend-developer-1.
- S-30 (Low) No test for S-12: nothing checks that `/openapi/v1.json` and `/swagger` return 404
  outside Development (`ApiFactory` runs as "Testing"). Fix: one integration test in
  Production environment. Owner: tester.

Notes, no fix required now: the old unversioned controllers still serve the same features
next to V1 (`api/owners` and `api/v1/owners`) until T-036 and T-037; both carry the same
policies today, and every security fix must go into both until the old ones are removed.
Traffic from `web` to `api` is plain HTTP on the internal compose network and carries the
JWT: accepted under D-023 (one host, no published API port). Backups are unencrypted dumps on
the same host: accepted risk (Q-039).

**2026-11-06, design review, first architecture.** No Critical findings.
- S-1 (High) Owner data isolation: every owner query must filter by OwnerId from the user's
  claims, never from a route/query value; integration test required. Covered in REQ-19 and T1.
  (Written for the portal; with decisions #1 and #2 it applies to the API.)
- S-2 (Medium) No self-registration; disable the Identity register page. Covered.
- S-3 (Medium) Antiforgery on all POSTs (Razor Pages default), HTTPS redirection, HSTS outside
  development.
- S-4 (Medium) Secrets via environment variables; the default seed admin password must be changed
  or supplied via an environment variable.
- S-5 (Low) Lockout after failed sign-ins (Identity default on). Password policy default.
- S-6 (Low) PDF generated server-side from stored data only; no user HTML rendered.

**2026-11-06, design re-review, revision 2 (API layer).**
- S-7 (High) JWT signing key from an environment variable, at least 256 bits, short expiry
  (8 h); the API validates issuer, audience and lifetime.
- S-8 (Medium) The API is not exposed publicly in compose except for development; CORS closed
  (server-to-server calls only).

**2026-11-06, design review, revision 3 (authentication and authorization).**
- S-9 (Medium) Two-factor challenge token valid 5 minutes, single use.
- S-10 (Low) Role changes take effect at the next sign-in (max 8 h): accepted; an Admin can force
  sign-out through the security stamp.
  Update 2026-11-10 (Q-057, D-034): once D-034 is accepted and T-058 is built (with C1 to C6 of
  the D-034 review), a change to a role's permissions takes effect at its members' next request
  and the 8-hour window no longer applies. A change to a user's own roles, lockout, forced
  sign-out and a two-factor reset already end the token at the next request (security stamp).
  What remains accepted: the frontend menu shows the token's permissions until the next sign-in
  (display only). Until T-058 is built, S-10 stays accepted as written.

**2026-11-06, design review, technology versions (decisions #12 to #20, task 5).** Scope: the
decisions, the trust boundaries of the diagram, and the files the outside upgrade already changed
(project files, Dockerfiles, both `Program.cs`, `docker-compose.yml`). Audit run read-only:
`dotnet list package --vulnerable --include-transitive` reports no vulnerable package in any of
the four projects. Verdict: #12 to #20 are acceptable from a security point of view, with S-11 to
S-16 below. No Critical or High finding.
- #13, #14, #16 and #12, #15, #17 to #20: no objection. The frontend's `ReadJwtToken` without
  validation is acceptable only because it is used for display and expiry, never for
  authorization; the API validates every request (S-7).
- S-11 (Medium, floating image tags), S-14 (Low, client libraries only from `wwwroot/lib`),
  S-15 (Medium, forwarded headers trusted only from known proxies) and S-16 (Medium, no TLS;
  Caddy, D-023): fixed and re-checked in T-020. S-12 (Low, OpenAPI and Swagger only in
  Development): fixed in the code, its test is open as S-30.
- S-13 (Low) PDFsharp-MigraDoc 6.2.4 may get no more security fixes. Risk is low (PDFs are built
  from stored data only, S-6). The scheduled advisory check watches it; a published vulnerability
  without a 6.2.x fix triggers a replacement decision. Owner: security-officer.

**Full assessment of the implementation:** done 2026-11-10 (T-020), see the top of this file.
