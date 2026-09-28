# Hotspot Control — Designvorschau

Eigenständige WPF-Demoversion auf Deutsch, Russisch und Englisch. Alle Netzwerkdaten sind erfunden. Die Vorschau steuert keinen echten Hotspot und schreibt weder Autostart-Einträge noch Benutzereinstellungen. Änderungen gelten nur im Arbeitsspeicher. Die eigentliche Anwendung bleibt davon getrennt.

## Starten

Mit dem .NET-10-SDK: `./Run-Preview.ps1`. Ein portables SDK kann mit `-DotnetPath <Pfad zu dotnet.exe>` angegeben werden.

Eine eigenständige x64-Version wird mit `./Publish-Preview.ps1` erstellt. Danach `artifacts/HotspotControl-DesignPreview-win-x64/HotspotControl.Preview.exe` starten. Ohne Sprachargument erscheint die Sprachauswahl bei jedem Demostart.

Das separate Demofenster kann Sprache, Szenario und kompakte Darstellung wechseln. Mit der Taste auf dem gezeichneten Laptop lässt sich der simulierte Hotspot umschalten. Geräte, Einstellungen und Fehlermeldungen verwenden denselben Fensterstil. Das Schließen des Hauptfensters beendet die Vorschau; diese Demoversion hat keinen echten Infobereich.

## Prüfen

`./Run-Preview.ps1 -ExportPath artifacts/preview-renders` prüft Übersetzungen und Demoabläufe und exportiert WPF-Renderings aller Sprachen und Szenarien. Diese Bilder sind keine Bildschirmaufnahmen des Desktops. Echte Windows-Skalierung, Tastaturbedienung und Fensterrahmen müssen zusätzlich am Bildschirm geprüft werden.
