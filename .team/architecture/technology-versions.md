# Technology versions

_Owner: architect. Part of [architecture.md](../architecture.md) (split in T-076, text unchanged)._

for every technology, the newest stable version whose official support
ends latest (usually the newest long-term support release), checked on the vendor's lifecycle
page when decided. An end of support within the expected life of the solution is a risk.

Filled by the architect on 2026-11-06 (task 1) from the project files, Dockerfiles,
`docker-compose.yml` and `wwwroot/lib`. Every source was checked on 2026-11-06. "Code now" is
what the code uses today; the version in front of it is the proposed version. LTS means
long-term support.

| Technology | Version | Release type (for example LTS) | End of support | Source (lifecycle page) | Decision |
|---|---|---|---|---|---|
| .NET runtime and ASP.NET Core (target framework of all four projects) | 10.0, latest patch 10.0.14 (2026-10-25). Code now: `net8.0` | LTS | 2028-11-14 (.NET 8: 2026-11-10) | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | #12 |
| .NET SDK (build) | 10.0, newest installed 10.0 feature band, set by a new `global.json` (no preview SDKs). Code now: no `global.json`; SDK 8.0 image | LTS (follows the runtime) | 2028-11-14 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | #12 |
| ASP.NET Core packages: Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.AspNetCore.Identity.EntityFrameworkCore, Microsoft.AspNetCore.OpenApi, Microsoft.AspNetCore.Mvc.Testing | 10.0.14. Code now: 8.0.11 (OpenApi and Mvc.Testing not referenced yet) | LTS | 2028-11-14 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | #12, #16, #19 |
| Entity Framework Core (EF Core): Microsoft.EntityFrameworkCore.Design and the `dotnet-ef` tool; the runtime packages come in through Identity and Npgsql | 10.0.14. Code now: 8.0.11 | LTS | 2028-11-10 on the EF Core page (the .NET page gives 2028-11-14 for .NET 10) | [EF Core releases](https://learn.microsoft.com/en-us/ef/core/what-is-new/) | #12 |
| Npgsql.EntityFrameworkCore.PostgreSQL and Npgsql | 10.0.3 (2026-07-10). Code now: 8.0.11 and 8.0.6 | No lifecycle published; the provider's major version follows the EF Core major version | Not published | [Npgsql compatibility](https://www.npgsql.org/doc/compatibility.html), [EF provider 10.0 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html), NuGet | #12 |
| System.IdentityModel.Tokens.Jwt (Microsoft IdentityModel) | 8.23.0 (2026-09-18), referenced explicitly in the API and the admin frontend. Code now: 7.1.2, deprecated on NuGet as "Legacy" | Active major version | Tied to .NET 10, about November 2028 (7.x: 2026-11-10) | [IdentityModel support policy](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/blob/dev/supportPolicy.md) | #13 |
| .NET container images (`mcr.microsoft.com/dotnet/aspnet` and `sdk`) | `10.0-resolute` (Ubuntu 26.04 LTS). Code now: `8.0` (Debian 12, in its long-term support phase since 2026-06-11) | Ubuntu LTS | Ubuntu 26.04 standard security maintenance until May 2031; Microsoft updates the images until .NET 10 ends (2028-11-14) | [Ubuntu release cycle](https://ubuntu.com/about/release-cycle), [default .NET 10 images use Ubuntu](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu), [.NET in Ubuntu 26.04](https://devblogs.microsoft.com/dotnet/whats-new-for-dotnet-in-ubuntu-2604/), [image OS policy](https://github.com/dotnet/dotnet-docker/blob/main/documentation/supported-platforms.md) | #14 |
| PostgreSQL | 18 (minor 18.6). Code now: 16 | Major version, supported 5 years | 2030-11-14 (16: 2028-11-09) | [PostgreSQL versioning policy](https://www.postgresql.org/support/versioning/) | #15 |
| PostgreSQL container image | `postgres:18-trixie` (Debian 13). Code now: `postgres:16` (also Debian 13) | Debian stable, then Debian LTS | Debian 13: regular security support until 2028-08-09, long-term support until 2030-06-30 | [Debian LTS](https://wiki.debian.org/LTS), [postgres image documentation](https://github.com/docker-library/docs/blob/master/postgres/README.md) | #15 |
| OpenAPI document of the API (development only) | Microsoft.AspNetCore.OpenApi 10.0.14. Code now: Swashbuckle.AspNetCore 6.6.2 | LTS (part of ASP.NET Core 10) | 2028-11-14 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [OpenAPI in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) | #16 |
| Swagger UI of the API (development only) | Swashbuckle.AspNetCore.SwaggerUI 10.3.1 (2026-10-19, MIT license) | Community project, no lifecycle published | Not published | NuGet, [Use the generated OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0) | #16 |
| PDFsharp-MigraDoc | 6.2.4 (2026-01-06), the last version under the MIT license. Code now: 6.1.1. Newest: 6.5.0 (2026-10-14), licensed AGPL-3.0 only | No lifecycle published; maintenance of 6.2.x not stated | Not published | NuGet license data, [PDFsharp history](https://docs.pdfsharp.net/General/History.html), [6.5.0 announcement](https://github.com/empira/PDFsharp/discussions/413) | #17, question 21 |
| QRCoder | 1.8.1 (2026-10-13, MIT license). Code now: 1.6.0 | No lifecycle published | Not published | NuGet, [QRCoder releases](https://github.com/Shane32/QRCoder/releases) | #18 |
| Microsoft.VisualStudio.Azure.Containers.Tools.Targets (Visual Studio container debugging) | 1.23.0 (2025-09-26), newest stable. Code now: 1.24.2-preview.1, a preview that is unlisted on NuGet | Stable | Not published | NuGet | #18 |
| xUnit.net: xunit.v3 and xunit.runner.visualstudio | xunit.v3 4.0.2 (2026-10-09), xunit.runner.visualstudio 4.0.1. Code now: xunit 2.5.3, deprecated on NuGet as "Legacy" (security fixes only), runner 2.5.3 | Current major version | Not published | NuGet deprecation notice, [xUnit v3 migration guide](https://xunit.net/docs/getting-started/v3/migration) | #19 |
| Microsoft.NET.Test.Sdk | 18.11.0 (2026-10-13). Code now: 17.8.0 | Stable | Not published | NuGet | #19 |
| coverlet.collector | Removed by D-051 part 3 (T-137, commit cacfe2d on change/D-051-code-checks, pull request #20): coverage comes from Microsoft.CodeCoverage, which Microsoft.NET.Test.Sdk brings (command in testing.md). Was 6.0.4 | - | - | D-051 | #19, D-051 |
| Testcontainers.PostgreSql (tests only; proposed with D-031) | 4.16.0, newest stable on 2026-11-10, MIT license; targets .NET 8 and .NET Standard 2.0, so it runs on .NET 10. Testcontainers.XunitV3 4.16.0 only if the fixtures use it, after xUnit v3 (T-011) | Stable | Not published | [NuGet Testcontainers.PostgreSql](https://www.nuget.org/packages/Testcontainers.PostgreSql), [NuGet Testcontainers.XunitV3](https://www.nuget.org/packages/Testcontainers.XunitV3) | D-031 |
| Bootstrap | 5.3.8 (2025-08-26). Code now: 5.1.0 | Version 5 is "Active LTS" | Not published ("TBD") | [Bootstrap release policy](https://github.com/twbs/release) | #20 |
| jQuery, jQuery Validation, jQuery Validation Unobtrusive | Depends on question 22: removed, or jQuery 3.7.1 + jQuery Validation 1.22.1 + Unobtrusive 4.0.0. Code now: 3.6.0, 1.19.5, 4.0.0. Newest jQuery: 4.0.0 (2026-01-18) | jQuery 4.x is current; 3.x gets critical security fixes only | No dates published | [jQuery support](https://jquery.com/support/), [jQuery 4 upgrade guide](https://jquery.com/upgrade-guide/4.0/), [jQuery Validation releases](https://github.com/jquery-validation/jquery-validation/releases) | #20, question 22 |

**End-of-support risks.** The expected life of the solution is not known (question 19), so
every end of support is listed here; each one that falls before the end of that life is a risk.
- Code as it is today: .NET 8, EF Core 8 and IdentityModel 7.x end on 2026-11-10;
  System.IdentityModel.Tokens.Jwt 7.1.2 and xunit 2 are deprecated on NuGet; the .NET 8 images
  run Debian 12, which moved to Debian LTS on 2026-06-11 (until 2028-06-30). Fixed by decisions
  #12, #13, #14 and #19 (tasks U1 to U3 and U6).
- .NET 10, ASP.NET Core 10, EF Core 10 and IdentityModel 8: 2028-11-14 (the EF Core page says
  2028-11-10). Upgrade proposal: move to the next .NET LTS release before that date.
- Debian 13 in the PostgreSQL image: regular security support until 2028-08-09, then Debian LTS
  until 2030-06-30. Upgrade proposal: check the image's Debian base again before 2028-08.
- PostgreSQL 18: 2030-11-14. PostgreSQL 16 (code now): 2028-11-09.
- Ubuntu 26.04 in the .NET images: May 2031, after .NET 10 ends.
- PDFsharp-MigraDoc 6.2.x: the vendor states no maintenance period (decision #17, question 21).
- jQuery 3.x, only if it stays: critical security fixes only, no end date published; move to
  4.x when the Unobtrusive library supports it (decision #20, question 22).
- No lifecycle published for Npgsql, Swagger UI, QRCoder, the Visual Studio container tools,
  xUnit, the test SDK, coverlet and Bootstrap: keep each on its newest stable version and check
  them at every .NET patch update.

**Upgrade plan for the expected life (question 19: 5 years, until about 2031-11; task 15).**
| When | Action | Reason |
|---|---|---|
| Every month | .NET and PostgreSQL patch releases, rebuild images (Ubuntu/Debian fixes) | Security fixes |
| 2027-11 (.NET 12 LTS released, expected) | Spike: build and test on .NET 12 in a branch; record findings as a decision | Early impact estimate |
| 2028-02 to 2028-06 | Upgrade .NET 10 to .NET 12 LTS (projects, EF Core, Npgsql, IdentityModel, images); done at least 4 months before 2028-11-14 | .NET 10 ends 2028-11-14; .NET 12 is expected to be supported until about 2030-11 |
| 2028-07 | Check the PostgreSQL image's Debian base (Debian 13 regular support ends 2028-08-09) | Base image support |
| 2029-11 (.NET 14 LTS released, expected) | Plan .NET 12 to .NET 14 LTS; execute before 2030-06 | .NET 12 ends about 2030-11, before 2031-11 |
| 2029-09 to 2030-05 | Upgrade PostgreSQL 18 to the newest major (20 or 21 by then) with dump and restore, after a tested restore (Database architecture); at least 6 months before 2030-11-14 | PostgreSQL 18 ends 2030-11-14 |
| 2031-05 | Decide on the life after 2031-11 (extend, replace, stop) | End of expected life |

Expected dates of future releases follow the published yearly cadence (LTS every November in odd years for .NET, a major every autumn for PostgreSQL); recheck the lifecycle pages at each
step. Effort per .NET LTS step: about 5 story points; PostgreSQL major: about 3.
