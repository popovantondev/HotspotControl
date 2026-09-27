param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Run-Checks.ps1') -DotnetPath $DotnetPath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$version = (Get-Content (Join-Path $PSScriptRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid VERSION value.' }
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'artifacts'))
$output = [IO.Path]::GetFullPath((Join-Path $artifactRoot "HotspotControl-$version-win-x64"))
if ([IO.Path]::GetDirectoryName($output) -ne $artifactRoot) { throw 'Invalid output path.' }
if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
& $DotnetPath publish (Join-Path $PSScriptRoot 'src\HotspotControl.App\HotspotControl.App.csproj') -c Release -r win-x64 --self-contained true -o $output --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md'), (Join-Path $PSScriptRoot 'CHANGELOG.md'), (Join-Path $PSScriptRoot 'RIGHTS.md'), (Join-Path $PSScriptRoot 'THIRD_PARTY_NOTICES.md'), (Join-Path $PSScriptRoot 'VERSION') -Destination $output
$docs = Join-Path $output 'docs'
New-Item -ItemType Directory -Force -Path $docs | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\ARCHITECTURE.md'), (Join-Path $PSScriptRoot 'docs\RELEASE_0.3.0.md'), (Join-Path $PSScriptRoot 'docs\README.de.md'), (Join-Path $PSScriptRoot 'docs\README.ru.md') -Destination $docs
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\images') -Destination $docs -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'licenses') -Destination $output -Recurse
$archive = "$output.zip"
Compress-Archive -LiteralPath $output -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath "$archive.sha256"
Write-Output "Lokales Paket erstellt: $archive"
