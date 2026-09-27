namespace HotspotControl.Presentation;

// The host supplies storage later. The preview holds only session values.
public sealed class PreviewPreferences : IApplicationSettings
{
    public bool StartWithWindows { get; set; }
    public bool AutoEnableHotspot { get; set; }
    public string Language { get; set; } = "de";
    public StartupLinkState StartupState => StartWithWindows ? StartupLinkState.Enabled : StartupLinkState.Disabled;
    public HotspotControl.Core.Contracts.MessageCode? SetStartWithWindows(bool enabled)
    { StartWithWindows = enabled; return null; }
    public HotspotControl.Core.Contracts.MessageCode? SetAutoEnableHotspot(bool enabled)
    { AutoEnableHotspot = enabled; return null; }
    public HotspotControl.Core.Contracts.MessageCode? SetLanguage(string language)
    { Language = language; return null; }
}
