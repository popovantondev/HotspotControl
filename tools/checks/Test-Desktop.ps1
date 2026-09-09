param([string]$AppPath = (Join-Path $PSScriptRoot '..\..\artifacts\HotspotControl-0.2.0-win-x64\HotspotControl.App.exe'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class DesktopCheckNative {
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
}
"@
function Assert-Check($condition, [string]$label) {
 if (!$condition) { throw "Prüfung fehlgeschlagen: $label" }
 Write-Output "OK: $label"
}
function Find-Id($parent, [string]$id) {
 $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
  [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id))
}
function Invoke-Id($parent, [string]$id) {
 (Find-Id $parent $id).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}
$app = Start-Process -FilePath $AppPath -ArgumentList '--no-auto-start' -PassThru
$window = $null
$deadline = (Get-Date).AddSeconds(20)
do {
 Start-Sleep -Milliseconds 200
 $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
  [System.Windows.Automation.TreeScope]::Children,
  [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$app.Id))
} until ($window -or (Get-Date) -gt $deadline)
Assert-Check ($null -ne $window) 'Eigenständige EXE startet'
Start-Sleep -Seconds 2
$handle = [IntPtr]$window.Current.NativeWindowHandle
$round = 0
$null = [DesktopCheckNative]::DwmGetWindowAttribute($handle, 33, [ref]$round, 4)
Assert-Check ($round -eq 2) 'Runde Fenster angefordert'
Assert-Check (!(Find-Id $window 'LaptopPowerButton').Current.IsOffscreen) 'Power-Taste sichtbar'
$second = Start-Process -FilePath $AppPath -ArgumentList '--no-auto-start' -PassThru
Assert-Check ($second.WaitForExit(5000)) 'Zweiter Prozess beendet sich'
Assert-Check ([DesktopCheckNative]::IsWindowVisible($handle)) 'Vorhandenes Fenster aktiviert'
Invoke-Id $window 'MinimizeButton'
Start-Sleep -Milliseconds 600
Assert-Check (![DesktopCheckNative]::IsWindowVisible($handle)) 'Fenster in den Infobereich minimiert'
$restore = Start-Process -FilePath $AppPath -ArgumentList '--no-auto-start' -PassThru
Assert-Check ($restore.WaitForExit(5000)) 'Erneuter Start erzeugt keinen weiteren Prozess'
Start-Sleep -Milliseconds 600
Assert-Check ([DesktopCheckNative]::IsWindowVisible($handle)) 'Verstecktes Fenster wiederhergestellt'
foreach ($entry in @(@('SettingsButton','Einstellungen'), @('DevicesButton','Verbundene Geräte'))) {
 $deadline = (Get-Date).AddSeconds(10)
 while (!(Find-Id $window $entry[0]).Current.IsEnabled -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 100 }
 Invoke-Id $window $entry[0]
 Start-Sleep -Seconds 1
 $dialog = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
  [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$entry[1]))
 Assert-Check ($null -ne $dialog) ($entry[1] + ' geöffnet')
 Assert-Check (!(Find-Id $dialog 'DialogCloseButton').Current.IsOffscreen) 'Eigener Schließen-Knopf sichtbar'
 if ($entry[0] -eq 'SettingsButton') {
  Assert-Check (!(Find-Id $dialog 'UserStartupCheckBox').Current.IsOffscreen) 'Autostart-Option sichtbar'
  Assert-Check (!(Find-Id $dialog 'AutoEnableHotspotCheckBox').Current.IsOffscreen) 'Automatischer Hotspotstart sichtbar'
 }
 Invoke-Id $dialog 'DialogCloseButton'
 Start-Sleep -Milliseconds 500
}
Invoke-Id $window 'CloseButton'
Assert-Check ($app.WaitForExit(5000)) 'Beenden räumt den Prozess auf'
'Keine Netzwerkänderungen durchgeführt.'
