using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows;
using System.Windows.Media;
using HotspotControl.Windows;

namespace HotspotControl.App;

public partial class MainWindow : Window
{
    private TrayController? tray;
    private readonly System.Windows.Threading.DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private bool isRefreshing;
    private bool isActing;
    private HotspotState currentState = HotspotState.Unknown;
    private Button powerButton = null!;
    private void OnMinimize(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void OnClose(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
    public MainWindow()
    {
        InitializeComponent();
        WindowAppearance.Attach(this);
        SceneHost.Child = (UIElement)Application.LoadComponent(new Uri("/HotspotControl.App;component/Assets/Scene.xaml", UriKind.Relative));
        powerButton = (Button)((Canvas)SceneHost.Child).FindName("LaptopPowerButton");
        powerButton.Click += OnPower;
        tray = new TrayController(this);
        refreshTimer.Tick += async (_, _) => { if (IsEnabled) await RefreshAsync(); };
        Closed += (_, _) => { refreshTimer.Stop(); tray.Dispose(); };
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        if (PreferencesStore.Read().AutoEnableHotspot)
        {
            isActing = true;
            powerButton.IsEnabled = RefreshButton.IsEnabled = SettingsButton.IsEnabled = DevicesButton.IsEnabled = false;
            var result = await Task.Run(() => HotspotActions.SetEnabledAsync(true));
            isActing = false;
            SettingsButton.IsEnabled = DevicesButton.IsEnabled = true;
            await RefreshAsync();
            MessageText.Text = result.Message;
        }
        refreshTimer.Start();
    }
    private async void OnRefresh(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (isRefreshing || isActing) return;
        isRefreshing = true;
        RefreshButton.IsEnabled = false;
        powerButton.IsEnabled = false;
        StatusText.Text = "Wird abgefragt …";
        StatusText.Foreground = new SolidColorBrush(Color.FromRgb(167, 200, 215));
        ClientsText.Text = "—";
        MessageText.Text = "";
        try
        {
            // Keep synchronous Windows calls off the UI thread. Only one request runs at a time.
            var snapshot = await Task.Run(HotspotReader.Read);
            currentState = snapshot.State;
            StatusText.Text = snapshot.State switch
            {
                HotspotState.On => "Eingeschaltet",
                HotspotState.Off => "Ausgeschaltet",
                HotspotState.InTransition => "Status wird geändert",
                _ => "Unbekannt"
            };
            StatusText.Foreground = snapshot.State == HotspotState.On ? new SolidColorBrush(Color.FromRgb(89, 231, 218)) : new SolidColorBrush(Color.FromRgb(167, 200, 215));
            ClientsText.Text = snapshot.Clients.HasValue ? $"{snapshot.Clients} / {snapshot.MaximumClients}" : "Nicht verfügbar";
            MessageText.Text = snapshot.Message;
            UpdatedText.Text = $"{(snapshot.ExitCode == 0 ? "Aktualisiert" : "Abfrage fehlgeschlagen")} · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception)
        {
            currentState = HotspotState.Unknown;
            // Last-resort UI boundary: never retain an old successful status after a failed refresh.
            StatusText.Text = "Unbekannt";
            ClientsText.Text = "Nicht verfügbar";
            MessageText.Text = "Die Abfrage ist fehlgeschlagen. Bitte erneut versuchen.";
            UpdatedText.Text = $"Abfrage fehlgeschlagen · {DateTime.Now:HH:mm:ss}";
        }
        finally
        {
            isRefreshing = false;
            RefreshButton.IsEnabled = true;
            UpdatePowerButton();
            tray?.Update(currentState);
        }
    }
    private void UpdatePowerButton()
    {
        powerButton.IsEnabled = !isActing && !isRefreshing && currentState is HotspotState.On or HotspotState.Off;
        var label = currentState == HotspotState.On ? "Hotspot ausschalten" : "Hotspot einschalten";
        powerButton.ToolTip = label;
        AutomationProperties.SetName(powerButton, label);
    }

    private async void OnPower(object sender, RoutedEventArgs e)
    {
        if (isActing || isRefreshing || currentState is not (HotspotState.On or HotspotState.Off)) return;
        isActing = true;
        powerButton.IsEnabled = RefreshButton.IsEnabled = SettingsButton.IsEnabled = DevicesButton.IsEnabled = false;
        var enable = currentState == HotspotState.Off;
        StatusText.Text = enable ? "Wird eingeschaltet …" : "Wird ausgeschaltet …";
        OperationResult result;
        try { result = await Task.Run(() => HotspotActions.SetEnabledAsync(enable)); }
        catch (Exception exception) { result = HotspotActions.Error(exception); }
        finally
        {
            isActing = false;
            SettingsButton.IsEnabled = DevicesButton.IsEnabled = true;
        }
        await RefreshAsync();
        MessageText.Text = result.Message;
    }

    private async void OnSettings(object sender, RoutedEventArgs e)
    {
        if (isActing || isRefreshing) return;
        new SettingsWindow(this).ShowDialog();
        await RefreshAsync();
    }

    private void OnDevices(object sender, RoutedEventArgs e)
    {
        if (!isActing && !isRefreshing) new DevicesWindow(this).ShowDialog();
    }}






