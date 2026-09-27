using HotspotControl.Core.Contracts;

namespace HotspotControl.Preview;

public enum DemoScenario { On, Off, Empty, Unknown, Reading, Transition, NoInternet, Denied, Failure, Timeout }

// This provider has no Windows API or storage dependencies.
public sealed class DemoHotspotService : IHotspotService
{
    private HotspotState state;
    private int revision;
    private bool recovering;
    private string ssid = "Demo-WLAN";
    private int band;
    public DemoScenario Scenario { get; private set; }
    public OperationState OperationState { get; private set; }
    public event EventHandler? StateChanged;
    public DemoHotspotService(DemoScenario scenario = DemoScenario.On) => Select(scenario);

    public void Select(DemoScenario scenario)
    {
        revision++; recovering = false; Scenario = scenario;
        state = scenario switch { DemoScenario.On or DemoScenario.Empty => HotspotState.On, DemoScenario.Off => HotspotState.Off, DemoScenario.Transition => HotspotState.InTransition, _ => HotspotState.Unknown };
        OperationState = scenario switch { DemoScenario.Reading => OperationState.Reading, DemoScenario.Transition => OperationState.Starting, DemoScenario.Timeout => OperationState.PendingAfterTimeout, _ => OperationState.Idle };
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
    private MessageCode? Failure => Scenario switch
    {
        DemoScenario.NoInternet => MessageCode.NoInternetProfile, DemoScenario.Denied => MessageCode.AccessDenied,
        DemoScenario.Failure => MessageCode.NativeFailure, DemoScenario.Timeout => MessageCode.TimedOut, _ => null
    };
    private void SetOperation(OperationState operation) { OperationState = operation; StateChanged?.Invoke(this, EventArgs.Empty); }
    public Task<ServiceResult<HotspotSnapshot>> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (Scenario == DemoScenario.Timeout && !recovering) { recovering = true; _ = RecoverAsync(revision); }
        var failure = Failure;
        return Task.FromResult(new ServiceResult<HotspotSnapshot>(failure is null ? new(state, state == HotspotState.On ? (Scenario == DemoScenario.Empty ? 0u : 2u) : 0u, 8) : null,
            new(failure is null, failure ?? MessageCode.Ready)));
    }
    private async Task RecoverAsync(int request)
    {
        await Task.Delay(5000);
        if (revision != request) return;
        Scenario = DemoScenario.Off; state = HotspotState.Off; SetOperation(OperationState.Idle);
    }
    public Task<ServiceResult<NetworkSettings>> ReadSettingsAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var failure = Failure;
        return Task.FromResult(new ServiceResult<NetworkSettings>(failure is null ? new(ssid, band, new[] { 0, 1, 2 }, "demo-context") : null, new(failure is null, failure ?? MessageCode.Ready)));
    }
    public Task<ServiceResult<IReadOnlyList<ConnectedDevice>>> ReadDevicesAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        IReadOnlyList<ConnectedDevice> devices = state == HotspotState.On && Scenario != DemoScenario.Empty
            ? [new(1, new[] { "demo-phone", "192.0.2.10" }, "02:00:00:00:00:10"), new(2, new[] { "demo-laptop", "192.0.2.11" }, "02:00:00:00:00:11")] : [];
        var failure = Failure;
        return Task.FromResult(new ServiceResult<IReadOnlyList<ConnectedDevice>>(failure is null ? devices : null, new(failure is null, failure ?? MessageCode.Ready)));
    }
    public Task<OperationResult> SetEnabledAsync(bool enabled, CancellationToken token = default) => ExecuteAsync(
        enabled ? OperationState.Starting : OperationState.Stopping, () =>
        {
            state = enabled ? HotspotState.On : HotspotState.Off;
            Scenario = enabled ? DemoScenario.On : DemoScenario.Off;
            return new(true, enabled ? MessageCode.Enabled : MessageCode.Disabled);
        }, token);

    public Task<OperationResult> SaveSettingsAsync(string contextToken, string name, string password, int selectedBand, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (contextToken != "demo-context") return Task.FromResult(new OperationResult(false, MessageCode.ContextChanged));
        if (name.Length is < 1 or > 32 || name[0] == ' ' || name[^1] == ' ' || name.Any(c => c < 32 || c > 126))
            return Task.FromResult(new OperationResult(false, MessageCode.InvalidSsid));
        if (password.Length != 0 && (password.Length is < 8 or > 63 || password.Any(c => c < 32 || c > 126)))
            return Task.FromResult(new OperationResult(false, MessageCode.InvalidPassword));
        if (selectedBand is < 0 or > 2) return Task.FromResult(new OperationResult(false, MessageCode.UnsupportedBand));
        if (state != HotspotState.Off) return Task.FromResult(new OperationResult(false, MessageCode.MustTurnOff));
        return ExecuteAsync(OperationState.Saving, () => { ssid = name; band = selectedBand; return new(true, MessageCode.SettingsSaved); }, token);
    }
    private async Task<OperationResult> ExecuteAsync(OperationState operation, Func<OperationResult> complete, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (OperationState != OperationState.Idle) return new(false, MessageCode.Busy);
        if (Failure is { } failure) return new(false, failure);
        var request = revision; SetOperation(operation);
        try
        {
            await Task.Delay(550, token);
            return revision == request ? complete() : new(false, MessageCode.Cancelled);
        }
        finally { if (revision == request) SetOperation(OperationState.Idle); }
    }
}
