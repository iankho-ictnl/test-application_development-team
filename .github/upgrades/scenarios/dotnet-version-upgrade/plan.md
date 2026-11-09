# .NET 10 Upgrade Plan

## Overview

**Target**: Upgrade all projects from net8.0 to net10.0 (LTS)
**Scope**: 4 SDK-style projects, ~6K LOC

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 4 projects, all on .NET 8, shallow dependency graph, all SDK-style.

Projects: PropertyAdmin.Contracts (library), PropertyAdmin.Api (ASP.NET Core API), test application for development team (Razor Pages web app), PropertyAdmin.Tests (xUnit tests).

## Tasks

### 01-prerequisites: Verify .NET 10 SDK and global.json

Confirm the .NET 10 SDK is installed and that any global.json does not pin an incompatible SDK version.

**Done when**: .NET 10 SDK available; global.json (if present) compatible.

---

### 02-upgrade-all-projects: Upgrade all projects to net10.0

Change TargetFramework to net10.0 in all 4 projects. Bump Microsoft.AspNetCore.* and Microsoft.EntityFrameworkCore.* packages to 10.0.x, Npgsql.EntityFrameworkCore.PostgreSQL to a 10.x version, replace the incompatible preview Microsoft.VisualStudio.Azure.Containers.Tools.Targets with a stable version, and refresh deprecated packages (System.IdentityModel.Tokens.Jwt, xunit, test SDK). Fix compilation errors across the solution.

Assessment flags ~78 API incidents, mostly JWT/IdentityModel (JwtSecurityTokenHandler, JwtRegisteredClaimNames) binary incompatibilities that typically resolve by recompiling against updated packages, plus behavioral changes in System.Uri, HttpContent and JsonDocument. Check Dockerfiles for base image tags referencing 8.0.

**Done when**: All projects target net10.0, packages updated, solution builds with 0 errors and 0 new warnings.

---

### 03-final-validation: Build and test the full solution

Run a full solution build and the PropertyAdmin.Tests suite; fix failures and document any deferred items.

**Done when**: Solution builds cleanly and all tests pass.
