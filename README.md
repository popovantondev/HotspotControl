# Hotspot Control

Eine kleine deutsche Windows-Anwendung für den eingebauten mobilen Hotspot.
Sie zeigt Zustand und verbundene Geräte, schaltet die Freigabe um und bietet
einfache Einstellungen. Die Bedienoberfläche besteht aus skalierbaren WPF-Vektoren.

## Starten

1. Das Paket `HotspotControl-0.2.0-win-x64.zip` vollständig entpacken.
2. Im entpackten Ordner `HotspotControl.App.exe` öffnen.
3. Der Power-Knopf auf dem Laptop in der Illustration schaltet den Hotspot um.

Die gesamte Paketstruktur zusammenlassen. Das Paket enthält seine .NET-Laufzeit;
SDK, Visual Studio und Administratorrechte sind zum Starten nicht erforderlich.
Voraussetzung: Windows 11 ab Build 26100 (24H2), x64 und ein geeigneter WLAN-Adapter.
Geräterichtlinien können einzelne Hotspot-Aktionen einschränken. Geprüft auf Build 26200.

## Bedienung

- **Aktualisieren:** Zustand lesen und die letzte Meldung quittieren.
  Im Hintergrund wird alle zehn Sekunden aktualisiert, ohne die Anzeige zu leeren.
- **Power-Taste:** Freigabe ein-/ausschalten. Ausschalten trennt die verbundenen Geräte.
- **Geräte:** Von Windows gelieferte Namen/Adressen anzeigen; mit Zurück/Weiter blättern.
- **Einstellungen / Einfach:** Netzwerkname und neues Passwort.
  Ein leeres Passwortfeld behält das bisherige Passwort. Speichern ist nur bei
  ausgeschaltetem Hotspot möglich; die App schaltet ihn dafür nicht eigenständig aus.
- **Erweitert:** Vom Adapter unterstütztes Frequenzband wählen.
- **Mit Windows starten:** Benutzerbezogener Autostart ohne Administratorrechte.
- **Hotspot beim App-Start einschalten:** Unabhängige Option. Bei einer noch nicht
  bereiten Verbindung maximal zwölf Versuche mit fünf Sekunden Pause. Dauerhafte
  Fehler werden nicht wiederholt. Manuelles Handeln beendet ausstehende Wiederholungen.
- **Minimieren:** In den Infobereich ausblenden. Linksklick auf das Symbol oder
  „Öffnen“ im Kontextmenü bringt das Fenster zurück. Erneuter Programmstart ebenfalls.
- **Schließen / Beenden:** App und Tray-Symbol schließen; die Windows-Freigabe bleibt
  unverändert. Das Fenster lässt sich absichtlich nicht manuell vergrößern.

Im Tray: grüne Wellen = eingeschaltet; graue Wellen = ausgeschaltet oder unbekannt.
Der Tooltip unterscheidet ausgeschaltet, unbekannt und laufenden Wechsel.
Eine Fehlermeldung bleibt bis zur nächsten Benutzeraktion sichtbar.

## Daten und Grenzen

Keine Telemetrie, Cloud oder lokalen Protokolle mit SSIDs, Passwörtern oder
Geräteadressen. Nur die Option für den automatischen Hotspotstart wird in
`%LOCALAPPDATA%\HotspotControl\preferences.json` gespeichert.
Der Windows-Autostart verwendet `HotspotControl.UserStartup.lnk` im persönlichen
Autostartordner. Nach dem Verschieben der App den Autostart aus- und einschalten,
damit der Link auf den neuen Ort zeigt. Vor dem Entfernen der App Autostart deaktivieren.

Das Windows-API bietet keine Geschwindigkeitsbegrenzung pro Client. Eine fehlende
IP-Adresse oder ein fehlender Gerätename wird nicht erfunden. Die App verwendet
das aktuelle Internet-Verbindungsprofil von Windows.

Ein Timeout beendet das Warten der Oberfläche, nicht zwingend den Windows-Auftrag.
Bis dessen Ende bleiben weitere Windows-Aufträge gesperrt. In dieser Situation
zuerst den Status prüfen; keine Erfolgsannahme aus einer abgelaufenen Wartezeit.

## Entwickeln

.NET 10 SDK auf Windows erforderlich. Oberfläche und Fehler sind Deutsch;
Codekommentare in einfachem Englisch. Kein Bezug auf einen anderen Projektordner
ist zum Bauen erforderlich.

```powershell
.\Run-App.ps1
.\Run-Checks.ps1
.\Publish.ps1
```

Ein portables SDK kann mit `-DotnetPath 'C:\Werkzeuge\dotnet\dotnet.exe'` übergeben
werden. Die erste Wiederherstellung lädt Microsoft-Pakete aus NuGet.
`Publish.ps1` erstellt nur ein lokales Paket und lädt nichts hoch.

## Prüfungen

- `Run-Checks.ps1`: Build und 36 automatisierte Prüfungen ohne Netzwerkänderung.
- `tools\checks\Test-Desktop.ps1`: echte UI-Prüfung der fertigen EXE; ohne automatischen
  Hotspotstart. Vorher die App schließen. Prüft Wiederherstellung, Einzelexemplar,
  Dialoge und Beenden. Während der Prüfung die UI nicht parallel bedienen.
- `HotspotControl.Probe`: weiterhin rein lesende Diagnose.
- `HotspotControl.Integration`: standardmäßig rein lesend. Nur mit bewusst gesetztem
  `--network-cycle` schaltet die Prüfung die Freigabe aus/ein, schreibt dieselben
  Einstellungen zurück und stellt den ursprünglichen Zustand wieder her.

Die Option `--no-auto-start` an der EXE unterdrückt den automatischen Hotspotstart
für diesen Aufruf, ohne die gespeicherte Auswahl zu ändern.

Siehe [Architektur](docs/ARCHITECTURE.md), [Prüfbericht](docs/RELEASE_0.2.0.md) und
[Änderungen](CHANGELOG.md). Frühere nummerierte Berichte dokumentieren Zwischenstände.

## Demonstration in drei Minuten

1. Benutzerproblem erklären: eine übersichtliche Oberfläche für Windows-Hotspot.
2. Zustand, Geräteliste, einfache/erweiterte Einstellungen zeigen.
3. Minimieren und Wiederherstellen, danach die Schichten und Timeout-Tests erklären.

Für öffentliche Aufnahmen nur Testnetzwerke und fiktive Gerätedaten verwenden.
Dieses Repository wird lokal geführt; eine öffentliche Veröffentlichung und
deren Lizenz sind ein gesonderter Schritt.
