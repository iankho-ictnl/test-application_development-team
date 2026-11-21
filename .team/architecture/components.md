# Layers and components

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged)._

(decision #1)
- `PropertyAdmin.Api` (ASP.NET Core Web API): EF Core + Npgsql, ASP.NET Core Identity, JWT bearer
  authentication, all business logic (VatService, InvoiceService, ReportService,
  InvoicePdfService), REST endpoints `/api/owners`, `/api/assets`, `/api/expenses`,
  `/api/activities`, `/api/vatrates`, `/api/invoices` (+ `/pdf`, `/pay`),
  `/api/reports/asset-results`, `/api/reports/owner-overview`, `/api/auth/login`. Swagger in
  development.
- `PropertyAdmin.Contracts` (class library): request/response models of the admin frontend and
  the tests only; the API no longer references it (T-036). Kept in step with the API's V1
  models by hand: every API contract change updates its `Contracts` type in the same item
  (D-037).
- Since D-026/D-037 the backend is `PropertyAdmin.Api` (V1 controllers and contracts),
  `PropertyAdmin.Logic` (handlers, feature services, invoice PDF, D-032) and
  `PropertyAdmin.Data` (EF Core); see API structure.
- Admin frontend (the Razor Pages project): no database access; typed HttpClient to the API;
  cookie sign-in that stores the JWT server-side in the auth cookie claims and forwards it.
- `PropertyAdmin.Tests` (xUnit): service unit tests and API integration tests.
- docker compose: `db` (PostgreSQL image, version in Technology versions), `api`, `web`; secrets
  via environment variables.

## Business logic

- VAT (decision #7): the user picks the rate name (High/Low/Zero); VatService resolves the
  percentage valid on the line date and stores it on the line (snapshot). Overlapping periods
  per name are refused.
- Invoicing (decision #8): lines are copied from uninvoiced activities and re-invoiceable
  expenses of the owner; the source records get the invoice reference in one transaction;
  numbering `YYYY-NNNN`.
- PDF (decision #9): PDFsharp-MigraDoc 6 (MIT).
- Localization (decision #10): .resx resources in the frontend and for API error messages;
  supported cultures in appsettings (`nl-NL` default, `en-US`); culture switch in the layout via
  a cookie; a new language is a .resx file plus a configuration entry.
