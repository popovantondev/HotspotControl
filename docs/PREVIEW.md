# Hotspot Control — Design Preview

Separate WPF-Demoversion mit Deutsch, Russisch und Englisch. Alle Netzwerkdaten
sind erfunden. Die Vorschau steuert keinen echten Hotspot und schreibt weder
Autostart-Einträge noch Benutzereinstellungen. Änderungen gelten nur im Speicher.
Die bestehende Anwendung bleibt separat.

## Starten

Mit dem .NET 10 SDK: `./Run-Preview.ps1`. Ein portables SDK kann über
`-DotnetPath <Pfad zu dotnet.exe>` angegeben werden.

Eine eigenständig lauffähige x64-Version erstellt `./Publish-Preview.ps1`.
Danach `artifacts/HotspotControl-DesignPreview-win-x64/HotspotControl.Preview.exe`
starten. Ohne Sprachargument erscheint die Sprachauswahl bei jedem Demostart.

Das separate Demo-Fenster wechselt Sprache, Szenario und kompakte Darstellung.
Mit der Taste auf dem gezeichneten Laptop lässt sich der simulierte Hotspot
umschalten. Geräte, Einstellungen und Fehler verwenden dieselbe Fenstergestaltung.
Das Schließen des Hauptfensters beendet die Vorschau; ein echter Tray gehört
nicht zu dieser Demoversion.

## Prüfen

`./Run-Preview.ps1 -ExportPath artifacts/preview-renders` prüft Übersetzungen und
Demoabläufe und exportiert WPF-Renderings aller Sprachen und Szenarien.
Diese Bilder sind keine Desktop-Screenshots. Echte Windows-Skalierung,
Tastaturbedienung und Fensterrahmen müssen zusätzlich am Bildschirm geprüft werden.
