# Hotspot Control — Ergebnis des technischen Probe

Stand: 8. September 2026

## Ergebnis

Der geplante Probe konnte den Mobile-Hotspot-API-Zugriff auf diesem
Schulcomputer nicht bestätigen.

- Microsoft dokumentiert für `NetworkOperatorTetheringManager` die
  App-Fähigkeit `wiFiControl`.
- Die aktuelle .NET-Desktop-Umgebung enthält keinen systemweiten SDK.
- Der portable .NET-10-SDK kann ein gewöhnliches Console-Projekt bauen, stellt
  den modernen Windows-SDK-Workload für die erforderliche WinRT-Projektion
  aber nicht bereit.
- Ein letzter, reiner Leseversuch über Windows PowerShell konnte den
  `Windows.Networking.Connectivity`-Typ nicht laden.

Die Prüfung hat keine Hotspot- oder Netzwerkeinstellung verändert und keine
Netzwerkdaten gespeichert.

## Konsequenz

Hotspot Control wird nicht als erstes Hauptprojekt begonnen. Für ein
zuverlässiges Portfolio-Projekt müsste vorher eine unterstützte
Windows-App-SDK-Umgebung und die tatsächliche Verfügbarkeit von `wiFiControl`
geklärt werden. Das ist auf einem verwalteten Schulcomputer keine sichere
Voraussetzung.

Ein möglicher späterer Ersatz ist ein allgemeiner Netzwerkstatus-Viewer ohne
Hotspot-Steuerung. Er wäre jedoch ein anderes Projekt und wird nicht ohne neue
Entscheidung begonnen.
