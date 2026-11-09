# Upgrade Options

## Strategy

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade all 4 projects (all SDK-style, net8.0, ~6K LOC) in one atomic pass, then build and test together. |
| Bottom-Up | Upgrade Contracts first, then Api / Web app, then Tests, validating each tier. |
| Top-Down | Upgrade applications first, libraries later. |

## Package Handling

| Value | Description |
|-------|-------------|
| **Update to latest compatible** (selected) | Bump ASP.NET Core / EF Core packages to 10.0.x, Npgsql EF to 10.x, replace incompatible preview `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` with a stable version, update deprecated test packages (xunit, test SDK). |
| Minimum required only | Only change packages that block the build. |
