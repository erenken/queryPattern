# Changelog

## 1.0.0 (unreleased)

### Breaking changes

- Removed the .NET 8 target and EF Core 8 dependency. Applications must target
  .NET 10 or .NET 11 to use this release. Applications remaining on .NET 8
  must use the earlier 0.x package line.

### Added

- .NET 11 support with EF Core 11 RC2 (`11.0.0-rc.2.26504.105`), including
  SQLite sample and test coverage, NuGet assets, and debugging symbols.
- Builds use the .NET 11 RC2 SDK. .NET 10 with EF Core 10 remains supported.

### Release versioning

- GitVersion's `next-version: 1.0.0` makes this a major release from the
  previous 0.x line. Remove the hint after tagging `v1.0.0`.
- Use `+semver: major` in the commit or merge message to record the breaking
  change explicitly.
