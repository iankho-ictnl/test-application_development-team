# API structure

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged)._

**Before D-026 (code, 2026-11-09):** `PropertyAdmin.Api/Controllers/` with several controllers per file
that use `AppDbContext` directly, services in `PropertyAdmin.Api/Services/`, data access in
`PropertyAdmin.Api/Data/`, one shared `Mapper.cs`, all DTOs in `PropertyAdmin.Contracts/Dtos.cs`
shared with the frontend, routes without a version.

**Now (2026-11-10, D-037 replaces D-026 after Q-075 b):** the backend follows the target below
(T-032 to T-036). The frontend and the tests do not: the frontend keeps `Services/ApiClient.cs`
with `PropertyAdmin.Contracts` and the unversioned `/api` routes (kept as extra route
attributes on the V1 controllers), and the existing tests keep their places. Remaining
deviations, accepted by the user's choice: one `Dtos.cs` for all frontend models, no API client
or mapping per feature in the frontend, unversioned routes from the frontend, existing tests
not mirrored. The Frontend line below and "No shared contracts project" no longer apply to the
frontend.

**Target (D-026, accepted 2026-11-09, stopped by D-037; supersedes D-021):**

```
PropertyAdmin.Api       V1/<Feature>/<Feature>Controller.cs
                        V1/<Feature>/<UseCase>/Request.cs, RequestValidation.cs, Response.cs, Mapping.cs, Models/
                        Program.cs, error handler, auth set-up (outside V1)
PropertyAdmin.Logic     V1/<Feature>/I<Feature>Service.cs, <Feature>Service.cs
                        V1/<Feature>/<UseCase>/Query.cs|Command.cs, ...Handler.cs, ...Validation.cs, Result.cs, <Source>Mapping.cs, Models/
PropertyAdmin.Data      AppDbContext, entities, migrations, seeder
Frontend                Services/Api/V1/<Feature>/<Feature>Client.cs, Models/, Mapping.cs
PropertyAdmin.Tests     mirrors V1/<Feature>/<UseCase>/
```

Proposed additions (reviewed in T-048, 2026-11-10): the invoice PDF renderer, its font resolver
and `CompanyOptions` live in `PropertyAdmin.Logic/V1/Invoices/GetPdf/` (D-032); the tests mirror
`V1/<Feature>/<UseCase>/` with `ApiTests`, `HandlerTests` and `MappingTests`, frontend client
tests under `Frontend/V1/<Feature>/`, and carry `Requirement` and `TestCase` traits (D-027).

Features: Auth, Users, Roles (D-035, accepted), Owners, Assets, VatRates, Expenses, Activities,
Invoices, Reports.
Routes `/api/v1/<feature>`. Commands change data in a transaction; queries only read
(`AsNoTracking`, projection). The feature service is the only user of the data access; entities
never leave `PropertyAdmin.Data` and `PropertyAdmin.Logic`. No shared contracts project.
