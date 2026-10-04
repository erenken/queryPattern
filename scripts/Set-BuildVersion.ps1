param(
    [string]$OutputPath = 'artifacts/Version.props',
    [switch]$RequireStable
)

$ErrorActionPreference = 'Stop'
$versionJson = & dotnet gitversion /output json /nofetch
if ($LASTEXITCODE -ne 0) { throw 'GitVersion failed.' }
$version = ($versionJson -join "`n") | ConvertFrom-Json
if ($RequireStable) {
    & "$PSScriptRoot/Assert-StableRelease.ps1" -Version $version.SemVer -Branch $version.BranchName
}
$properties = [ordered]@{
    Version = $version.SemVer
    PackageVersion = $version.SemVer
    AssemblyVersion = $version.AssemblySemVer
    FileVersion = $version.AssemblySemFileVer
    InformationalVersion = $version.InformationalVersion
    IncludeSourceRevisionInInformationalVersion = 'false'
    SourceRevisionId = $version.Sha
    RepositoryCommit = $version.Sha
    ContinuousIntegrationBuild = 'true'
}
$document = [System.Xml.XmlDocument]::new()
$project = $document.AppendChild($document.CreateElement('Project'))
$group = $project.AppendChild($document.CreateElement('PropertyGroup'))
foreach ($property in $properties.GetEnumerator()) {
    $element = $group.AppendChild($document.CreateElement($property.Key))
    $element.InnerText = $property.Value
}
$fullPath = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($fullPath)) | Out-Null
$document.Save($fullPath)
if ($env:GITHUB_OUTPUT) {
    "version=$($version.SemVer)" >> $env:GITHUB_OUTPUT
    "sha=$($version.Sha)" >> $env:GITHUB_OUTPUT
    "artifact-name=packages-$($env:GITHUB_RUN_ID)-$($env:GITHUB_RUN_ATTEMPT)" >> $env:GITHUB_OUTPUT
}
Write-Host "Version: $($version.SemVer); commit: $($version.Sha)"
