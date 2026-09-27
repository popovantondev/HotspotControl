using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;

namespace HotspotControl.Presentation;

public sealed class SettingsView : StyledWindow
{
    private readonly IHotspotService service;
    private readonly IApplicationSettings preferences;
    private readonly TextBox name = new() { MaxLength = 32, Height = 42 };
    private readonly PasswordBox password = new() { MaxLength = 63, Height = 42 };
    private readonly ComboBox band = new();
    private readonly TextBlock feedback = Ui.Text("", 12, Ui.Accent);
    private readonly Button save;
    private readonly CancellationTokenSource lifetime = new();
    private NetworkSettings? current;
    private bool off, closed, loading, wasPending;
    public SettingsView(TextCatalog text, IHotspotService provider, IApplicationSettings prefs,
        bool advanced = false, bool demo = true)
        : base(text, text["Settings"], 460, 790, demo: demo)
    {
        service = provider; preferences = prefs;
        feedback.Visibility = Visibility.Collapsed;
        var root = new Grid { Margin = new Thickness(24, 12, 24, 18) };
        root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new());
        root.Children.Add(Ui.Margin(Ui.Stack(Ui.Label(T["Application"]), Ui.Margin(Ui.Text(T["Settings"], 27, bold: true), 7)), bottom: 20));
        var content = new StackPanel();
        var simple = Ui.Stack(Ui.Text(T["NetworkName"], 12, Ui.Muted), Ui.Margin(name, 7, 15), Ui.Text(T["NewPassword"], 12, Ui.Muted), Ui.Margin(password, 7, 8), Ui.Text(T["PasswordHelp"], 11, Ui.Muted));
        AutomationProperties.SetName(name, T["NetworkName"]); AutomationProperties.SetName(password, T["NewPassword"]);
        AutomationProperties.SetAutomationId(name, "NetworkName"); AutomationProperties.SetAutomationId(password, "NewPassword");
        AutomationProperties.SetName(band, T["Frequency"]);
        var extended = Ui.Stack(Ui.Text(T["Frequency"], 12, Ui.Muted), Ui.Margin(band, 7, 15), Ui.Text(T["BandHelp"], 12, Ui.Muted));
        var simpleButton = Ui.Button(T["Simple"], () => { }); var advancedButton = Ui.Button(T["Advanced"], () => { }, true);
        var tabs = new Grid(); tabs.ColumnDefinitions.Add(new()); tabs.ColumnDefinitions.Add(new());
        tabs.Children.Add(simpleButton); Grid.SetColumn(advancedButton, 1); advancedButton.Margin = new Thickness(8, 0, 0, 0); tabs.Children.Add(advancedButton);
        void Select(bool isAdvanced)
        {
            simple.Visibility = isAdvanced ? Visibility.Collapsed : Visibility.Visible;
            extended.Visibility = isAdvanced ? Visibility.Visible : Visibility.Collapsed;
            simpleButton.Opacity = isAdvanced ? .55 : 1; advancedButton.Opacity = isAdvanced ? 1 : .55;
            AutomationProperties.SetHelpText(simpleButton, isAdvanced ? "" : T["Selected"]);
            AutomationProperties.SetHelpText(advancedButton, isAdvanced ? T["Selected"] : "");
        }
        simpleButton.Click += (_, _) => Select(false); advancedButton.Click += (_, _) => Select(true); Select(advanced);
        save = Ui.Button(T["SaveNetwork"], async () => await SaveAsync()); save.IsEnabled = false;
        AutomationProperties.SetAutomationId(save, "SaveNetwork");
        var network = Ui.Stack(Ui.Label(T["Network"]), Ui.Margin(tabs, 12, 16), simple, extended,
            Ui.Margin(Ui.Text(T["NetworkHelp"], 11, Ui.Muted), 16), Ui.Margin(save, 14), Ui.Margin(feedback, 10));
        content.Children.Add(Ui.Card(network, 18));
        var startup = Check(T["StartWindows"], prefs.StartupState == StartupLinkState.Enabled, prefs.SetStartWithWindows);
        if (prefs.StartupState == StartupLinkState.Unavailable) startup.IsEnabled = false;
        var auto = Check(T["StartHotspot"], prefs.AutoEnableHotspot, prefs.SetAutoEnableHotspot);
        var language = new ComboBox();
        foreach (var label in new[] { "Deutsch", "Русский", "English" }) language.Items.Add(label);
        language.SelectedIndex = Array.IndexOf(TextCatalog.Languages, prefs.Language);
        AutomationProperties.SetName(language, T["Language"]);
        bool restoringLanguage = false;
        language.SelectionChanged += (_, _) =>
        {
            if (restoringLanguage || language.SelectedIndex < 0) return;
            var code = preferences.SetLanguage(TextCatalog.Languages[language.SelectedIndex]);
            ShowFeedback(T[code?.ToString() ?? "LanguageSaved"]);
            if (code is not null)
            {
                restoringLanguage = true;
                language.SelectedIndex = Array.IndexOf(TextCatalog.Languages, preferences.Language);
                restoringLanguage = false;
            }
        };
        var app = Ui.Stack(Ui.Label(T["Application"]), Ui.Margin(startup, 8), auto,
            Ui.Margin(Ui.Text(T[Demo ? "ImmediateHelp" : "ImmediateHelpReal"], 11, Ui.Muted), 6, 17),
            Ui.Text(T["Language"], 12, Ui.Muted), Ui.Margin(language, 7));
        if (prefs.StartupState is StartupLinkState.NeedsRepair or StartupLinkState.Unavailable)
            app.Children.Insert(2, Ui.Margin(Ui.Text(T[prefs.StartupState == StartupLinkState.NeedsRepair ?
                "StartupRepair" : "StartupUnavailable"], 11, Ui.Muted), 5));
        content.Children.Add(Ui.Margin(Ui.Card(app, 18), 14));
        var scroll = Ui.Scroll(content); Grid.SetRow(scroll, 1); root.Children.Add(scroll); SetBody(root);
        Loaded += async (_, _) => await InitializeAsync();
        service.StateChanged += OnServiceChanged;
        Closed += (_, _) => { closed = true; service.StateChanged -= OnServiceChanged; lifetime.Cancel(); password.Clear(); };
    }
    private CheckBox Check(string text, bool value, Func<bool, MessageCode?> change)
    {
        var check = new CheckBox { Content = Ui.Text(text, 12), IsChecked = value };
        AutomationProperties.SetName(check, text);
        bool restoring = false;
        void Changed(bool next)
        {
            if (restoring) return;
            var error = change(next);
            ShowFeedback(T[error?.ToString() ?? (Demo ? "MemorySaved" : "PreferencesSaved")]);
            if (error is null) return;
            restoring = true; check.IsChecked = !next; restoring = false;
        }
        check.Checked += (_, _) => Changed(true);
        check.Unchecked += (_, _) => Changed(false);
        return check;
    }
    public async Task InitializeAsync()
    {
        if (closed || loading) return;
        loading = true;
        try
        {
            var result = await service.ReadSettingsAsync(lifetime.Token);
            var status = await service.ReadAsync(lifetime.Token);
            if (closed) return;
            current = result.Data; off = status.Data?.State == HotspotState.Off;
            if (current is { } settings)
            {
                name.Text = settings.Ssid;
                band.Items.Clear();
                foreach (var supported in settings.SupportedBands)
                    band.Items.Add(new ComboBoxItem { Content = supported switch
                    { 0 => T["Automatic"], 1 => "2,4 GHz", 2 => "5 GHz", _ => T["Unavailable"] }, Tag = supported });
                band.SelectedItem = band.Items.Cast<ComboBoxItem>().FirstOrDefault(item => (int)item.Tag == settings.Band);
            }
            if (!result.Result.Success) ShowFeedback(T[result.Result.Code.ToString()]);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!closed) ShowFeedback(T["NativeFailure"]); }
        finally { loading = false; if (!closed) UpdateAvailability(); }
    }
    private void OnServiceChanged(object? sender, EventArgs args)
    {
        if (closed || Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (closed) return;
            var recovered = wasPending && service.OperationState == OperationState.Idle;
            wasPending = service.OperationState == OperationState.PendingAfterTimeout;
            UpdateAvailability();
            if (recovered && !loading) _ = InitializeAsync();
        });
    }
    private void UpdateAvailability()
    {
        bool idle = service.OperationState == OperationState.Idle;
        save.IsEnabled = idle && off && current is not null && band.SelectedItem is ComboBoxItem && !loading;
        name.IsEnabled = password.IsEnabled = band.IsEnabled = idle && current is not null;
        if (service.OperationState == OperationState.PendingAfterTimeout) ShowFeedback(T["TimedOut"]);
    }
    private void ShowFeedback(string text) { feedback.Text = text; feedback.Visibility = Visibility.Visible; }
    private async Task SaveAsync()
    {
        if (current is null || !save.IsEnabled) return;
        try
        {
            if (band.SelectedItem is not ComboBoxItem selected) return;
            var result = await service.SaveSettingsAsync(current.ContextToken, name.Text, password.Password, (int)selected.Tag, lifetime.Token);
            if (closed) return;
            ShowFeedback(T[result.Code.ToString()]);
            feedback.Foreground = result.Success ? Ui.Accent : Ui.Brush("#FFD3A5");
            if (result.Success) password.Clear();
            if (result.Code == MessageCode.ContextChanged) { current = null; _ = InitializeAsync(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!closed) ShowFeedback(T["NativeFailure"]); }
        finally { if (!closed) UpdateAvailability(); }
    }
}
