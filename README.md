[![Build and Test](https://github.com/erenken/queryPattern/actions/workflows/build-tests.yml/badge.svg)](https://github.com/erenken/queryPattern/actions/workflows/build-tests.yml) [![Release](https://github.com/erenken/queryPattern/actions/workflows/release.yml/badge.svg)](https://github.com/erenken/queryPattern/actions/workflows/release.yml) <a href="https://www.nuget.org/packages/myNOC.EntityFramework.Query"><img src="https://img.shields.io/nuget/v/myNOC.EntityFramework.Query.svg" alt="NuGet Version" /></a> 
<a href="https://www.nuget.org/packages/myNOC.EntityFramework.Query"><img src="https://img.shields.io/nuget/dt/myNOC.EntityFramework.Query.svg" alt="NuGet Download Count" /></a>

# myNOC.EntityFramework.Query

## Overview

This *Query Pattern* is something I got from listening to a [.NET Rocks](https://www.dotnetrocks.com/) episode #1494 
[Developer Tips and Design Patterns with Steve Smith](https://www.dotnetrocks.com/details/1494).  [Steve Smith](https://ardalis.com/) talks about the 
*Specification Pattern* at 49:50 in the podcast, and this is what inspired this.

What Steve describes is a problem we saw in our projects.  Developers would keep adding more methods to the repository or keep adding JOINs to a result 
because they need this one other column added to an already returning list.  This will start to overly complicate the original list.  For example, you 
already have a screen that shows the list of users, and if they are active or not.  Now, some other screen needs the list of users and wants the security 
role or permissions that user has also displayed.  What happens often is someone just goes and modifies that original user list and adds this other data.  
So, where you needed a simple quick list of users is now doing way more than it needs and could potentially slow down the query.

This pattern also gives you the ability to easily test your queries.  As Steve points out in the podcast what might work fine in compile time, 
or even within a unit test using mocked lists as your data, could give you a runtime error when running against EntityFramework.  

## Installation and supported frameworks

```shell
dotnet add package myNOC.EntityFramework.Query
```

The package, tests, and sample target **.NET 8.0 and .NET 10.0** (LTS).
The .NET 8 asset uses the latest EF Core 8 servicing release; the .NET 10
asset uses EF Core 10. EF Core 10 cannot run on .NET 8. Dependency reports
may therefore show newer major versions for .NET 8; this is intentional.
Test tooling uses the current stable MSTest, VSTest, Coverlet and NSubstitute.

## Local build and validation

Install the .NET SDK selected by [global.json](./global.json), plus the
.NET 8 runtime to run the .NET 8 tests. No .NET 9 SDK/runtime is needed.
Run these commands from the repository root:

```shell
dotnet tool restore
dotnet restore -warnaserror
dotnet build --configuration Release --no-restore -warnaserror
dotnet test --configuration Release --no-build --no-restore --logger trx --collect:"XPlat Code Coverage" --results-directory artifacts/test-results
dotnet pack src/myNOC.EntityFramework.Query --configuration Release --no-build --no-restore -warnaserror --output artifacts/packages
dotnet list package --outdated --include-transitive
dotnet list package --vulnerable --include-transitive
```

Warnings are errors in every project. Tests run for both target frameworks.
PowerShell 7 is required for the versioning and package-validation scripts.
To reproduce CI's version stamping locally:

```powershell
./scripts/Test-Versioning.ps1
./scripts/Set-BuildVersion.ps1
$env:VersionPropsFile = (Resolve-Path artifacts/Version.props).Path
dotnet restore -warnaserror
dotnet build --configuration Release --no-restore -warnaserror
dotnet pack src/myNOC.EntityFramework.Query --configuration Release --no-build --no-restore -warnaserror --output artifacts/packages
$version = dotnet gitversion /output json /nofetch | ConvertFrom-Json
./scripts/Test-Packages.ps1 -Version $version.SemVer -Commit $version.Sha
Remove-Item Env:VersionPropsFile
```

Source downloads require network access and a commit pushed to GitHub.
Uncommitted source changes cannot match the published commit's checksums.
Without the version props file, local builds retain the SDK's default version.

## Automatic versioning

[GitVersion.yml](./GitVersion.yml) uses GitVersion 6, pinned in the local
tool manifest. CI fetches complete history and tags, then generates a single
props file used by restore, build and pack. It stamps `Version`, `PackageVersion`,
`AssemblyVersion`, `FileVersion` and `InformationalVersion`. The informational
version and NuGet repository metadata contain the exact build commit.

- `main` produces only stable `major.minor.patch` packages; two guards reject
  prereleases and publishing from any other branch.
- A commit after `v1.2.3` produces `1.2.4`. Publishing creates `v1.2.4` on
  that exact commit; the next release becomes `1.2.5`.
- Rerunning a tagged commit retains its version rather than incrementing it.
- Add `+semver: minor` to the commit or merge/squash message for `1.3.0`.
  Add `+semver: major` for `2.0.0`. Preserve these directives when squashing.
- Work/feature branches produce `alpha.<branch>.<number>` previews; PRs
  produce preview versions. CI validates but never publishes them.
- Avoid `+semver: none` for publishable changes: NuGet versions are immutable.

[Test-Versioning.ps1](./scripts/Test-Versioning.ps1) exercises patch releases,
stable tags, tagged reruns, previews, explicit minor/major increments and
merge-message increments in an isolated temporary Git repository.

## CI and Trusted Publishing

[Build and Test](./.github/workflows/build-tests.yml) restores, builds and tests
the entire solution for both frameworks, collects coverage/TRX files, audits
dependencies, validates workflow syntax, and creates packages with warnings
treated as errors. Its packaged Source Link check downloads source and verifies
checksums for **each PDB extracted from the `.snupkg`**, not just build output.
Test results, coverage, dependency reports and validated packages are artifacts.

[Release](./.github/workflows/release.yml) calls that same validation workflow,
then publishes only from `main` using the `nuget` GitHub environment.
Repository workflow token permissions default to read-only; the publishing job
alone receives `contents: write` and `id-token: write`.

Trusted Publishing configuration for this repository:

| Setting | Value |
| --- | --- |
| GitHub secret `NUGET_USER` | `erenken` (NuGet username, not an API key) |
| GitHub environment | `nuget`, deployment branch restricted to `main` |
| NuGet policy name | `queryPattern-release` |
| Package owner | `erenken` |
| Provider | GitHub Actions |
| Repository owner / repository | `erenken` / `queryPattern` |
| Workflow filename | `release.yml` (not a full path) |
| Environment in policy | `nuget` (must match exactly) |
| Package scope | `myNOC.EntityFramework.Query` (no wildcard) |
| Permissions | Push only new package versions; no unlist/relist |

Manage the policy at [NuGet Trusted Publishing](https://www.nuget.org/account/trustedpublishing).
`NuGet/login` exchanges a GitHub OIDC token for a short-lived publishing key.
There is no permanent API-key fallback. After this PR is merged, delete the
legacy `NUGET_PUBLISH` repository secret and revoke its old key on NuGet.
It is retained during the PR so the existing `main` workflow is not broken.

Releases are serialized. The workflow checks that an existing version tag
points to the exact build commit, explicitly publishes packages and symbols
with duplicate skipping, then creates a stable GitHub release and `v<version>`
tag. Existing releases are reused and assets refreshed on reruns. A failed-job
rerun downloads the original successful validation job's package artifact.
NuGet or symbol indexing may finish after the push completes; unrelated
authentication/network errors fail the job rather than being ignored.

## Source Link debugging

Every framework has a portable PDB in the separate `.snupkg`. The SDK's built-in
Source Link provider maps source to the exact GitHub commit; no extra Source
Link package is required. The primary `.nupkg` contains the framework DLLs.

In Visual Studio, add `https://symbols.nuget.org/download/symbols` under
**Tools > Options > Debugging > Symbols**, enable **Source Link support**,
and disable **Just My Code** when stepping into the library. Load the symbols
for `myNOC.EntityFramework.Query.dll`, then step into a method. Wait for NuGet's
symbol validation/indexing if symbols are not available immediately.

## Temporary artifacts

`.tools/` (downloaded CLI/lint tools), `.validation/` (local inspection output),
`.pack/`, `artifacts/` (packages, version props, TRX/coverage/audit reports),
and project `bin/`/`obj/` directories are ignored and can be deleted after
validation. `.nupkg` and `.snupkg` files are ignored anywhere. Removing the
tools means downloading them again if needed. Keep the tracked tool manifest,
build props, GitVersion configuration and validation scripts.

## Documentation & Usage

The documentation for the library can be found [here](./src/myNOC.EntityFramework.Query/README.md)
A very simple sample project [here](./sample/QuerySample/).
