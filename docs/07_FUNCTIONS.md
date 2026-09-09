# Steuerung, Einstellungen und Geräte

Stand: 9. September 2026

## Bedienung

- Die beleuchtete Taste auf dem Laptop schaltet den Hotspot ein oder aus.
  Ausschalten trennt verbundene Geräte. Während einer Anfrage ist sie gesperrt.
- Einstellungen / Einfach: Netzwerkname und neues Passwort. Ein leeres
  Passwortfeld behält das bisherige Passwort. Das aktuelle Passwort wird nicht angezeigt.
- Einstellungen / Erweitert: Automatisch, 2,4 GHz und 5 GHz, soweit unterstützt.
- Speichern erfordert einen ausgeschalteten Hotspot. Keine automatische Trennung.
- Geräte: von Windows gelieferte Adressen, seitenweise mit Zurück und Weiter.
  Namen/Adressen bleiben nur im Arbeitsspeicher; keine Protokolldateien.

## Validierung

Release-Build: keine Warnungen oder Fehler. Neun Prüfungen der SSID- und
Passwortgrenzen bestanden. Im echten Fenster: Power-Taste sichtbar und aktiv,
Einstellungen erfolgreich geladen, Gerätedaten erfolgreich angezeigt.
Hauptfenster visuell geprüft. Netzwerkdaten wurden nicht in Prüfausgaben übernommen.
Start/Stop und das tatsächliche Speichern wurden bewusst nicht ausgeführt,
um die zwei aktiven Verbindungen nicht zu unterbrechen. Diese Funktionen
sind implementiert, ihre Freigabe durch Windows ist noch praktisch zu prüfen.

## Grenzen

Kein Geschwindigkeitslimit pro Gerät: Die verwendete öffentliche Hotspot-API
bietet dafür keine Methode. Keine Treiber-, Firewall- oder Administratoränderungen.
Die aktuelle Internetverbindung bestimmt den verwendeten Hotspot-Manager.
Die Geräteansicht zeigt nur die Informationen, die Windows tatsächlich liefert.

Quellen:
https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators.networkoperatortetheringmanager
https://learn.microsoft.com/en-us/uwp/api/windows.networking.networkoperators.networkoperatortetheringaccesspointconfiguration
