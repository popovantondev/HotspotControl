# Hotspot Control — Anforderungen der ersten Version

## Ziel

Eine kleine Windows-Anwendung zeigt den Zustand des bereits vorhandenen
Windows Mobile Hotspot verständlich an. Sie ist für einen normalen
Windows-Benutzer gedacht und benötigt zum Starten keine Administratorrechte.

## Erste Version (MVP)

- Anzeige, ob der Mobile Hotspot ein- oder ausgeschaltet ist.
- Anzeige nur der Informationen, die Windows für den angemeldeten Benutzer
  tatsächlich bereitstellt: zum Beispiel Netzwerkname, Band, Internetquelle
  und Anzahl verbundener Geräte.
- Anzeige, wenn eine Information nicht verfügbar ist; keine erfundenen Werte.
- Aktualisieren über eine sichtbare Schaltfläche.
- Klarer Hinweis, wenn Windows die benötigte Schnittstelle oder eine Funktion
  auf diesem Gerät nicht erlaubt.

## Nicht Teil der ersten Version

- Aktivieren oder Deaktivieren des Hotspots.
- Ändern von SSID, Passwort, Adapter, Firewall, NAT oder Treibern.
- Umgehen von Gruppenrichtlinien, UAC oder anderen Windows-Schutzmechanismen.
- Speichern personenbezogener Clientnamen, IP- oder MAC-Adressen.
- Cloud, Telemetrie oder Übertragung von Netzwerkdaten.

## Technische Leitlinien

- C# und eine native Windows-11-Oberfläche. Die erste technische Grundlage
  zielt auf Windows SDK 26100 oder neuer.
- Oberfläche, Fachlogik und Zugriff auf Windows getrennt halten.
- Die Windows-Schnittstelle zuerst mit einem kleinen, lesenden Probe-Programm
  prüfen. Erst danach die endgültliche Oberfläche bauen.
- Jede Windows-Abfrage ist fehlbar und liefert einen verständlichen Fehler
  statt eines Absturzes.
- Testbare Logik bleibt unabhängig von der Oberfläche.

## Öffentliche Veröffentlichung

- Keine persönlichen Pfade, SSIDs, Passwörter, Geräte-, IP- oder MAC-Adressen
  in Quellcode, Tests, Screenshots oder Dokumentation.
- Nur fiktive Beispieldaten verwenden.
- Dokumentation auf Deutsch bereitstellen;
  Deutsch ist die Standardsprache der Anwendung.
- Vor der ersten Veröffentlichung Lizenz, README, Screenshots mit Testdaten,
  `.gitignore`, Build-Anleitung und Tests prüfen.

## Fertig für den Probe-Schritt

Der nächste Schritt ist erfolgreich, wenn ein kleines Programm ohne Änderungen
am Netzwerk feststellt und dokumentiert, welche Informationen und Aktionen die
Windows-APIs auf diesem Rechner tatsächlich erlauben.

## Sprache

Alle sichtbaren Texte und Fehlermeldungen der Anwendung und der Diagnose sind
auf Deutsch. Kommentare im Quellcode werden in einfachem Englisch geschrieben.


## Erweiterung nach Nutzerfreigabe

Die ursprüngliche MVP-Abgrenzung oben beschreibt Schritt 1. Ab jetzt sind
Start/Stop, Änderung von SSID und Passwort, Auswahl eines unterstützten
Frequenzbands und Anzeige verbundener Geräte beauftragt. Gerätedaten werden
nur im Speicher angezeigt. Keine lokalen Protokolle mit Netzwerkdaten.
Die Einstellungen sind in Einfach und Erweitert gegliedert. Änderungen werden
nur bei ausgeschaltetem Hotspot gespeichert. Kein automatisches Abschalten
beim Öffnen oder Speichern der Einstellungen.
