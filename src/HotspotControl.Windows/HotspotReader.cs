using System.Runtime.InteropServices;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace HotspotControl.Windows;

public enum HotspotState { Unknown, On, Off, InTransition }
public sealed record HotspotSnapshot(HotspotState State, uint? Clients, uint? MaximumClients, int ExitCode, string Message);

public static class HotspotReader
{
    public static HotspotSnapshot Read()
    {
        try
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile is null)
                return Failure(2, "Kein Internet-Verbindungsprofil verfügbar.");
            var manager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
            var state = manager.TetheringOperationalState switch
            {
                TetheringOperationalState.On => HotspotState.On,
                TetheringOperationalState.Off => HotspotState.Off,
                TetheringOperationalState.InTransition => HotspotState.InTransition,
                _ => HotspotState.Unknown
            };
            return new(state, manager.ClientCount, manager.MaxClientCount, 0,
                "Daten für das aktuelle Internet-Verbindungsprofil.");
        }
        catch (UnauthorizedAccessException)
        {
            return Failure(4, "Windows verweigert den Zugriff auf den Hotspot-Status.");
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException or NotSupportedException or ArgumentException)
        {
            // Do not expose exception messages that may contain local network details.
            return Failure(5, $"Abfrage nicht verfügbar (Fehlercode 0x{exception.HResult:X8}).");
        }
    }

    private static HotspotSnapshot Failure(int code, string message) =>
        new(HotspotState.Unknown, null, null, code, message);
}

