namespace HotspotControl.App;

public static class StartupDecision
{
    public static bool MayAutoStart(bool suppressed, PreferencesReadStatus status, AppPreferences preferences) =>
        !suppressed && (status is PreferencesReadStatus.Valid or PreferencesReadStatus.Missing) &&
        preferences.AutoEnableHotspot && (preferences.Language is "de" or "ru" or "en");
}
