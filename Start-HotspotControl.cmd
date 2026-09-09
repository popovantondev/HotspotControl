@echo off
set "APP=%~dp0src\HotspotControl.App\bin\Release\net10.0-windows10.0.26100.0\HotspotControl.App.dll"
set "LOCAL_DOTNET=%~dp0..\SlideFilter\.tools\dotnet\dotnet.exe"
if exist "%LOCAL_DOTNET%" (
  start "" "%LOCAL_DOTNET%" "%APP%"
) else (
  start "" dotnet "%APP%"
)
