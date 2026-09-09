param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
& $DotnetPath build (Join-Path $PSScriptRoot 'HotspotControl.slnx') -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $DotnetPath run --project (Join-Path $PSScriptRoot 'tests\HotspotControl.Checks\HotspotControl.Checks.csproj') -c Release --no-build
exit $LASTEXITCODE
