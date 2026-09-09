using System.IO;
using System.Text.Json;

namespace HotspotControl.App;

internal sealed record AppPreferences(bool AutoEnableHotspot = false);
internal static class PreferencesStore
{
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotspotControl", "preferences.json");
    public static AppPreferences Read()
    {
        try { return JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception) { return new(); }
    }
    public static void Save(AppPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(preferences));
        File.Move(temporary, FilePath, true);
    }
}
