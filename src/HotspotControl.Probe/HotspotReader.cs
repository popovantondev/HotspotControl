namespace HotspotControl.Probe;

internal sealed record ProbeResult(int ExitCode, string Message);
internal static class HotspotReader
{
    public static ProbeResult Read()
    {
        var snapshot = HotspotControl.Windows.HotspotReader.Read();
        var clients = snapshot.Clients.HasValue ? $"{snapshot.Clients} / {snapshot.MaximumClients}" : "Unbekannt";
        var state = snapshot.State switch
        {
            HotspotControl.Windows.HotspotState.On => "Eingeschaltet",
            HotspotControl.Windows.HotspotState.Off => "Ausgeschaltet",
            HotspotControl.Windows.HotspotState.InTransition => "Status wird geändert",
            _ => "Unbekannt"
        };
        return new(snapshot.ExitCode, $"Hotspot: {state}\nVerbundene Geräte: {clients}\n{snapshot.Message}");
    }
}
