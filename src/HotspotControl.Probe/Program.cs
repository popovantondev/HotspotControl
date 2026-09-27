using System.Globalization;
using System.Security.Principal;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;
using HotspotControl.Windows;

var languageIndex = Array.IndexOf(args, "--language");
var language = languageIndex >= 0 && languageIndex + 1 < args.Length
    ? args[languageIndex + 1] : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
var text = new TextCatalog(language);
using var identity = WindowsIdentity.GetCurrent();
if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.WriteLine(text["ProbeAdmin"]);
    return 3;
}
Console.WriteLine(text["ProbeTitle"]);
var result = await new WindowsHotspotService().ReadAsync();
var state = result.Data?.State switch
{
    HotspotState.On => text["On"],
    HotspotState.Off => text["Off"],
    HotspotState.InTransition => text["Transition"],
    _ => text["Unknown"]
};
var clients = result.Data?.Clients is { } count ? $"{count} / {result.Data.MaximumClients}" : text["Unavailable"];
Console.WriteLine(string.Format(text.Culture, text["ProbeState"], state));
Console.WriteLine(string.Format(text.Culture, text["ProbeDevices"], clients));
Console.WriteLine(text[result.Result.Code.ToString()]);
return result.Result.Success ? 0 : result.Result.Code switch
{
    MessageCode.NoInternetProfile => 2,
    MessageCode.AccessDenied => 4,
    _ => 5
};
