using System.Security.Principal;
using HotspotControl.Windows;

using var identity = WindowsIdentity.GetCurrent();
if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.WriteLine("Bitte ohne Administratorrechte prüfen.");
    return 3;
}
var service = new HotspotService();
var original = await service.ReadAsync();
var stateLabel = original.State switch { HotspotState.On => "Eingeschaltet", HotspotState.Off => "Ausgeschaltet", HotspotState.InTransition => "Status wird geändert", _ => "Unbekannt" };
Console.WriteLine($"Lesezugriff: Zustand {stateLabel}; Geräte {original.Clients} / {original.MaximumClients}.");
if (!args.Contains("--network-cycle")) return original.ExitCode;
if (original.State is not (HotspotState.On or HotspotState.Off))
    throw new InvalidOperationException("Kein eindeutiger Ausgangszustand. Keine Änderung.");
var settings = await service.ReadSettingsAsync();
bool wasOn = original.State == HotspotState.On;
try
{
    var off = await service.SetEnabledAsync(false);
    if (!off.Success) throw new InvalidOperationException(off.Message);
    if ((await service.ReadAsync()).State != HotspotState.Off)
        throw new InvalidOperationException("Ausschalten wurde nicht bestätigt.");
    Console.WriteLine("OK: Ausschalten ohne Administratorrechte bestätigt.");

    // Reapply the same SSID and band; an empty password preserves the current one.
    var saved = await service.SaveSettingsAsync(settings.Ssid, "", settings.Band);
    if (!saved.Success) throw new InvalidOperationException(saved.Message);
    var reloaded = await service.ReadSettingsAsync();
    if (settings.Ssid != reloaded.Ssid || settings.Band != reloaded.Band)
        throw new InvalidOperationException("Einstellungen weichen vom Ausgangswert ab.");
    Console.WriteLine("OK: Konfiguration gespeichert; Name und Band unverändert, Passwort beibehalten.");

    var on = await service.SetEnabledAsync(true);
    if (!on.Success) throw new InvalidOperationException(on.Message);
    if ((await service.ReadAsync()).State != HotspotState.On)
        throw new InvalidOperationException("Einschalten wurde nicht bestätigt.");
    Console.WriteLine("OK: Einschalten ohne Administratorrechte bestätigt.");
}
finally
{
    // A timed-out native request must finish before a restoration can start.
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(35));
    while (service.IsBusy) await Task.Delay(200, deadline.Token);
    var restored = await service.SetEnabledAsync(wasOn);
    if (!restored.Success) throw new InvalidOperationException("Wiederherstellung fehlgeschlagen: " + restored.Message);
    var final = await service.ReadAsync();
    if (final.State != original.State) throw new InvalidOperationException("Ausgangszustand nicht wiederhergestellt.");
    Console.WriteLine("OK: Ursprünglicher Hotspot-Zustand wiederhergestellt.");
}
return 0;
