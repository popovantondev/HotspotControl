# Schritt 2 — erstes Fenster

Stand: 9. September 2026

Die WPF-Anwendung zeigt den Hotspot-Zustand, verbundene Geräte und den
Zeitpunkt der letzten Abfrage. Die Schaltfläche Aktualisieren liest erneut.
Die Oberfläche verwendet Deutsch. Die Windows-Abfrage läuft außerhalb des
UI-Threads. Gleichzeitige Abfragen werden verhindert. Bei Fehlern werden alte
Statuswerte verworfen und als unbekannt beziehungsweise nicht verfügbar angezeigt.

## Struktur

- HotspotControl.Windows: gemeinsamer, ausschließlich lesender Windows-Zugriff
  mit typisiertem Ergebnis, ohne UI-Abhängigkeit.
- HotspotControl.App: WPF-Fenster und Darstellung.
- HotspotControl.Probe: weiterhin nutzbare Konsolendiagnose mit Administratorprüfung.

## Starten

Auf diesem Rechner: Start-HotspotControl.cmd doppelklicken. Dieser lokale
Starter verwendet, falls vorhanden, die portable .NET-Laufzeit aus dem
Nachbarprojekt SlideFilter; ansonsten dotnet aus dem Suchpfad. Das ist noch
keine eigenständig verteilbare Veröffentlichung. Vorhandene Build-Dateien
müssen vorhanden sein.

Erneut bauen und starten mit einem .NET 10 SDK:

```powershell
.\Run-App.ps1 -DotnetPath 'C:\Pfad\zum\SDK\dotnet.exe'
```

## Validierung

- App und Probe: Release-Build ohne Fehler und Warnungen.
- Reales WPF-Fenster über Windows UI Automation gefunden.
- Nach Abfrage: Eingeschaltet; 2 / 8 Geräte.
- Aktualisieren über UI Automation ausgelöst; Zeitstempel aktualisiert,
  Ergebnis weiterhin Eingeschaltet und 2 / 8; Schaltfläche danach wieder aktiv.
- Probe mit gemeinsamem Windows-Modul erneut ausgeführt.

Noch nicht geprüft: ausgeschalteter Hotspot, erzwungener Zugriffsfehler,
anderer Rechner, unterschiedliche DPI-Einstellungen. Keine Änderung der
Netzwerkeinstellungen zum Testen. Keine visuelle Pixelprüfung durchgeführt.
