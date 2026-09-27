namespace HotspotControl.Core.Contracts;

public enum HotspotState { Unknown, On, Off, InTransition }
public enum OperationState { Idle, Reading, Starting, Stopping, Saving, PendingAfterTimeout }
public enum MessageSeverity { Information, Success, Warning, Error }
public enum MessageCode
{
    Ready, NoInternetProfile, AccessDenied, NotSupported, Busy, TimedOut, Cancelled,
    ContextChanged, NativeFailure, InvalidSsid, InvalidPassword, UnsupportedBand,
    MustTurnOff, Enabled, Disabled, AlreadyInRequestedState, SettingsSaved,
    PreferencesReadFailed, PreferencesSaveFailed, StartupFailed,
    MobileBroadbandDeviceOff, WiFiDeviceOff, EntitlementCheckTimeout,
    EntitlementCheckFailure, OperationInProgress, BluetoothDeviceOff,
    NetworkLimitedConnectivity, RadioRestriction, BandInterference
}
public sealed record AppMessage(MessageCode Code, MessageSeverity Severity = MessageSeverity.Information, int? SystemCode = null);
public sealed record OperationResult(bool Success, MessageCode Code, bool Retryable = false, int? SystemCode = null);
public sealed record ServiceResult<T>(T? Data, OperationResult Result);
public sealed record HotspotSnapshot(HotspotState State, uint? Clients, uint? MaximumClients);
public sealed record NetworkSettings(string Ssid, int Band, IReadOnlyList<int> SupportedBands, string ContextToken);
public sealed record ConnectedDevice(int Number, IReadOnlyList<string> HostNames, string? MacAddress);

public interface IHotspotService
{
    OperationState OperationState { get; }
    event EventHandler? StateChanged;
    Task<ServiceResult<HotspotSnapshot>> ReadAsync(CancellationToken token = default);
    Task<ServiceResult<NetworkSettings>> ReadSettingsAsync(CancellationToken token = default);
    Task<ServiceResult<IReadOnlyList<ConnectedDevice>>> ReadDevicesAsync(CancellationToken token = default);
    Task<OperationResult> SetEnabledAsync(bool enabled, CancellationToken token = default);
    Task<OperationResult> SaveSettingsAsync(string contextToken, string ssid, string password, int band, CancellationToken token = default);
}
