# 02-upgrade-all-projects: Upgrade all projects to net10.0

Change TargetFramework to net10.0 in all 4 projects. Bump Microsoft.AspNetCore.* and Microsoft.EntityFrameworkCore.* packages to 10.0.x, Npgsql.EntityFrameworkCore.PostgreSQL to a 10.x version, replace the incompatible preview Microsoft.VisualStudio.Azure.Containers.Tools.Targets with a stable version, and refresh deprecated packages (System.IdentityModel.Tokens.Jwt, xunit, test SDK). Fix compilation errors across the solution.

Assessment flags ~78 API incidents, mostly JWT/IdentityModel (JwtSecurityTokenHandler, JwtRegisteredClaimNames) binary incompatibilities that typically resolve by recompiling against updated packages, plus behavioral changes in System.Uri, HttpContent and JsonDocument. Check Dockerfiles for base image tags referencing 8.0.

**Done when**: All projects target net10.0, packages updated, solution builds with 0 errors and 0 new warnings.

## Research

### Project files (all SDK-style, net8.0, no Directory.*.props / CPM)
- PropertyAdmin.Contracts: no packages.
- PropertyAdmin.Api: JwtBearer 8.0.11 → 10.0.x, Identity.EntityFrameworkCore 8.0.11 → 10.0.x, EF Core Design 8.0.11 → 10.0.x, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11 → 10.0.3, PDFsharp-MigraDoc 6.1.1 (compatible, keep), Swashbuckle 6.6.2 (compatible; keep, because 10.x has breaking Microsoft.OpenApi v2 changes).
- test application for development team (Razor Pages): Containers.Tools.Targets 1.24.2-preview.1 (incompatible) → 1.23.0 stable; System.IdentityModel.Tokens.Jwt 7.1.2 (deprecated) → 8.x; QRCoder 1.6.0 keep.
- PropertyAdmin.Tests: Microsoft.NET.Test.Sdk 17.8.0 → 18.x, xunit 2.5.3 → 2.9.3 (latest v2; xunit.v3 migration deferred), xunit.runner.visualstudio → 3.1.x, coverlet.collector → 6.0.4.

### Dockerfiles
- PropertyAdmin.Api/Dockerfile and test application for development team/Dockerfile use aspnet:8.0 / sdk:8.0 images → 10.0.

### Code issues
- JWT APIs (JwtSecurityTokenHandler, JwtRegisteredClaimNames) flagged as binary incompatible; these should resolve when rebuilt against the updated packages.
- Behavioral changes: System.Uri, HttpContent, JsonDocument, TimeSpan.FromSeconds overloads (source incompatible).
- No `// STUB:` markers found.

