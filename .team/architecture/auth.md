# Authentication and authorization

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged)._

(decisions #4, #5, #6; D-034 replaces the last clause of #5)
- Local accounts with ASP.NET Core Identity; no self-registration; Entra ID later.
- The API uses JWT bearer tokens: signing key from an environment variable, at least 256 bits,
  8 h expiry; the API validates issuer, audience and lifetime (S-7). The admin frontend signs in
  with a cookie that holds the JWT server-side and forwards it.
- Two-factor sign-in (authenticator app) is optional per user: enabled on the profile page (QR
  code and recovery codes). An Admin can see whether it is on and reset it. When it is on, login
  has two steps: `/api/auth/login` returns `requires2fa` and a challenge token (valid 5 min,
  single use, S-9), then `/api/auth/login-2fa` with the code returns the JWT.
- A user can have several roles. Permissions are policies in the API; roles live in Identity
  tables and are seeded at start-up. Policies, not role names, are used on endpoints, so the
  matrix can change without code changes on the pages. The frontend shows menu items per policy
  (from the JWT claims, display only); the API enforces. Changing a user's own roles, locking
  the user or forcing sign-out updates the security stamp (401 at the next request). A change
  to a role's permissions takes effect at its members' next request (REQ-012, Q-055 option A,
  D-034, replaces D-005's "next sign-in" and S-10's 8-hour window for role permissions): on
  every request, after the stamp and lockout check, the API replaces the token's permission
  claims with those of the user's current roles from the database; an in-memory cache of role
  permissions is allowed only when every role create, update and delete clears it (one API
  instance); when the permissions cannot be read the request fails (fail closed). Until T-058
  is built the code still waits for the next sign-in. Conditions of the security officer
  (Q-057), all part of T-058:
  - C1: the user's roles are read from the database by user id (user-role rows, role ids), never
    from the token's role-name claims; otherwise renaming a role and creating a new one with the
    old name would give an old token the new role's permissions.
  - C2: the cache, keyed by role id, is cleared after the role's transaction commits, never
    before, in both the legacy `RolesController` (`/api/roles`, Q-045) and the V1 Roles slice
    (`/api/v1/roles`, D-035), for create, update and delete.
  - C3: every permission claim of the token is removed before the current ones are added, and
    the owner-id claim is replaced from the loaded user, so `ReadScopeOwnerId()` uses the
    current value.
  - C4: a failed read is never caught to fall back to the token claims: the central error
    handler returns 500 with a general Problem Details message (D-022).
  - C5: a user whose roles give no permissions gets 403 on every endpoint with a permission
    policy. The own-account endpoints (`auth/2fa`, `auth/2fa/enable`, `auth/2fa/disable`,
    `auth/password`, `auth/logout`, old and `/api/v1` routes) stay on the fallback policy for them too: they act
    only on the caller's own account and let the user secure it (Q-058, security officer,
    2026-11-10). To cut a user off completely, an Admin locks the user or forces sign-out (401
    everywhere). Conditions: the stamp, lockout and permission reload run on these endpoints
    too, with no skip path, and C4 applies; the user id comes only from the token;
    `auth/password` keeps requiring the current password, and S-19 stays open in T-044/T-046;
    a new endpoint without a permission policy is listed below and reviewed by the security
    officer.
  - C6 (deployment note): after a role or permission change made straight in the database, not
    through the API, restart the API so the cache is empty.
  - S-28 (`ReadOwn` with other permissions refused) still applies to role create and update in
    both role endpoints.
  An Admin's two-factor reset
  (`POST /api/users/{id}/reset-2fa`) also updates the security stamp (Identity's
  `ResetAuthenticatorKeyAsync`), so the user's existing token gets 401 at the next request
  (REQ-013; checked in the code 2026-11-10, T-055).
- Endpoints without a permission policy: only `auth/login` and `auth/login-2fa` are anonymous;
  the own-account endpoints (`auth/2fa`, `auth/2fa/enable`, `auth/2fa/disable`,
  `auth/password`, `auth/logout`) need only a signed-in user (the fallback policy) and act on the
  caller's own account only, with the user id taken only from the token. Each is served on the
  old `/api/auth` and the `/api/v1/auth` route. Every other endpoint has a permission policy
  (REQ-019). `auth/logout` was added by T-044 (S-26, D-036 proposed); its review by the security
  officer is Q-060. The enumeration test (T6) allows exactly this list.
- Own-account and sign-in contract after T-044 (D-036, proposed; S-18, S-19, S-23, S-24, S-26,
  S-28). Failures are Problem Details as in Error handling:
  - `GET auth/2fa`: `{ enabled, sharedKey, authenticatorUri, ... }`; `sharedKey` and
    `authenticatorUri` are `null` while `enabled` is true, so the secret is shown only during
    set-up (S-18).
  - `POST auth/2fa/disable`: body `{ "password": string?, "code": string? }`, the current password
    or a current authenticator code (no recovery code). Neither given: 400; wrong: 400 and
    counted as a failed access (lockout after 5 failures). A 401 is not used here, because the
    frontend reads 401 as an expired sign-in (S-19). The profile page asks for one of the two
    (T-046); until then its "Turn off" gets 400.
  - `POST auth/logout`: no body; renews the caller's security stamp and returns 204. From then
    on every token of that user gets 401 at its next request, so signing out ends all of the
    user's sessions, not only the current one. The frontend calls it before it deletes its
    cookie (T-046) (S-26).
  - Maximum lengths, 400 when longer: user name 100, password 128, code 64, challenge token
    4096, on `login`, `login-2fa`, `2fa/enable`, `2fa/disable`, `password` and the password of
    user create (S-24).
  - `login` with an unknown or locked-out user name runs a password-hash check against a dummy
    hash, so it takes as long as a wrong password; the response stays 401 (S-23).
  - Role create and update (`/api/roles` and `/api/v1/roles`) refuse `ReadOwn` together with any
    other permission: 400 (S-28).

**Authorization matrix**

| Policy | Admin | Manager | Staff | Finance | Viewer | Owner (later) |
|---|---|---|---|---|---|---|
| Manage users, roles, 2FA reset | x | | | | | |
| Manage VAT rates | x | | | x | | |
| Manage owners and assets | x | x | | | | |
| Record/edit expenses and activities | x | x | x | | | |
| Create invoices, mark paid, PDF | x | x | | x | | |
| Reports | x | x | | x | x | |
| Read all data | x | x | x | x | x | |
| Read own data only | | | | | | x |
| Own account: password, two-factor sign-in, sign-out (self only) | x | x | x | x | x | x |
