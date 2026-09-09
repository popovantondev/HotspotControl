using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotspotControl.Windows;

namespace HotspotControl.App;

public sealed class DevicesWindow : AppDialog
{
    private readonly TextBlock detail = new() { TextWrapping = TextWrapping.Wrap, FontSize = 16, Margin = new Thickness(0, 24, 0, 24) };
    private readonly Button previous = new() { Content = "Zurück", IsEnabled = false, Margin = new Thickness(0, 0, 12, 0) };
    private readonly Button next = new() { Content = "Weiter", IsEnabled = false };
    private readonly Button refresh = new() { Content = "Aktualisieren", Margin = new Thickness(0, 20, 0, 0) };
    private IReadOnlyList<string> devices = Array.Empty<string>();
    private int index;

    public DevicesWindow(Window owner) : base(owner, "Verbundene Geräte", 460)
    {
        var panel = new StackPanel { Margin = new Thickness(24, 10, 24, 24) };
        panel.Children.Add(new TextBlock { Text = "Verbundene Geräte", FontSize = 24 });
        panel.Children.Add(detail);
        var navigation = new StackPanel { Orientation = Orientation.Horizontal };
        navigation.Children.Add(previous); navigation.Children.Add(next);
        panel.Children.Add(navigation); panel.Children.Add(refresh);
        panel.Children.Add(new TextBlock { Text = "Gerätedaten werden nur angezeigt und nicht gespeichert.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 18, 0, 0), FontSize = 12 });
        SetBody(panel);
        previous.Click += (_, _) => { index--; ShowDevice(); };
        next.Click += (_, _) => { index++; ShowDevice(); };
        refresh.Click += Load;
        Loaded += Load;
    }

    private async void Load(object sender, RoutedEventArgs e)
    {
        refresh.IsEnabled = previous.IsEnabled = next.IsEnabled = false;
        detail.Text = "Geräte werden abgefragt …";
        try { devices = await Task.Run(HotspotActions.ReadDevices); index = 0; ShowDevice(); }
        catch (Exception exception) { devices = Array.Empty<string>(); detail.Text = HotspotActions.Error(exception).Message; }
        finally { refresh.IsEnabled = true; }
    }

    private void ShowDevice()
    {
        detail.Text = devices.Count == 0 ? "Keine verbundenen Geräte gemeldet." : $"{index + 1} / {devices.Count}\n\n{devices[index]}";
        previous.IsEnabled = index > 0;
        next.IsEnabled = index + 1 < devices.Count;
    }
}

