# Anpassbare Fenstergröße

Stand: 9. September 2026

Das Fenster startet mit 480 x 820 und erlaubt mindestens 440 x 600.
Die Illustration nutzt den verbleibenden Platz. Status und Aktualisieren
bleiben sichtbar. Es gibt keinen ScrollViewer und keine Bildlaufleisten.
WindowChrome erhält Verschieben und Größenänderung über die Fensterränder.
Eigene Schaltflächen bieten Minimieren, Maximieren/Wiederherstellen und Schließen.
Alle Tooltips und zugänglichen Namen sind Deutsch.

Geprüft: Release-Build ohne Warnungen und Fehler; Aktualisieren im minimalen
Fenster erreichbar; Maximieren, Wiederherstellen, Minimieren und Schließen
über die echten UI-Schaltflächen erfolgreich. Keine ScrollBar-Elemente.
Das Fenster wurde nach der Prüfung erneut geöffnet.

Update: Fenstergröße vorübergehend auf 480 x 820 festgesetzt (ResizeMode=NoResize).
Maximieren-Schaltfläche entfernt. UI Automation bietet kein TransformPattern mehr an und bestätigt
CanMaximize=False sowie keine Maximieren-Schaltfläche.
