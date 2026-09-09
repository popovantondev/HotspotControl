# Architektur

- **HotspotControl.Core:** Von WPF und Windows unabhängige Regeln für begrenztes
  Warten, genau einen laufenden Auftrag, Autostartwiederholungen und Meldungen.
- **HotspotControl.Windows:** Zugriff auf `NetworkOperatorTetheringManager` und
  Übersetzung technischer Fehler in deutsche Meldungen. `HotspotService` wird
  von allen Fenstern gemeinsam verwendet.
- **HotspotControl.App:** WPF-Fenster, gemeinsamer Dialograhmen, Tray und persönliche
  Starteinstellungen. Native Fensterfunktionen sind in kleinen Hilfsklassen gekapselt.
- **HotspotControl.Probe:** Lesende Diagnose ohne Administratorrechte.

## Nebenläufigkeit

Eine Semaphore erlaubt nur einen Windows-Auftrag gleichzeitig. Der Auftrag läuft
außerhalb des UI-Threads. Lesen wartet höchstens acht Sekunden, Schreiben 25.
Nach einem Timeout hält der noch laufende Auftrag die Sperre weiterhin. Sein
späteres Ergebnis wird nicht als neue UI-Aktion angewendet, Fehler werden beobachtet.
Erst die nächste Statusabfrage liefert wieder einen aktuellen Zustand.

Der Autostart wird nur beim ersten Laden ausgeführt. Er wartet begrenzt auf die
Internetverbindung; Wiederherstellen aus dem Tray startet keinen neuen Versuch.
Manuelle Bedienung cancelt ausstehende Wiederholungen. Ein bereits an Windows
übergebener Auftrag ist damit nicht automatisch rückgängig gemacht.

Ein benutzer- und sitzungsbezogener Mutex schützt vor mehreren Instanzen.
Ein benanntes Event bittet die vorhandene Instanz, ihr Fenster wiederherzustellen.
Das Tray nutzt denselben Wiederherstellungspfad. Beim Beenden werden Timer,
Aktivierungsregistrierung und Tray-Ressourcen freigegeben.

## Teststrategie

Die Core-Regeln werden mit kontrollierten Tasks getestet: verspätete Antwort,
Timeout, Abbruch, erneuter Aufruf und verspäteter Fehler. Diese Tests berühren
keine Netzwerkeinstellungen. Eine separate Prüfung bedient das echte WPF-Fenster.
Der ausdrücklich aktivierbare Integrationstest verifiziert die Windows-Aktionen.
