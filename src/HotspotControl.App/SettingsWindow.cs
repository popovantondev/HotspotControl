using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotspotControl.Windows;

namespace HotspotControl.App;

public sealed class SettingsWindow : AppDialog
{
    private readonly TextBox nameBox = new();
    private readonly PasswordBox passwordBox = new();
    private readonly ComboBox bandBox = new();
    private readonly TextBlock message = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 0) };
    private readonly Button saveButton = new() { Content = "Speichern", IsEnabled = false, Margin = new Thickness(0, 18, 0, 0) };
    private bool busy;
    private bool closed;
    private readonly HotspotService service;
    private readonly CancellationTokenSource lifetime = new();

    public SettingsWindow(MainWindow owner) : base(owner, "Einstellungen", 650)
    {
        service = owner.Service;
        var panel = new StackPanel { Margin = new Thickness(24, 10, 24, 24) };
        panel.Children.Add(new TextBlock { Text = "Einstellungen", FontSize = 24, FontWeight = FontWeights.SemiBold });
        var tabs = new TabControl { Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(17, 44, 67)), Margin = new Thickness(0, 18, 0, 0), Height = 260 };
        var tabStyle = (Style)System.Windows.Markup.XamlReader.Parse("""
<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="TabItem">
 <Setter Property="Foreground" Value="#B6D7E2"/>
 <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="TabItem"><Border x:Name="TabSurface" Background="#183D52" Padding="18,10" CornerRadius="8,8,0,0" Margin="0,0,4,0"><ContentPresenter ContentSource="Header"/></Border><ControlTemplate.Triggers><Trigger Property="IsSelected" Value="True"><Setter TargetName="TabSurface" Property="Background" Value="#236271"/><Setter Property="Foreground" Value="#83FFE8"/></Trigger><Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="TabSurface" Property="BorderBrush" Value="White"/><Setter TargetName="TabSurface" Property="BorderThickness" Value="1"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
</Style>
""");
        tabs.Resources.Add(typeof(TabItem), tabStyle);
        nameBox.Background = passwordBox.Background = new SolidColorBrush(Color.FromRgb(27, 65, 84));
        nameBox.Foreground = passwordBox.Foreground = Brushes.White;
        nameBox.BorderBrush = passwordBox.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 145, 161));
        nameBox.Padding = passwordBox.Padding = new Thickness(6, 3, 6, 3);
        nameBox.CaretBrush = passwordBox.CaretBrush = Brushes.White;
        bandBox.Foreground = Brushes.Black;
        var simple = new StackPanel { Margin = new Thickness(16) };
        simple.Children.Add(new TextBlock { Text = "Netzwerkname (SSID)" });
        nameBox.Margin = new Thickness(0, 6, 0, 16); nameBox.MaxLength = 32; nameBox.Height = 30;
        simple.Children.Add(nameBox);
        simple.Children.Add(new TextBlock { Text = "Neues Passwort" });
        passwordBox.Margin = new Thickness(0, 6, 0, 10); passwordBox.MaxLength = 63; passwordBox.Height = 30;
        simple.Children.Add(passwordBox);
        simple.Children.Add(new TextBlock { Text = "Leer lassen: bisheriges Passwort behalten.\nÄnderungen erst bei ausgeschaltetem Hotspot speichern.", TextWrapping = TextWrapping.Wrap });
        var advanced = new StackPanel { Margin = new Thickness(16) };
        advanced.Children.Add(new TextBlock { Text = "Frequenzband" });
        bandBox.Margin = new Thickness(0, 8, 0, 16); bandBox.Height = 30;
        advanced.Children.Add(bandBox);
        advanced.Children.Add(new TextBlock { Text = "2,4 GHz: größere Reichweite.\n5 GHz: häufig höhere Geschwindigkeit.\nNur vom Adapter unterstützte Optionen werden angeboten.", TextWrapping = TextWrapping.Wrap });
        tabs.Items.Add(new TabItem { Header = "Einfach", Content = simple });
        tabs.Items.Add(new TabItem { Header = "Erweitert", Content = advanced });
        panel.Children.Add(tabs);
        var startup = new CheckBox
        {
            Content = "Mit Windows starten",
            IsChecked = UserStartup.IsEnabled,
            Margin = new Thickness(0, 18, 0, 4),
            Foreground = Brushes.White,
            ToolTip = "Startet die App bei deiner Windows-Anmeldung."
        };
        System.Windows.Automation.AutomationProperties.SetAutomationId(startup, "UserStartupCheckBox");
        bool updatingStartup = false;
        void SaveStartup(object sender, RoutedEventArgs e)
        {
            try
            {
                if (updatingStartup) return;
                updatingStartup = true;
                UserStartup.SetEnabled(startup.IsChecked == true);
                message.Text = startup.IsChecked == true ? "Autostart für dein Benutzerkonto aktiviert." : "Autostart deaktiviert.";
            }
            catch (Exception)
            {
                startup.IsChecked = UserStartup.IsEnabled;
                message.Text = "Windows erlaubt das Ändern des Autostarts nicht.";
            }
            finally { updatingStartup = false; }
        }
        startup.Checked += SaveStartup;
        startup.Unchecked += SaveStartup;
        panel.Children.Add(startup);
        panel.Children.Add(new TextBlock { Text = "Autostart gilt nur für dein Benutzerkonto.", FontSize = 11, Foreground = Brushes.LightSteelBlue });
        var autoEnable = new CheckBox
        {
            Content = "Hotspot beim App-Start einschalten",
            IsChecked = PreferencesStore.Read().AutoEnableHotspot,
            Margin = new Thickness(0, 12, 0, 0),
            Foreground = Brushes.White,
            ToolTip = "Gilt ab dem nächsten App-Start. Windows muss die Freigabe erlauben."
        };
        System.Windows.Automation.AutomationProperties.SetAutomationId(autoEnable, "AutoEnableHotspotCheckBox");
        bool updatingAutoEnable = false;
        void SaveAutoEnable(object sender, RoutedEventArgs e)
        {
            try
            {
                if (updatingAutoEnable) return;
                updatingAutoEnable = true;
                PreferencesStore.Save(new AppPreferences(autoEnable.IsChecked == true));
                message.Text = "Startverhalten gespeichert. Gilt ab dem nächsten App-Start.";
            }
            catch (Exception)
            {
                autoEnable.IsChecked = PreferencesStore.Read().AutoEnableHotspot;
                message.Text = "Die Einstellung konnte nicht gespeichert werden.";
            }
            finally { updatingAutoEnable = false; }
        }
        autoEnable.Checked += SaveAutoEnable;
        autoEnable.Unchecked += SaveAutoEnable;
        panel.Children.Add(autoEnable);
        panel.Children.Add(saveButton); panel.Children.Add(message);
        SetBody(panel);
        saveButton.Click += Save;
        Loaded += Load;
        Closing += (_, e) => { if (busy) e.Cancel = true; };
        Closed += (_, _) => { closed = true; lifetime.Cancel(); passwordBox.Clear(); };
    }

    private async void Load(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = await service.ReadSettingsAsync(lifetime.Token);
            if (closed) return;
            nameBox.Text = settings.Ssid;
            foreach (var band in settings.SupportedBands)
                bandBox.Items.Add(new ComboBoxItem { Content = band switch { 0 => "Automatisch", 1 => "2,4 GHz", 2 => "5 GHz", _ => "Unbekannt" }, Tag = band });
            bandBox.SelectedItem = bandBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => (int)item.Tag == settings.Band);
            saveButton.IsEnabled = bandBox.SelectedItem is not null;
        }
        catch (Exception exception) { if (!closed) message.Text = HotspotActions.Error(exception).Message; }
    }

    private async void Save(object sender, RoutedEventArgs e)
    {
        if (busy || service.IsBusy || bandBox.SelectedItem is not ComboBoxItem selected) return;
        busy = true; saveButton.IsEnabled = false;
        nameBox.IsEnabled = passwordBox.IsEnabled = bandBox.IsEnabled = false;
        var ssid = nameBox.Text; var password = passwordBox.Password; var band = (int)selected.Tag;
        try
        {
            var result = await service.SaveSettingsAsync(ssid, password, band, lifetime.Token);
            if (closed) return;
            message.Text = result.Message;
            if (result.Success) passwordBox.Clear();
        }
        catch (Exception exception) { if (!closed) message.Text = HotspotActions.Error(exception).Message; }
        finally
        {
            password = "";
            busy = false;
            if (!closed)
            {
                nameBox.IsEnabled = passwordBox.IsEnabled = bandBox.IsEnabled = true;
                saveButton.IsEnabled = true;
            }
        }
    }
}
