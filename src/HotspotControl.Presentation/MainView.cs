using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;

namespace HotspotControl.Presentation;

public sealed class MainView : StyledWindow
{
    private readonly IHotspotService service;
    private readonly IApplicationSettings preferences;
    private readonly Action? manualIntent;
    private readonly CancellationTokenSource lifetime = new();
    private HotspotSnapshot snapshot = new(HotspotState.Unknown, null, null);
    private MessageCode message = MessageCode.Ready;
    private bool messageIsError;
    private string? statusNotice;
    private TextBlock status = null!, count = null!, note = null!;
    private Button power = null!, refresh = null!, devices = null!, settings = null!;
    private Canvas signal = null!;
    private System.Windows.Media.Brush signalBrush = null!;
    private bool reading, closed, wasPending, compact;
    public Task InitialRead { get; private set; } = Task.CompletedTask;
    public HotspotState CurrentState => snapshot.State;
    public event Action<HotspotState>? StatusChanged;
    public MainView(TextCatalog text, IHotspotService provider, IApplicationSettings prefs, bool compactMode = false,
        bool demo = true, Action? manualIntent = null)
        : base(text, demo ? text["PreviewTitle"] : "Hotspot Control", compactMode ? 400 : 480,
            compactMode ? 360 : 820, demo: demo, canMinimize: true)
    {
        service = provider; preferences = prefs; this.manualIntent = manualIntent; compact = compactMode; Build();
        service.StateChanged += OnServiceChanged;
        Loaded += (_, _) => InitialRead = RefreshAsync();
        SizeChanged += (_, _) => { bool next = Height < 650; if (compact != next) { compact = next; Build(); } };
        Closed += (_, _) => { closed = true; service.StateChanged -= OnServiceChanged; lifetime.Cancel(); };
    }
    private void Build()
    {
        var root = new Grid { Margin = new Thickness(compact ? 18 : 26, compact ? 4 : 14, compact ? 18 : 26, compact ? 12 : 20) };
        root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new());
        root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var heading = Ui.Stack(Ui.Text("Hotspot Control", compact ? 23 : 28, bold: true));
        if (!compact) heading.Children.Add(Ui.Margin(Ui.Text(T["Tagline"], 12, Ui.Muted), 6));
        var scene = (Canvas)Application.LoadComponent(new Uri("/HotspotControl.Presentation;component/Assets/Scene.xaml", UriKind.Relative));
        signal = scene.Children.OfType<Canvas>().First();
        signalBrush = (System.Windows.Media.Brush)scene.Resources["Signal"];
        power = (Button)scene.FindName("LaptopPowerButton");
        power.MinHeight = 0;
        if (compact)
        {
            power.Focusable = false;
            power = Ui.Button("", () => { }); power.Width = 44; power.Height = 44; power.Padding = new Thickness(0);
            power.Content = new System.Windows.Shapes.Path { Data = System.Windows.Media.Geometry.Parse("M10,1 L10,11 M4,5 A9,9 0 1 0 16,5"), Stroke = Ui.Brush("#075DD1"), StrokeThickness = 2, Width = 22, Height = 23 };
            root.Children.Add(Ui.Pair(heading, power));
        }
        else root.Children.Add(heading);
        power.Click += async (_, _) => await ToggleAsync();
        AutomationProperties.SetAutomationId(power, "PreviewPower");
        var illustration = new Viewbox { Child = scene, Stretch = System.Windows.Media.Stretch.Uniform, MaxHeight = 340, Margin = new Thickness(0, 4, 0, 4) };
        Grid.SetRow(illustration, 1); root.Children.Add(illustration);
        status = Ui.Text("", compact ? 21 : 28, Ui.Accent, true); count = Ui.Text("—", compact ? 19 : 26, bold: true);
        var deviceLabel = Ui.Text(T["ConnectedDevices"], 12, Ui.Muted); deviceLabel.VerticalAlignment = VerticalAlignment.Center;
        var panel = Ui.Stack();
        if (!compact) panel.Children.Add(Ui.Margin(Ui.Label(T["MobileHotspot"]), bottom: 9));
        panel.Children.Add(status);
        var detail = Ui.Button(T["Details"] + "  ↗", () => ShowChild(new MessageView(T, message)), true);
        detail.Padding = new Thickness(9, 4, 9, 4); detail.MinHeight = 28; detail.HorizontalAlignment = HorizontalAlignment.Right;
        if (compact)
        {
            count.VerticalAlignment = VerticalAlignment.Center;
            count.TextWrapping = TextWrapping.NoWrap;
            detail.Margin = new Thickness(12, 0, 0, 0);
            var countAndDetails = new StackPanel { Orientation = Orientation.Horizontal };
            countAndDetails.Children.Add(count); countAndDetails.Children.Add(detail);
            panel.Children.Add(Ui.Margin(Ui.Pair(deviceLabel, countAndDetails), 5));
        }
        else panel.Children.Add(Ui.Margin(Ui.Pair(deviceLabel, count), 20));
        note = Ui.Text("", 12, Ui.Muted); note.MaxHeight = compact ? 0 : 35; note.TextTrimming = TextTrimming.CharacterEllipsis;
        if (!compact)
        {
            panel.Children.Add(new Border { Height = 1, Background = Ui.Brush("#DBE4F0"), Margin = new Thickness(0, 16, 0, 13) });
            panel.Children.Add(note);
        }
        if (!compact) panel.Children.Add(Ui.Margin(detail, 8));
        var card = Ui.Card(panel, compact ? 12 : 22); Grid.SetRow(card, 2); root.Children.Add(card);
        var footer = Ui.Stack();
        var actions = new Grid { Margin = new Thickness(0, compact ? 8 : 16, 0, 0) };
        for (int i = 0; i < 3; i++) actions.ColumnDefinitions.Add(new());
        refresh = Ui.Button(T["Refresh"], async () => { ManualIntent(); await RefreshAsync(); });
        devices = Ui.Button(T["Devices"], () => { ManualIntent(); ShowChild(new DevicesView(T, service)); }, true);
        settings = Ui.Button(T["Settings"], () => { ManualIntent(); ShowChild(new SettingsView(T, service, preferences, demo: Demo)); }, true);
        var buttons = new[] { refresh, devices, settings };
        for (int i = 0; i < buttons.Length; i++)
        { buttons[i].Padding = new Thickness(6, 10, 6, 10); buttons[i].Margin = new Thickness(i == 0 ? 0 : 6, 0, 0, 0); Grid.SetColumn(buttons[i], i); actions.Children.Add(buttons[i]); }
        footer.Children.Add(actions);
        if (!compact && Demo) footer.Children.Add(Ui.Margin(Ui.Text(T["Demo"], 10, Ui.Muted), 15));
        Grid.SetRow(footer, 3); root.Children.Add(footer); SetBody(root); UpdateVisuals();
    }
    private void OnServiceChanged(object? sender, EventArgs args)
    {
        if (closed) return;
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(() => OnServiceChanged(sender, args)); return; }
        bool recovered = wasPending && service.OperationState == OperationState.Idle;
        wasPending = service.OperationState == OperationState.PendingAfterTimeout;
        UpdateVisuals();
        if (service.OperationState is OperationState.Starting or OperationState.Stopping or OperationState.PendingAfterTimeout)
            StatusChanged?.Invoke(HotspotState.InTransition);
        if (recovered && !reading) _ = RefreshAsync();
    }
    private void UpdateVisuals()
    {
        var operation = service.OperationState;
        var key = operation switch
        {
            OperationState.Starting => "Starting", OperationState.Stopping => "Stopping", OperationState.Reading => "Reading",
            OperationState.Saving => "Saving", OperationState.PendingAfterTimeout => "Pending",
            _ => snapshot.State switch { HotspotState.On => "On", HotspotState.Off => "Off", HotspotState.InTransition => "Transition", _ => "Unknown" }
        };
        status.Text = T[key]; status.Foreground = snapshot.State == HotspotState.On ? Ui.Accent : Ui.Brush("#607087");
        foreach (var shape in signal.Children.OfType<System.Windows.Shapes.Shape>())
        {
            var brush = snapshot.State == HotspotState.On ? signalBrush : Ui.Brush("#8BA1AF");
            if (shape.Stroke is not null) shape.Stroke = brush;
            if (shape.Fill is not null) shape.Fill = brush;
            if (shape.Effect is System.Windows.Media.Effects.DropShadowEffect shadow)
                shadow.Opacity = snapshot.State == HotspotState.On ? .65 : 0;
        }
        count.Text = snapshot.Clients is { } clients ? $"{clients} / {snapshot.MaximumClients}" : "—";
        note.Text = statusNotice ?? T[message.ToString()];
        bool idle = operation == OperationState.Idle && !reading;
        power.IsEnabled = idle && snapshot.State is HotspotState.On or HotspotState.Off;
        power.ToolTip = T[snapshot.State == HotspotState.On ? "PowerOff" : "PowerOn"];
        AutomationProperties.SetName(power, (string)power.ToolTip);
        refresh.IsEnabled = idle; devices.IsEnabled = idle;
        settings.IsEnabled = true;
    }
    public async Task RefreshAsync()
    {
        if (closed || reading) return;
        reading = true; UpdateVisuals();
        try
        {
            var result = await service.ReadAsync(lifetime.Token);
            if (closed) return;
            snapshot = result.Data ?? new(HotspotState.Unknown, null, null);
            if (!messageIsError)
            { message = result.Result.Code; messageIsError = !result.Result.Success; }
            StatusChanged?.Invoke(snapshot.State);
            wasPending = service.OperationState == OperationState.PendingAfterTimeout;
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!closed) { snapshot = new(HotspotState.Unknown, null, null); message = MessageCode.NativeFailure; messageIsError = true; } }
        finally { reading = false; if (!closed) UpdateVisuals(); }
    }
    private async Task ToggleAsync()
    {
        ManualIntent();
        try
        {
            var enable = snapshot.State == HotspotState.Off;
            var result = await service.SetEnabledAsync(enable, lifetime.Token);
            if (closed) return;
            await RefreshAsync();
            message = result.Success && snapshot.State != (enable ? HotspotState.On : HotspotState.Off)
                ? MessageCode.NativeFailure : result.Code;
            messageIsError = !result.Success || message == MessageCode.NativeFailure;
            UpdateVisuals();
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!closed) ShowMessage(MessageCode.NativeFailure); }
    }
    public void ShowMessage(MessageCode code)
    {
        message = code;
        messageIsError = code is not (MessageCode.Ready or MessageCode.Enabled or MessageCode.Disabled or
            MessageCode.AlreadyInRequestedState or MessageCode.SettingsSaved);
        statusNotice = null; if (!closed) UpdateVisuals();
    }
    public void ShowNotice(string text) { statusNotice = text; if (!closed) UpdateVisuals(); }
    private void ManualIntent() { manualIntent?.Invoke(); message = MessageCode.Ready; messageIsError = false; statusNotice = null; }
    public void RestoreFromTray()
    {
        if (closed) return;
        Show(); WindowState = WindowState.Normal; Activate();
        foreach (Window owned in OwnedWindows) if (owned.IsVisible) owned.Activate();
    }
}
