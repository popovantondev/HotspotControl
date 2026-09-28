# Hotspot Control — Design Preview

Separate WPF demonstration app in German, Russian and English. All network data is fictional. The preview does not control a real hotspot or write startup entries or user preferences. Changes remain in memory. The production app is separate.

## Run

Use the .NET 10 SDK: `./Run-Preview.ps1`. A portable SDK can be selected with `-DotnetPath <path to dotnet.exe>`.

Create a self-contained x64 app with `./Publish-Preview.ps1`. Then run `artifacts/HotspotControl-DesignPreview-win-x64/HotspotControl.Preview.exe`. Without a language argument, the language picker appears on each demo launch.

The separate demo window can switch language, scenario and compact mode. The button on the illustrated laptop toggles the simulated hotspot. Devices, settings and errors use the same window style. Closing the main window exits the preview; this demo has no real tray icon.

## Verify

`./Run-Preview.ps1 -ExportPath artifacts/preview-renders` checks translations and demo flows, then exports WPF renders for all languages and scenarios. These images are not desktop screenshots. Actual Windows scaling, keyboard interaction and native window chrome need additional on-screen checks.
