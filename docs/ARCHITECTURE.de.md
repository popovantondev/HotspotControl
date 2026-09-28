# Architektur

`HotspotControl.Core` enthält sprachunabhängige Verträge, Eingabevalidierung, Operationsserialisierung und Wiederholungsregeln. `HotspotControl.Windows` bindet die Windows-Mobile-Hotspot-API an und überführt ihre Ergebnisse in diese Verträge. `HotspotControl.Localization` ordnet Ergebniscodes und UI-Schlüssel Deutsch, Russisch und Englisch zu. `HotspotControl.Presentation` stellt gemeinsame WPF-Ansichten bereit. `HotspotControl.App` verbindet sie mit Windows-Netzwerkfunktionen, Einstellungen, Einzelinstanz-Aktivierung und Infobereich.

`HotspotControl.Preview` verwendet dieselben Ansichten mit einem Demo-Dienst im Speicher. Es verweist weder auf die Produktionsprojekte Windows/App noch greift es auf echte Netzwerkeinstellungen oder Benutzereinstellungen zu. `HotspotControl.Probe` liest Diagnosedaten, ohne sie zu ändern.

Windows-Operationen laufen nacheinander. Nach einem Zeitlimit endet die Wartezeit der Oberfläche, die Windows-Operation kann jedoch weiterlaufen; ein verspätetes Ergebnis wird nicht als neuer Erfolg gemeldet. Eine erneute Statusabfrage stellt den aktuellen Zustand fest. Manuelle Befehle brechen ausstehende automatische Startwiederholungen ab. Ein Koordinator pro Benutzer stellt ein vorhandenes Fenster wieder her, wenn die ausführbare Datei erneut gestartet wird.

Einstellungen übernehmen das Autostart-Flag aus Version 0.2.0, ergänzen die Sprache und weisen beschädigte oder zukünftige Schemata sicher zurück. Netzwerkpasswörter werden beim Speichern an Windows übergeben und nicht in der Einstellungsdatei der App gespeichert. Die Windows-Startverknüpfung gilt nur für den aktuellen Benutzer.

`Run-Checks.ps1` erstellt den Build und führt Verhaltensprüfungen aus, ohne das Netzwerk zu ändern. Vorschauprüfungen und Renderings verwenden fiktive Daten. Integrationstests, die den echten Hotspot ändern, benötigen ein ausdrücklich vereinbartes Testfenster. Nummerierte ältere Dokumente beschreiben Entwurf und Prüfung der früheren Version 0.2.0, nicht die aktuellen Aussagen zu 0.3.0.
