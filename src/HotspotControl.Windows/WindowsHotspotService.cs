using System.Runtime.InteropServices;
using HotspotControl.Core;
using HotspotControl.Core.Contracts;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using ContractResult = HotspotControl.Core.Contracts.OperationResult;

namespace HotspotControl.Windows;

// This adapter is the sole WinRT boundary for the new presentation contract.
public sealed class WindowsHotspotService : IHotspotService
{
    private readonly OperationRunner runner = new();
    private readonly object stateLock = new();
    private OperationState state;
    private sealed class UnknownProfileContextException : Exception;
    public event EventHandler? StateChanged;
    public OperationState OperationState { get { lock (stateLock) return state; } }

    public WindowsHotspotService() => runner.BecameIdle += (_, _) => ChangeState(OperationState.Idle);

    private void ChangeState(OperationState next)
    {
        lock (stateLock) state = next;
        Notify();
    }

    private void Notify()
    {
        foreach (EventHandler observer in StateChanged?.GetInvocationList().Cast<EventHandler>() ?? [])
        {
            try { observer(this, EventArgs.Empty); }
            catch { /* A view observer cannot change the Windows result. */ }
        }
    }

    private async Task<T> Run<T>(Func<Task<T>> action, OperationState next, TimeSpan limit, CancellationToken token)
    {
        lock (stateLock)
        {
            if (state != OperationState.Idle || runner.IsBusy) throw new OperationBusyException();
            state = next;
        }
        Notify();
        try { return await runner.RunAsync(action, limit, token); }
        catch (TimeoutException)
        {
            if (runner.IsBusy) ChangeState(OperationState.PendingAfterTimeout);
            throw;
        }
        finally { if (!runner.IsBusy) ChangeState(OperationState.Idle); }
    }

