param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Commit,
    [string]$PackageDirectory = 'artifacts/packages'
)

$ErrorActionPreference = 'Stop'
$packageId = 'myNOC.EntityFramework.Query'
$package = Join-Path $PackageDirectory "$packageId.$Version.nupkg"
$symbols = Join-Path $PackageDirectory "$packageId.$Version.snupkg"
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
[System.IO.Directory]::CreateDirectory($scratch) | Out-Null
try {
    [System.IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path $package), "$scratch/package")
    [System.IO.Compression.ZipFile]::ExtractToDirectory((Resolve-Path $symbols), "$scratch/symbols")
    [xml]$manifest = Get-Content "$scratch/package/$packageId.nuspec" -Raw
    $metadata = $manifest.package.metadata
    if ($metadata.version -cne $Version -or $metadata.repository.commit -cne $Commit) {
        throw 'Package version or repository commit does not match the build.'
    }
    if (Get-ChildItem "$scratch/package" -Recurse -Filter '*.pdb') {
        throw 'Portable PDBs must be in the symbol package, not the primary package.'
    }
    $frameworks = @('net8.0', 'net10.0')
    $pdbs = @(Get-ChildItem "$scratch/symbols" -Recurse -Filter '*.pdb')
    if ($pdbs.Count -ne $frameworks.Count) { throw 'Expected one packaged PDB per target framework.' }
    foreach ($framework in $frameworks) {
        $assembly = "$scratch/package/lib/$framework/$packageId.dll"
        $pdb = "$scratch/symbols/lib/$framework/$packageId.pdb"
        if (!(Test-Path $assembly) -or !(Test-Path $pdb)) { throw "Missing DLL/PDB for $framework." }
        $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($assembly).Version.ToString()
        $stableVersion = ($Version -split '-')[0]
        if ($assemblyVersion -ne "$stableVersion.0") { throw "Incorrect assembly version: $assemblyVersion" }
        $fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($assembly)
        if ($fileInfo.FileVersion -ne "$stableVersion.0" -or !$fileInfo.ProductVersion.Contains($Commit)) {
            throw "File/informational version mismatch for $framework."
        }
        $json = & dotnet sourcelink print-json $pdb
        if ($LASTEXITCODE -ne 0) { throw "Cannot read portable PDB for $framework." }
        $mapping = ($json -join "`n") | ConvertFrom-Json
        $urls = @($mapping.documents.PSObject.Properties.Value)
        $prefix = "https://raw.githubusercontent.com/erenken/queryPattern/$Commit/"
        if ($urls.Count -eq 0 -or @($urls | Where-Object { !$_.StartsWith($prefix, [System.StringComparison]::Ordinal) }).Count -gt 0) {
            throw "Source Link does not point exclusively to the exact build commit for $framework."
        }
        & dotnet sourcelink test $pdb
        if ($LASTEXITCODE -ne 0) { throw "Source downloads/checksums failed for $framework." }
        Write-Host "Validated packaged DLL, portable PDB, versions and source checksums: $framework"
    }
} finally {
    if ([System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($scratch)) -ne [System.IO.Path]::GetTempPath().TrimEnd('\', '/')) {
        throw 'Refusing to delete outside the temporary directory.'
    }
    [System.IO.Directory]::Delete($scratch, $true)
}
