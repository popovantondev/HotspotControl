# Hotspot Control — bestätigter Lesezugriff

Stand: 9. September 2026

Dieser Bericht ersetzt die Schlussfolgerung aus 02_PROBE_RESULT.md. Die frühere
Prüfung bestätigte keinen API-Zugriff; sie bewies aber auch keine Zugriffssperre.

## Tatsächlich geprüft

- Build mit vorhandenem portablem .NET SDK 10.0.400: 0 Fehler, 0 Warnungen.
- Ziel: net10.0-windows10.0.26100.0, normale Konsolenanwendung.
- Keine zusätzliche Workload-Installation erforderlich für diese Prüfung.
- Der Prozess prüft vor dem API-Aufruf, dass er nicht als Administrator läuft.
- NetworkInformation.GetInternetConnectionProfile und
  NetworkOperatorTetheringManager.CreateFromConnectionProfile funktionieren.
- Gemeldeter Zustand: On. ClientCount: 2. MaxClientCount: 8.
- Rückgabecode: 0.
- Keine Netzwerkeinstellungen verändert; keine SSIDs, Kennwörter, Namen,
  IP- oder MAC-Adressen abgefragt oder gespeichert.

## Wiederholen

Mit .NET 10 SDK im Suchpfad:

```powershell
.\Run-Probe.ps1
```

Alternativ den Pfad zu einem vorhandenen SDK übergeben:

```powershell
.\Run-Probe.ps1 -DotnetPath 'C:\Pfad\zum\SDK\dotnet.exe'
```

Der SDK-Pfad gehört zur lokalen Umgebung, nicht zur Anwendung. Das SDK im
Nachbarprojekt wurde für diesen Versuch verwendet und nicht verändert.

## Aufbau und Grenzen

Program.cs enthält Konsolenausgabe und die Prüfung der Administratorrechte.
HotspotReader.cs kapselt den ausschließlich lesenden Windows-Zugriff.
Fehler liefern einen verständlichen Status und einen Rückgabecode; der Zustand
wird bei Fehlern ausdrücklich als unbekannt ausgewiesen.

Exitcodes: 0 Erfolg, 2 kein Internetprofil, 3 administrativer Start,
4 Zugriff verweigert, 5 unterstützter API-Fehler.

Nur der aktuelle Zustand mit eingeschaltetem Hotspot wurde praktisch geprüft.
Kein automatisches Umschalten zur Prüfung: Die bestehende Verbindung soll
nicht unterbrochen werden. Fehlerfälle und andere Geräte sind noch nicht
praktisch validiert. Start/Stop, SSID, Band und Geräteinformationen sind nicht
Teil dieses Nachweises. Der gewählte Manager gehört zum aktuellen Internetprofil.

Nächster Schritt: Eine kleine Oberfläche mit Status, Clientanzahl und
Aktualisieren, auf Grundlage dieses bestätigten Lesezugriffs.

## Quellen

- https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps
- https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators.networkoperatortetheringmanager
