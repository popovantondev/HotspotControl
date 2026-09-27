using System.Globalization;
using System.Security.Principal;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;
using HotspotControl.Windows;

var text = new TextCatalog(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
using var identity = WindowsIdentity.GetCurrent();
if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.WriteLine(text["ProbeAdmin"]); return 3;
}
var service = new WindowsHotspotService();
var original = await service.ReadAsync();
Console.WriteLine(text[original.Result.Code.ToString()]);
if (!args.Contains("--network-cycle")) return original.Result.Success ? 0 : 5;
if (original.Data?.State is not (HotspotState.On or HotspotState.Off))
    throw new InvalidOperationException(text["Unknown"]);
var network = await service.ReadSettingsAsync();
if (network.Data is null) throw new InvalidOperationException(text[network.Result.Code.ToString()]);
var settings = network.Data;
bool wasOn = original.Data.State == HotspotState.On;
try
{
    var off = await service.SetEnabledAsync(false);
    if (!off.Success) throw new InvalidOperationException(text[off.Code.ToString()]);
    if ((await service.ReadAsync()).Data?.State != HotspotState.Off)
        throw new InvalidOperationException(text["Unknown"]);
    Console.WriteLine(text["Disabled"]);

    // Empty password preserves the current passphrase.
    var saved = await service.SaveSettingsAsync(settings.ContextToken, settings.Ssid, "", settings.Band);
    if (!saved.Success) throw new InvalidOperationException(text[saved.Code.ToString()]);
    var reloaded = await service.ReadSettingsAsync();
    if (reloaded.Data?.Ssid != settings.Ssid || reloaded.Data.Band != settings.Band)
        throw new InvalidOperationException(text["ContextChanged"]);
    Console.WriteLine(text["SettingsSaved"]);

    var on = await service.SetEnabledAsync(true);
    if (!on.Success) throw new InvalidOperationException(text[on.Code.ToString()]);
    if ((await service.ReadAsync()).Data?.State != HotspotState.On)
        throw new InvalidOperationException(text["Unknown"]);
    Console.WriteLine(text["Enabled"]);
}
finally
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(35));
    while (service.OperationState != OperationState.Idle) await Task.Delay(200, deadline.Token);
    var restored = await service.SetEnabledAsync(wasOn);
    if (!restored.Success) throw new InvalidOperationException(text[restored.Code.ToString()]);
    var final = await service.ReadAsync();
    if (final.Data?.State != original.Data.State) throw new InvalidOperationException(text["Unknown"]);
}
return 0;
