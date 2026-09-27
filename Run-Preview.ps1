param([string]$DotnetPath = 'dotnet', [string]$Language = '', [switch]$Compact, [string]$ExportPath = '')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'src\HotspotControl.Preview\HotspotControl.Preview.csproj'
& $DotnetPath build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$arguments = @()
if ($Language) { $arguments += @('--language', $Language) }
if ($Compact) { $arguments += '--compact' }
if ($ExportPath) { $arguments += @('--export', $ExportPath) }
& $DotnetPath (Join-Path $PSScriptRoot 'src\HotspotControl.Preview\bin\Release\net10.0-windows10.0.26100.0\HotspotControl.Preview.dll') @arguments
exit $LASTEXITCODE
