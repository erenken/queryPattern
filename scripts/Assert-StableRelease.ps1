param(
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Branch
)

if ($Branch -cne 'main' -or $Version -cnotmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$') {
    throw "Only stable major.minor.patch versions from main may publish: $Branch / $Version"
}
