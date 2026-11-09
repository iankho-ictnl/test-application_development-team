# .NET Version Upgrade

## Strategy
**Selected**: All-At-Once — 4 SDK-style net8.0 projects, shallow graph.

### Execution Constraints
- Single atomic upgrade — all projects updated together
- Update TFMs, then packages, restore, then build and fix errors in one pass
- Validate full solution build after upgrade; tests run after build is green

## Upgrade Options
- **Strategy**: All-at-Once
- **Package Handling**: Update to latest compatible

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0
- **Solution**: test application for development team.slnx
