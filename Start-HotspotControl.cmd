@echo off
set "APP=%~dp0artifacts\HotspotControl-0.2.0-win-x64\HotspotControl.App.exe"
if not exist "%APP%" (
  echo Das Programmpaket fehlt. Bitte zuerst Publish.ps1 ausfuehren.
  pause
  exit /b 1
)
start "" "%APP%"