    private static (ConnectionProfile Profile, NetworkOperatorTetheringManager Manager) Current()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile() ??
            throw new InvalidOperationException("No internet profile");
        return (profile, NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile));
    }

    public static ContractResult DescribeError(Exception error) => error switch
    {
        OperationBusyException => new(false, MessageCode.Busy, true),
        TimeoutException => new(false, MessageCode.TimedOut, true),
        OperationCanceledException => new(false, MessageCode.Cancelled),
        UnknownProfileContextException => new(false, MessageCode.ContextChanged),
        UnauthorizedAccessException => new(false, MessageCode.AccessDenied),
        InvalidOperationException => new(false, MessageCode.NoInternetProfile, true),
        NotSupportedException => new(false, MessageCode.NotSupported),
        COMException => new(false, MessageCode.NativeFailure, false, error.HResult),
        _ => new(false, MessageCode.NativeFailure, false, error.HResult)
    };

    private static async Task<ServiceResult<T>> Read<T>(Func<T> read, Func<Func<Task<ServiceResult<T>>>, CancellationToken, Task<ServiceResult<T>>> run, CancellationToken token)
    {
        try { return await run(() => Task.FromResult(new ServiceResult<T>(read(), new(true, MessageCode.Ready))), token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error) { return new(default, DescribeError(error)); }
    }

    public Task<ServiceResult<HotspotControl.Core.Contracts.HotspotSnapshot>> ReadAsync(CancellationToken token = default) =>
        Read(() =>
        {
            var (_, manager) = Current();
            var state = manager.TetheringOperationalState switch
            {
                TetheringOperationalState.On => HotspotControl.Core.Contracts.HotspotState.On,
                TetheringOperationalState.Off => HotspotControl.Core.Contracts.HotspotState.Off,
                TetheringOperationalState.InTransition => HotspotControl.Core.Contracts.HotspotState.InTransition,
                _ => HotspotControl.Core.Contracts.HotspotState.Unknown
            };
            return new HotspotControl.Core.Contracts.HotspotSnapshot(state, manager.ClientCount, manager.MaxClientCount);
        }, (action, ct) => Run(action, OperationState.Reading, TimeSpan.FromSeconds(8), ct), token);

    public Task<ServiceResult<HotspotControl.Core.Contracts.NetworkSettings>> ReadSettingsAsync(CancellationToken token = default) =>
        Read(() =>
        {
            var (profile, manager) = Current();
            var context = ProfileContext.From(profile) ?? throw new UnknownProfileContextException();
            var config = manager.GetCurrentAccessPointConfiguration();
            var bands = new[] { 0, 1, 2 }.Where(b => config.IsBandSupported((TetheringWiFiBand)b)).ToArray();
            return new HotspotControl.Core.Contracts.NetworkSettings(config.Ssid, (int)config.Band, bands, context);
        }, (action, ct) => Run(action, OperationState.Reading, TimeSpan.FromSeconds(8), ct), token);

    public Task<ServiceResult<IReadOnlyList<ConnectedDevice>>> ReadDevicesAsync(CancellationToken token = default) =>
        Read<IReadOnlyList<ConnectedDevice>>(() =>
        {
            var (_, manager) = Current();
            return manager.GetTetheringClients().Select((client, index) => DeviceProjection.FromReported(
                index + 1, client.HostNames.Select(host => host.DisplayName), client.MacAddress)).ToArray();
        }, (action, ct) => Run(action, OperationState.Reading, TimeSpan.FromSeconds(8), ct), token);

    public async Task<ContractResult> SetEnabledAsync(bool enabled, CancellationToken token = default)
    {
        try
        {
            return await Run<ContractResult>(async () =>
            {
                var (_, manager) = Current();
                var state = manager.TetheringOperationalState;
                if (state == TetheringOperationalState.InTransition) return new(false, MessageCode.OperationInProgress, true);
                if (state is not (TetheringOperationalState.On or TetheringOperationalState.Off))
                    return new(false, MessageCode.OperationInProgress, true);
                if (state == (enabled ? TetheringOperationalState.On : TetheringOperationalState.Off))
                    return new(true, MessageCode.AlreadyInRequestedState);
                var result = enabled ? await manager.StartTetheringAsync() : await manager.StopTetheringAsync();
                return result.Status == TetheringOperationStatus.Success ?
                    new(true, enabled ? MessageCode.Enabled : MessageCode.Disabled) : NativeStatus(result.Status);
            }, enabled ? OperationState.Starting : OperationState.Stopping, TimeSpan.FromSeconds(25), token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error) { return DescribeError(error); }
    }

    public async Task<ContractResult> SaveSettingsAsync(string contextToken, string ssid, string password, int band, CancellationToken token = default)
    {
        var validation = NetworkInput.Validate(ssid, password);
        if (validation is not null) return new(false, validation.Value);
        if (band is < 0 or > 2) return new(false, MessageCode.UnsupportedBand);
        try
        {
            return await Run<ContractResult>(async () =>
            {
                var (profile, manager) = Current();
                var config = manager.GetCurrentAccessPointConfiguration();
                var bands = new[] { 0, 1, 2 }.Where(b => config.IsBandSupported((TetheringWiFiBand)b)).ToArray();
                var state = manager.TetheringOperationalState == TetheringOperationalState.Off ?
                    HotspotControl.Core.Contracts.HotspotState.Off : HotspotControl.Core.Contracts.HotspotState.InTransition;
                var guard = NetworkSettingsGuard.Check(contextToken, ProfileContext.From(profile), state, ssid, password, band, bands);
                if (guard is not null) return new(false, guard.Value);
                config.Ssid = ssid;
                if (password.Length > 0) config.Passphrase = password;
                config.Band = (TetheringWiFiBand)band;
                await manager.ConfigureAccessPointAsync(config);
                return new(true, MessageCode.SettingsSaved);
            }, OperationState.Saving, TimeSpan.FromSeconds(25), token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error) { return DescribeError(error); }
    }

    public static ContractResult DescribeNativeStatus(int status) => NativeStatus((TetheringOperationStatus)status);
    private static ContractResult NativeStatus(TetheringOperationStatus status) => status switch
    {
        TetheringOperationStatus.MobileBroadbandDeviceOff => new(false, MessageCode.MobileBroadbandDeviceOff),
        TetheringOperationStatus.WiFiDeviceOff => new(false, MessageCode.WiFiDeviceOff),
        TetheringOperationStatus.EntitlementCheckTimeout => new(false, MessageCode.EntitlementCheckTimeout, true),
        TetheringOperationStatus.EntitlementCheckFailure => new(false, MessageCode.EntitlementCheckFailure),
        TetheringOperationStatus.OperationInProgress => new(false, MessageCode.OperationInProgress, true),
        TetheringOperationStatus.BluetoothDeviceOff => new(false, MessageCode.BluetoothDeviceOff),
        TetheringOperationStatus.NetworkLimitedConnectivity => new(false, MessageCode.NetworkLimitedConnectivity, true),
        TetheringOperationStatus.RadioRestriction => new(false, MessageCode.RadioRestriction),
        TetheringOperationStatus.BandInterference => new(false, MessageCode.BandInterference),
        _ => new(false, MessageCode.NativeFailure, false, (int)status)
    };
}
