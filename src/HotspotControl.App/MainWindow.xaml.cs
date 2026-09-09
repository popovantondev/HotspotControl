using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HotspotControl.Core;
using HotspotControl.Windows;

namespace HotspotControl.App;

public partial class MainWindow : Window
{
    public HotspotService Service { get; } = new();
    private readonly CancellationTokenSource lifetime = new();
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly StatusMessage messages = new();
    private readonly TrayController tray;
    private readonly Button powerButton;
    private CancellationTokenSource? automaticStart;
    private HotspotState currentState = HotspotState.Unknown;
    private bool isRefreshing;
    private bool isActing;
    private bool initialized;
    private bool closed;
    private readonly bool suppressAutoStart;

    public MainWindow(bool suppressAutoStart = false)
    {
        this.suppressAutoStart = suppressAutoStart;
        InitializeComponent();
        Height = Math.Min(Height, Math.Max(MinHeight, SystemParameters.WorkArea.Height - 24));
        WindowAppearance.Attach(this);
        SceneHost.Child = (UIElement)Application.LoadComponent(new Uri("/HotspotControl.App;component/Assets/Scene.xaml", UriKind.Relative));
        powerButton = (Button)((Canvas)SceneHost.Child).FindName("LaptopPowerButton");
        powerButton.Click += OnPower;
        tray = new TrayController(this);
        refreshTimer.Tick += async (_, _) =>
        {
            UpdateControls();
            if (IsEnabled && !Service.IsBusy) await RefreshAsync();
        };
        Closed += (_, _) =>
        {
            closed = true;
            refreshTimer.Stop();
            automaticStart?.Cancel();
            lifetime.Cancel();
            tray.Dispose();
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Showing a hidden window must not repeat the startup action.
        if (initialized) return;
        initialized = true;
        await RefreshAsync();
        if (closed) return;
        refreshTimer.Start();
        if (!suppressAutoStart && PreferencesStore.Read().AutoEnableHotspot) await AutoStartAsync();
        else if (PreferencesStore.LastReadError is string error) ShowOperation(error);
    }

    private async Task AutoStartAsync()
    {
        ShowOperation("Automatischer Hotspotstart wird vorbereitet …");
        automaticStart = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        try
        {
            var result = await AutoStartPolicy.RunAsync(Service.TryAutoStartAsync, Task.Delay,
                attempt => { if (!closed) ShowOperation($"Warten auf die Verbindung … Versuch {attempt + 1} von 12."); },
                cancellationToken: automaticStart.Token);
            if (!closed)
            {
                ShowOperation(result.Message);
                await RefreshAsync();
            }
        }
        catch (OperationCanceledException) { }
        finally { if (!closed) UpdateControls(); }
    }

    public void RestoreFromTray()
    {
        if (closed) return;
        Show();
        WindowState = WindowState.Normal;
        Activate();
        foreach (Window owned in OwnedWindows) if (owned.IsVisible) owned.Activate();
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private async void OnRefresh(object sender, RoutedEventArgs e)
    {
        messages.ClearOperation();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (closed || isRefreshing || isActing || Service.IsBusy) return;
        isRefreshing = true;
        UpdateControls();
        try
        {
            var snapshot = await Service.ReadAsync(lifetime.Token);
            if (closed) return;
            currentState = snapshot.State;
            StatusText.Text = snapshot.State switch
            {
                HotspotState.On => "Eingeschaltet",
                HotspotState.Off => "Ausgeschaltet",
                HotspotState.InTransition => "Status wird geändert",
                _ => "Unbekannt"
            };
            StatusText.Foreground = new SolidColorBrush(snapshot.State == HotspotState.On
                ? Color.FromRgb(89, 231, 218) : Color.FromRgb(167, 200, 215));
            ClientsText.Text = snapshot.Clients.HasValue ? $"{snapshot.Clients} / {snapshot.MaximumClients}" : "Nicht verfügbar";
            MessageText.Text = messages.ForRefresh(snapshot.Message);
            UpdatedText.Text = $"{(snapshot.ExitCode == 0 ? "Aktualisiert" : "Abfrage fehlgeschlagen")} · {DateTime.Now:HH:mm:ss}";
        }
        catch (OperationCanceledException) when (closed) { }
        catch (Exception exception)
        {
            if (closed) return;
            currentState = HotspotState.Unknown;
            StatusText.Text = "Unbekannt";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(167, 200, 215));
            ClientsText.Text = "Nicht verfügbar";
            ShowOperation(HotspotActions.Error(exception).Message);
            UpdatedText.Text = $"Abfrage fehlgeschlagen · {DateTime.Now:HH:mm:ss}";
        }
        finally
        {
            isRefreshing = false;
            if (!closed) { UpdateControls(); tray.Update(currentState); }
        }
    }

    private void UpdateControls()
    {
        bool available = !closed && !isActing && !isRefreshing && !Service.IsBusy;
        RefreshButton.IsEnabled = DevicesButton.IsEnabled = SettingsButton.IsEnabled = available;
        powerButton.IsEnabled = available && currentState is HotspotState.On or HotspotState.Off;
        var label = currentState == HotspotState.On ? "Hotspot ausschalten" : "Hotspot einschalten";
        powerButton.ToolTip = label;
        AutomationProperties.SetName(powerButton, label);
    }

    private void ShowOperation(string message)
    {
        messages.SetOperation(message);
        MessageText.Text = message;
    }

    private async void OnPower(object sender, RoutedEventArgs e)
    {
        if (isActing || isRefreshing || Service.IsBusy || currentState is not (HotspotState.On or HotspotState.Off)) return;
        automaticStart?.Cancel();
        isActing = true;
        UpdateControls();
        var enable = currentState == HotspotState.Off;
        StatusText.Text = enable ? "Wird eingeschaltet …" : "Wird ausgeschaltet …";
        tray.Update(HotspotState.InTransition);
        try
        {
            var result = await Service.SetEnabledAsync(enable, lifetime.Token);
            if (!closed) ShowOperation(result.Message);
        }
        catch (OperationCanceledException) when (closed) { }
        catch (Exception exception) { if (!closed) ShowOperation(HotspotActions.Error(exception).Message); }
        finally
        {
            isActing = false;
            if (!closed)
            {
                currentState = HotspotState.Unknown;
                StatusText.Text = "Unbekannt";
                ClientsText.Text = "Nicht verfügbar";
                tray.Update(currentState);
                UpdateControls();
            }
        }
        await RefreshAsync();
    }

    private async void OnSettings(object sender, RoutedEventArgs e)
    {
        if (isActing || isRefreshing || Service.IsBusy) return;
        automaticStart?.Cancel();
        refreshTimer.Stop();
        new SettingsWindow(this).ShowDialog();
        if (!closed) { refreshTimer.Start(); await RefreshAsync(); }
    }

    private void OnDevices(object sender, RoutedEventArgs e)
    {
        if (isActing || isRefreshing || Service.IsBusy) return;
        automaticStart?.Cancel();
        refreshTimer.Stop();
        new DevicesWindow(this).ShowDialog();
        if (!closed) refreshTimer.Start();
    }
}
