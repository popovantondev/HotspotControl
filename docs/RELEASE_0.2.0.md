# Prüfbericht 0.2.0

Geprüft am 9. September 2026 auf Windows 11, Build 26200, x64.
SDK für die Erstellung: 10.0.400. Alle Builds ohne Fehler und Warnungen.

## Bestätigt

| Bereich | Ergebnis |
| --- | --- |
| Automatisierte Regeln | 36 Prüfungen bestanden: Eingabegrenzen, Timeout, Sperren, verspätete Fehler, Abbruch, Wiederholungsgrenzen, Meldungserhalt |
| Eigenständige EXE | Direkt aus dem Paket gestartet; mitgelieferte .NET-Laufzeit, kein Nachbarprojekt erforderlich |
| Einzelexemplar | Zweiter Prozess beendet sich; vorhandenes Fenster wird aktiviert |
| Tray | Minimieren versteckt das Fenster; erneuter Start und tatsächlicher Klick auf das Tray-Symbol stellen es wieder her |
| Fenster | Power-Taste, Einstellungen und Geräte erreichbar; eigene Schließen-Knöpfe; DWM-Rundungspräferenz 2 |
| Programmende | Prozess und Tray-Ressourcen werden beendet |
| Autostart | Persönlichen Link aus-/eingeschaltet, neues EXE-Ziel geprüft und gespeicherten Link ausgeführt |
| Startpräferenz | Aus-/eingeschaltet und gespeicherte Werte geprüft; vorherige Benutzerauswahl wiederhergestellt |
| Hotspot ausschalten | Ohne Administratorrechte ausgeführt; ausgeschalteten Zustand anschließend gelesen |
| Konfiguration | Dieselbe SSID und dasselbe Band gespeichert und erneut verglichen; Passwort unverändert beibehalten |
| Hotspot einschalten | Ohne Administratorrechte ausgeführt; eingeschalteten Zustand gelesen |
| Wiederherstellung | Ursprünglich eingeschalteter Zustand nach Netzwerktest wiederhergestellt |

Die Verbindung der zwei Clients wurde für den Netzwerktest kurz unterbrochen.
Es wurden keine SSIDs, Passwörter oder Clientadressen in Testausgaben gespeichert.
Die Prüffunktion kontrolliert selbst, dass ihr Prozess nicht als Administrator läuft.

## Noch nicht praktisch abgedeckt

- Ein echter Windows-Neustart oder Ab-/Anmelden; der gespeicherte Startlink wurde
  direkt getestet. Der Benutzer wurde für diese Prüfung nicht abgemeldet.
- Andere Geräte, WLAN-Adapter, Gruppenrichtlinien und mehrere Monitore mit
  unterschiedlichen Skalierungen. Die Anwendung fordert native Windows-Rundungen an.
- Tatsächlich neues Passwort oder neuer Netzwerkname: bewusst nicht gesetzt, damit
  verbundene Geräte ihre vorhandenen Zugangsdaten behalten. Die Werteprüfung und
  der Windows-Konfigurationsaufruf wurden getestet.
- Dauerhafte native Hänger wurden über kontrollierte Tasks simuliert, nicht durch
  Manipulation des Netzwerktreibers. Ein Timeout kann den nativen Auftrag nicht zurücknehmen.

Die lokale Version ist als 0.2.0 eingeordnet, nicht als Garantie für sämtliche
Windows-Konfigurationen. Kein öffentlicher Upload und keine Code-Signierung.
