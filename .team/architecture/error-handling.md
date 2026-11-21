# Error handling

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged)._

(D-022, accepted.) One `IExceptionHandler` with `AddProblemDetails()` and `UseExceptionHandler()`,
outside the version folders, turns every failure into RFC 9457 Problem Details
(`application/problem+json`) with a safe message and `traceId`. Endpoints and handlers never
format errors; `DomainExceptionFilter` is removed (T-029).

| Situation | Status | Logged as |
|---|---|---|
| Invalid request (validation) | 400 with `errors` per field | information |
| Not signed in, missing/invalid/expired token | 401 | information |
| Signed in, not allowed | 403 | warning |
| Not found or not visible to this user | 404 | information |
| Concurrency conflict (`xmin`), state conflict | 409 | information |
| Business rule violated | 422 (D-022; the default would be 409) | information |
| Other system failed / no answer in time | 502 / 504 | error |
| Unexpected failure | 500, general message only | error, once, with trace id |
| Client cancelled the request | 499, no body | Debug, not an error |

Every controller, handler and service takes the request's `CancellationToken` and passes it to
EF Core, PDF generation and every other call that can wait. A timeout of our own on a call to
another system is 504, not a cancellation.

**Other systems (Q-051, 2026-11-10):** none today. PostgreSQL is the API's own data store and
PDF generation is an in-process library: their failures, including the Npgsql command timeout,
are unexpected failures (500, logged once). The 502/504 row applies when an outbound
integration is added (for example mail or an off-host backup target, REQ-026): its failure gives
502, its timeout (HttpClient or resilience timeout, a cancellation not caused by the client) 504.
The code maps `BusinessRuleException` to 400, not 422 as above: a deviation from D-022 to fix. Responses never contain stack traces, exception messages, queries or internal ids.

**Report period (D-033, accepted; Q-056; built in T-161):** both report endpoints require `from`
and `to` (`yyyy-MM-dd`); missing, unparseable or reversed gives 400 with `errors` per field
(`errors.from`, `errors.to`; reversed at `to`), localized by `lang`. The API has no default
period; the frontend sends its own (the current calendar year).

**Language of the messages (D-049, changed after Q-108; T-117):** the API owns every
user-facing message text and returns it in the language of the call.

Interface contract, every API endpoint (`/api/...` and `/api/v1/...`):

| Item | Contract |
|---|---|
| Parameter | query parameter `lang`, optional, on every call (GET, POST, PUT, DELETE alike), next to any other query parameters: `/api/v1/owners/5?lang=en-US`, `/api/v1/reports/x?from=...&to=...&lang=en-US` |
| Values | `nl-NL` or `en-US`, case-insensitive |
| Absent, empty or another value (`de-DE`, `en`, `xx`) | Dutch (`nl-NL`); never a 400 |
| What it changes | only the UI culture: field validation messages (400 `errors`) and business-rule messages (`detail`). Number and date formatting, JSON values and the Problem Details `title` do not change |
| `Accept-Language` | no longer read: the API ignores it (no fallback) |
| API set-up | `RequestLocalizationOptions.RequestCultureProviders` = only `QueryStringRequestCultureProvider` with `QueryStringKey` and `UIQueryStringKey` = `lang`; supported UI cultures `nl-NL` (default) and `en-US`; the culture stays the process culture (T-114) |
| Frontend | a `DelegatingHandler` on the `ApiClient` HTTP client (next to `AccessTokenHandler`) adds `lang=<CultureInfo.CurrentUICulture.Name>` to the query of every request, keeping the existing query; the pages and `ApiClient` methods do not pass it themselves |

Field validation and business-rule messages come from the API's resource files (D-010), keyed
by the English text; business-rule messages are looked up in the central error handler in the
request's UI culture (D-050). The frontend shows the `detail` (else the `title`) as received and
never translates API text (replaces the lookup of D-048). In S2 for the owner messages; the
other features' business-rule messages, still fixed Dutch text in the Logic project, follow
when their requirement is in the current sprint.

Field validation attributes (D-052; T-127, T-140): an attribute's `ErrorMessage` is the English
key, and `AddDataAnnotationsLocalization` looks every key up in one shared resource
(`PropertyAdmin.Api/Validation/ValidationMessages.cs`, Dutch in
`Resources/Validation/ValidationMessages.nl.resx`, outside the version folders), because MVC gives
the localizer the parameter type, not the request type, for record parameters. The messages do not
name the field (the `errors` key does). Owners and assets use keys (S3, T-053/T-140); attributes
without `ErrorMessage` (the other features today) keep the framework's English defaults for every
`lang` until their requirement is in the current sprint. Feature-specific checks (owner KvK/BTW,
`V1/Owners/OwnerNumberValidation.cs`) stay in their feature and only take their texts from
`ValidationMessages`.
