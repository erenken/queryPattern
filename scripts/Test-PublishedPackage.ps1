param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Commit,
    [switch]$AllowMissing,
    [int]$Attempts = 1,
    [int]$RetrySeconds = 10
)

$ErrorActionPreference = 'Stop'
$packageId = 'mynoc.entityframework.query'
$url = "https://api.nuget.org/v3-flatcontainer/$packageId/$Version/$packageId.$Version.nupkg"
$file = Join-Path ([System.IO.Path]::GetTempPath()) "$([guid]::NewGuid()).nupkg"
try {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Invoke-WebRequest $url -OutFile $file
        } catch {
            if ([int]$_.Exception.Response.StatusCode -ne 404) { throw }
            if ($AllowMissing) { Write-Host 'Version not published yet.'; return }
            if ($attempt -eq $Attempts) { throw 'Published package did not become available in time.' }
            Start-Sleep -Seconds $RetrySeconds
            continue
        }
        $archive = [System.IO.Compression.ZipFile]::OpenRead($file)
        try {
            $entry = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })
            if ($entry.Count -ne 1) { throw 'Expected one published package manifest.' }
            $reader = [System.IO.StreamReader]::new($entry[0].Open())
            try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $metadata = $manifest.package.metadata
            if ($metadata.id -ine $packageId -or $metadata.version -cne $Version -or $metadata.repository.commit -cne $Commit) {
                throw "NuGet version $Version belongs to another commit or has no verifiable provenance; refusing to create a mismatched release."
            }
        } finally { $archive.Dispose() }
        Write-Host "Published package $Version matches commit $Commit."
        return
    }
    throw 'No package verification attempt was made.'
} finally {
    if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
}
