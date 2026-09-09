using HotspotControl.Core;

namespace HotspotControl.Windows;

// All windows share this service, so settings cannot overlap a power command.
public sealed class HotspotService
{
    private readonly OperationRunner operations = new();
    public bool IsBusy => operations.IsBusy;

    public Task<HotspotSnapshot> ReadAsync(CancellationToken token = default) =>
        operations.RunAsync(() => Task.FromResult(HotspotReader.Read()), TimeSpan.FromSeconds(8), token);
    public Task<NetworkSettings> ReadSettingsAsync(CancellationToken token = default) =>
        operations.RunAsync(() => Task.FromResult(HotspotActions.ReadSettings()), TimeSpan.FromSeconds(8), token);
    public Task<IReadOnlyList<string>> ReadDevicesAsync(CancellationToken token = default) =>
        operations.RunAsync(() => Task.FromResult(HotspotActions.ReadDevices()), TimeSpan.FromSeconds(8), token);
    public Task<OperationResult> SetEnabledAsync(bool enabled, CancellationToken token = default) =>
        operations.RunAsync(() => HotspotActions.SetEnabledAsync(enabled), TimeSpan.FromSeconds(25), token);
    public Task<OperationResult> SaveSettingsAsync(string ssid, string password, int band, CancellationToken token = default) =>
        operations.RunAsync(() => HotspotActions.SaveSettingsAsync(ssid, password, band), TimeSpan.FromSeconds(25), token);

    public async Task<StartAttempt> TryAutoStartAsync(CancellationToken token)
    {
        try
        {
            var status = await ReadAsync(token);
            if (status.ExitCode == 2) return new(false, true, "Warten auf die Internetverbindung.");
            if (status.ExitCode != 0) return new(false, false, status.Message);
            if (status.State == HotspotState.On) return new(true, false, "Hotspot ist bereits eingeschaltet.");
            if (status.State != HotspotState.Off) return new(false, true, "Windows bereitet die Verbindung vor.");
            var result = await SetEnabledAsync(true, token);
            return new(result.Success, result.Retryable, result.Message);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            var result = HotspotActions.Error(exception);
            return new(false, exception is OperationBusyException, result.Message);
        }
    }
}
