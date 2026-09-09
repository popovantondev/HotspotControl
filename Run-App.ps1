param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'src\HotspotControl.App\HotspotControl.App.csproj'
& $DotnetPath build $projectPath -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$assemblyPath = Join-Path $PSScriptRoot 'src\HotspotControl.App\bin\Release\net10.0-windows10.0.26100.0\HotspotControl.App.dll'
& $DotnetPath $assemblyPath
exit $LASTEXITCODE
