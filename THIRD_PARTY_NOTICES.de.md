# Hinweise zu Drittanbietern

Der eigenständige Windows-Build enthält Komponenten der Microsoft-.NET-10-Laufzeit und der Windows-Desktop-Laufzeit. Für sie gelten weiterhin die jeweiligen Bedingungen. Kopien der Lizenz- und Hinweisdokumente aus den für diesen Build verwendeten Laufzeitpaketen 10.0.11 liegen im Verzeichnis `licenses/` und im ZIP:

- `licenses/dotnet-runtime-LICENSE.txt`
- `licenses/dotnet-runtime-THIRD-PARTY-NOTICES.txt`
- `licenses/windowsdesktop-runtime-LICENSE.txt`

Die App verwendet von Windows bereitgestellte Netzwerk-APIs. Es werden weder externe Grafikdateien noch UI-Bibliotheken von Drittanbietern mitgeliefert. Das WLAN-Symbol wird von `tools/Build-Icon.ps1` erzeugt; Person, Laptop und Signal bestehen aus WPF-Vektorpfaden in `src/HotspotControl.App/Assets/Scene.xaml`. Die Grafiken wurden für dieses Projekt nach den visuellen Vorgaben des Eigentümers erstellt. Wenn die Lizenz des visuellen Referenzbilds unklar ist, sollte dessen Herkunft vor einer öffentlichen Veröffentlichung geprüft werden.

Die genannten Laufzeitdateien gelten für die konkrete Build-Version. Bei einem Upgrade von .NET oder geänderten Paketabhängigkeiten müssen sie erneut geprüft werden. Siehe [Lizenzinformationen zu .NET](https://github.com/dotnet/core/blob/main/license-information.md).
