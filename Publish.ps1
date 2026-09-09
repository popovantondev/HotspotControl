param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Run-Checks.ps1') -DotnetPath $DotnetPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$version = (Get-Content (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim()
$output = Join-Path $PSScriptRoot "artifacts\HotspotControl-$version-win-x64"
& $DotnetPath publish (Join-Path $PSScriptRoot 'src\HotspotControl.App\HotspotControl.App.csproj') -c Release -r win-x64 --self-contained true -o $output --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md'), (Join-Path $PSScriptRoot 'CHANGELOG.md') -Destination $output
$docs = Join-Path $output 'docs'
New-Item -ItemType Directory -Force -Path $docs | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\ARCHITECTURE.md'), (Join-Path $PSScriptRoot 'docs\RELEASE_0.2.0.md') -Destination $docs
$archive = "$output.zip"
Compress-Archive -LiteralPath $output -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath "$archive.sha256"
Write-Output "Lokales Paket erstellt: $archive"
