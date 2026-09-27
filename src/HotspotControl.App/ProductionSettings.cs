using HotspotControl.Core.Contracts;
using HotspotControl.Presentation;
using HotspotControl.Localization;

namespace HotspotControl.App;

internal sealed class ProductionSettings(AppPreferences initial) : IApplicationSettings
{
    private AppPreferences current = initial;
    public bool AutoEnableHotspot => current.AutoEnableHotspot;
    public string Language => current.Language ?? "en";
    public StartupLinkState StartupState => UserStartup.GetState() switch
    {
        StartupShortcutState.Enabled => StartupLinkState.Enabled,
        StartupShortcutState.NeedsRepair => StartupLinkState.NeedsRepair,
        StartupShortcutState.Unavailable => StartupLinkState.Unavailable,
        _ => StartupLinkState.Disabled
    };

    public MessageCode? SetStartWithWindows(bool enabled)
    {
        try { UserStartup.SetEnabled(enabled, new TextCatalog(Language)["StartWindows"]); return null; }
        catch { return MessageCode.StartupFailed; }
    }

    public MessageCode? SetAutoEnableHotspot(bool enabled)
    {
        try
        {
            current = PreferencesStore.Update(value => value with { AutoEnableHotspot = enabled });
            return null;
        }
        catch { return MessageCode.PreferencesSaveFailed; }
    }

    public MessageCode? SetLanguage(string language)
    {
        try
        {
            current = PreferencesStore.Update(value => value with { Language = language });
            return null;
        }
        catch { return MessageCode.PreferencesSaveFailed; }
    }
}
