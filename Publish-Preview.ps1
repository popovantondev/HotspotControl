param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\HotspotControl.Preview\HotspotControl.Preview.csproj'
$destination = Join-Path $PSScriptRoot 'artifacts\HotspotControl-DesignPreview-win-x64'
& $DotnetPath publish $project -c Release -r win-x64 --self-contained true -o $destination --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output (Join-Path $destination 'HotspotControl.Preview.exe')
