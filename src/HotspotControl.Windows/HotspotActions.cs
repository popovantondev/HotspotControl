using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace HotspotControl.Windows;

public sealed record NetworkSettings(string Ssid, int Band, IReadOnlyList<int> SupportedBands);
public sealed record OperationResult(bool Success, string Message);

public static class HotspotActions
{
    private static NetworkOperatorTetheringManager Manager()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile()
            ?? throw new InvalidOperationException("Keine Internetverbindung verfügbar.");
        return NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
    }

    public static NetworkSettings ReadSettings()
    {
        var config = Manager().GetCurrentAccessPointConfiguration();
        var bands = new[] { 0, 1, 2 }.Where(b => config.IsBandSupported((TetheringWiFiBand)b)).ToArray();
        return new(config.Ssid, (int)config.Band, bands);
    }

    public static IReadOnlyList<string> ReadDevices() => Manager().GetTetheringClients()
        .Select((client, index) => $"Gerät {index + 1}\n{string.Join(", ", client.HostNames.Select(host => host.DisplayName))}\nMAC: {client.MacAddress}")
        .ToArray();

    public static async Task<OperationResult> SetEnabledAsync(bool enabled)
    {
        try
        {
            var manager = Manager();
            var state = manager.TetheringOperationalState;
            if (state == TetheringOperationalState.InTransition)
                return new(false, "Windows ändert den Status bereits. Bitte kurz warten.");
            if (state == (enabled ? TetheringOperationalState.On : TetheringOperationalState.Off))
                return new(true, "Der gewünschte Status ist bereits aktiv.");
            var result = enabled ? await manager.StartTetheringAsync() : await manager.StopTetheringAsync();
            return result.Status == TetheringOperationStatus.Success
                ? new(true, enabled ? "Hotspot eingeschaltet." : "Hotspot ausgeschaltet.")
                : new(false, $"Windows konnte den Hotspot nicht umschalten (Fehlercode {(int)result.Status}).");
        }
        catch (Exception exception) { return Error(exception); }
    }

    public static string? ValidateSettings(string ssid, string password)
    {
        // ASCII keeps the network name consistent across Windows locales.
        if (string.IsNullOrWhiteSpace(ssid) || ssid.Length > 32 || ssid.Any(c => c < 32 || c > 126))
            return "Netzwerkname: 1–32 Zeichen, bitte ohne Umlaute oder Sonderzeichen außerhalb des ASCII-Zeichensatzes.";
        if (password.Length > 0 && (password.Length < 8 || password.Length > 63 || password.Any(c => c < 32 || c > 126)))
            return "Passwort: 8–63 druckbare ASCII-Zeichen. Leer lassen, um es beizubehalten.";
        return null;
    }

    public static async Task<OperationResult> SaveSettingsAsync(string ssid, string password, int band)
    {
        var validation = ValidateSettings(ssid, password);
        if (validation is not null) return new(false, validation);
        try
        {
            var manager = Manager();
            // Do not disconnect clients as a side effect of saving settings.
            if (manager.TetheringOperationalState != TetheringOperationalState.Off)
                return new(false, "Bitte zuerst den Hotspot ausschalten. Danach die Einstellungen speichern.");
            var config = manager.GetCurrentAccessPointConfiguration();
            if (!config.IsBandSupported((TetheringWiFiBand)band))
                return new(false, "Dieses Frequenzband wird vom Adapter nicht unterstützt.");
            config.Ssid = ssid;
            if (password.Length > 0) config.Passphrase = password;
            config.Band = (TetheringWiFiBand)band;
            await manager.ConfigureAccessPointAsync(config);
            return new(true, "Einstellungen gespeichert. Du kannst den Hotspot jetzt einschalten.");
        }
        catch (Exception exception) { return Error(exception); }
    }

    public static OperationResult Error(Exception exception) => new(false,
        exception is UnauthorizedAccessException
            ? "Windows erlaubt diese Aktion auf diesem Computer nicht."
            : $"Die Windows-Anfrage ist fehlgeschlagen (Fehlercode 0x{exception.HResult:X8}).");
}
