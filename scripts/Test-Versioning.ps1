$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
$githubActions = $env:GITHUB_ACTIONS
$env:GITHUB_ACTIONS = 'false'
function Invoke-Git {
    & git -C $scratch @args
    if ($LASTEXITCODE -ne 0) { throw "git failed: $args" }
}
function Add-Commit([string]$Message) {
    Invoke-Git -c user.name=VersionTests -c user.email=version-tests@example.invalid -c commit.gpgsign=false commit --allow-empty -m $Message
}
function Assert-Version([string]$Expected) {
    $json = & dotnet gitversion $scratch /config "$scratch/GitVersion.yml" /output json /nofetch /nonormalize /nocache
    if ($LASTEXITCODE -ne 0) { throw "GitVersion failed: $json" }
    $version = ($json -join "`n") | ConvertFrom-Json
    if ($version.SemVer -cne $Expected) { throw "Expected $Expected, got $($version.SemVer)" }
    Write-Host "Verified $Expected"
}
try {
    [System.IO.Directory]::CreateDirectory($scratch) | Out-Null
    Invoke-Git init --initial-branch=main
    Copy-Item "$root/GitVersion.yml" "$scratch/GitVersion.yml"
    Invoke-Git add GitVersion.yml
    Add-Commit 'Initial release'
    Invoke-Git tag v1.2.3
    Assert-Version '1.2.3'
    Add-Commit 'Patch change'
    Assert-Version '1.2.4'
    Invoke-Git tag v1.2.4
    Assert-Version '1.2.4'
    Assert-Version '1.2.4'
    Add-Commit 'Next patch change'
    Assert-Version '1.2.5'
    Invoke-Git tag v1.2.5
    Add-Commit 'New feature +semver: minor'
    Assert-Version '1.3.0'
    Invoke-Git tag v1.3.0
    Add-Commit 'Breaking change +semver: major'
    Assert-Version '2.0.0'
    Invoke-Git tag v2.0.0
    Invoke-Git checkout -b work/version-preview
    Add-Commit 'Preview change'
    $json = & dotnet gitversion $scratch /config "$scratch/GitVersion.yml" /output json /nofetch /nonormalize /nocache
    if ($LASTEXITCODE -ne 0) { throw 'GitVersion preview calculation failed.' }
    $preview = ($json -join "`n") | ConvertFrom-Json
    if ($preview.SemVer -notmatch '^2\.0\.1-alpha\.version-preview\.[0-9]+$') {
        throw "Expected a branch preview, got $($preview.SemVer)"
    }
    Write-Host "Verified preview: $($preview.SemVer)"
    Invoke-Git checkout main
    Invoke-Git -c user.name=VersionTests -c user.email=version-tests@example.invalid -c commit.gpgsign=false merge --no-ff work/version-preview -m 'Merge preview +semver: minor'
    Assert-Version '2.1.0'
} finally {
    $env:GITHUB_ACTIONS = $githubActions
    if ([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($scratch)) -ne [System.IO.Path]::GetTempPath().TrimEnd('\', '/')) {
        throw 'Refusing to delete outside the temporary directory.'
    }
    Remove-Item -LiteralPath $scratch -Recurse -Force
}
