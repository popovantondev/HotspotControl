using System.IO;
using System.Text.Json;

namespace HotspotControl.App;

internal sealed record AppPreferences(bool AutoEnableHotspot = false);
internal static class PreferencesStore
{
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotspotControl", "preferences.json");
    public static string? LastReadError { get; private set; }
    public static AppPreferences Read()
    {
        try { LastReadError = null; return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FilePath)) ?? new() : new(); }
        catch (Exception) { LastReadError = "Die Starteinstellungen konnten nicht gelesen werden. Automatischer Hotspotstart bleibt aus."; return new(); }
    }
    public static void Save(AppPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(preferences));
        File.Move(temporary, FilePath, true);
    }
}
