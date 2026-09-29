using System.Windows;
using System.Windows.Controls;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;

namespace HotspotControl.Presentation;

public sealed class DevicesView : StyledWindow
{
    private readonly IHotspotService service;
    private readonly StackPanel list = new();
    private readonly Button refresh;
    private readonly CancellationTokenSource lifetime = new();
    private bool closed, loading;
    public DevicesView(TextCatalog text, IHotspotService provider) : base(text, text["Devices"], 460, 610)
    {
        service = provider;
        var root = new Grid { Margin = new Thickness(24, 14, 24, 22) };
        root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new()); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.Children.Add(Ui.Margin(Ui.Stack(Ui.Label(T["Network"]), Ui.Margin(Ui.Text(T["ConnectedDevices"], 26, bold: true), 8)), bottom: 24));
        var scroll = Ui.Scroll(list); Grid.SetRow(scroll, 1); root.Children.Add(scroll);
        refresh = Ui.Button(T["Refresh"], async () => await InitializeAsync(), true);
        var footer = Ui.Stack(Ui.Margin(refresh, 14, 13), Ui.Text(T["Privacy"], 11, Ui.Muted));
        Grid.SetRow(footer, 2); root.Children.Add(footer); SetBody(root);
        Loaded += async (_, _) => await InitializeAsync(); service.StateChanged += Changed;
        Closed += (_, _) => { closed = true; service.StateChanged -= Changed; lifetime.Cancel(); };
    }
    private void Changed(object? sender, EventArgs args) => Dispatcher.BeginInvoke(() => { if (!closed) refresh.IsEnabled = !loading && service.OperationState == OperationState.Idle; });
    public async Task InitializeAsync()
    {
        if (closed || loading) return;
        loading = true; refresh.IsEnabled = false;
        try
        {
            var result = await service.ReadDevicesAsync(lifetime.Token);
            if (closed) return;
            list.Children.Clear();
            if (!result.Result.Success) { list.Children.Add(Ui.Card(Ui.Text(T[result.Result.Code.ToString()], color: Ui.Brush("#9C5700")))); return; }
            if (result.Data is not { Count: > 0 })
            {
                list.Children.Add(Ui.Card(Ui.Stack(Ui.Margin(Ui.Text("◌", 62, Ui.Accent), 25, 20), Ui.Text(T["NoDevices"], 22, bold: true), Ui.Margin(Ui.Text(T["NoDevicesHelp"], 13, Ui.Muted), 10, 30)))); return;
            }
            foreach (var device in result.Data)
            {
                var title = Ui.Text($"{T["Device"]} {device.Number:00}", 17, bold: true);
                var lines = Ui.Stack(title, Ui.Margin(Ui.Text(string.Join("\n", device.HostNames), 13, Ui.Muted), 13),
                    Ui.Margin(Ui.Text("MAC  " + (device.MacAddress ?? T["Unavailable"]), 11, Ui.Muted), 13));
                list.Children.Add(Ui.Margin(Ui.Card(lines), bottom: 12));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            if (!closed) { list.Children.Clear(); list.Children.Add(Ui.Card(Ui.Text(T["NativeFailure"]))); }
        }
        finally { loading = false; if (!closed) refresh.IsEnabled = service.OperationState == OperationState.Idle; }
    }
}
