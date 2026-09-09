# Hotspot Control — Umgebung und API-Risiko

Stand: 8. September 2026

## Festgestellte Umgebung

- Windows Build `10.0.26200`, x64.
- Der Computer hat nur .NET-Runtimes systemweit; ein systemweiter SDK ist nicht
  vorhanden.
- In einem bestehenden, anderen Projekt liegt ein portables .NET SDK 10.0.400.
  Es wird nicht als Abhängigkeit von Hotspot Control übernommen, bevor die
  Projektstruktur entschieden ist.

## Ergebnis der Recherche

Windows stellt `NetworkOperatorTetheringManager` für Mobile Hotspot bereit. Die
offizielle Schnittstelle kann den Betriebszustand sowie die Anzahl und das
Maximum verbundener Clients liefern. Microsoft nennt jedoch die App-Fähigkeit
`wiFiControl` als Voraussetzung. Dadurch ist nicht sicher, ob eine normale
Desktop-Anwendung auf diesem Schulcomputer den Manager öffnen darf. Für .NET 10
wird dafür das moderne Zielpaket `Microsoft.Windows.SDK.NET.Ref` verwendet;
das ältere `Microsoft.Windows.SDK.Contracts` verweist direkt auf WinMD-Dateien
und lässt sich hier nicht bauen.

Die erste technische Prüfung muss deshalb nur lesend sein. Sie darf weder den
Hotspot starten oder stoppen noch SSID, Kennwort, Adapter, Firewall oder andere
Netzwerkeinstellungen ändern.

## Entscheidung für den nächsten Schritt

Ein minimaler C#-Probe wird gebaut, der ausschließlich versucht:

1. die aktuelle Verbindung zu ermitteln;
2. den Tethering-Manager zu öffnen;
3. Verfügbarkeit, Betriebszustand und Client-Anzahl auszugeben;
4. jede Ausnahme und jede fehlende Berechtigung als verständliches Ergebnis zu
   melden.

Wenn Windows den Zugriff verweigert, bleibt das Ergebnis wertvoll: Wir wissen
früh und reproduzierbar, dass der geplante Funktionskern auf diesem Gerät nicht
verlässlich möglich ist. Dann entscheiden wir zwischen einem reinen
Netzwerkstatus-Viewer und dem BackUp Manager als erstem öffentlichen Projekt.

Quellen: Microsoft Learn, `NetworkOperatorTetheringManager` und
`Windows.Networking.NetworkOperators`, abgerufen am 8. September 2026.
