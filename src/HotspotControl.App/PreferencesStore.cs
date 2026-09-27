using System.IO;
using System.Text.Json;

namespace HotspotControl.App;

public sealed record AppPreferences(bool AutoEnableHotspot = false, string? Language = null, int SchemaVersion = 1);
public enum PreferencesReadStatus { Missing, Valid, Invalid, FutureVersion, Unavailable }
public sealed record PreferencesReadResult(AppPreferences Preferences, PreferencesReadStatus Status);

public sealed class PreferencesFile(string path)
{
    public PreferencesReadResult Read()
    {
        if (!File.Exists(path)) return new(new(), PreferencesReadStatus.Missing);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(new(), PreferencesReadStatus.Invalid);
            int version = 0;
            if (root.TryGetProperty(nameof(AppPreferences.SchemaVersion), out var versionElement))
            {
                if (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out version))
                    return new(new(), PreferencesReadStatus.Invalid);
                if (version > 1) return new(new(), PreferencesReadStatus.FutureVersion);
                if (version != 1) return new(new(), PreferencesReadStatus.Invalid);
            }
            bool autoEnable = false;
            if (root.TryGetProperty(nameof(AppPreferences.AutoEnableHotspot), out var autoElement))
            {
                if (autoElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return new(new(), PreferencesReadStatus.Invalid);
                autoEnable = autoElement.GetBoolean();
            }
            string? language = null;
            if (root.TryGetProperty(nameof(AppPreferences.Language), out var languageElement))
            {
                if (languageElement.ValueKind != JsonValueKind.Null)
                {
                    if (languageElement.ValueKind != JsonValueKind.String)
                        return new(new(), PreferencesReadStatus.Invalid);
                    language = languageElement.GetString();
                    if (language is not ("de" or "ru" or "en"))
                        return new(new(), PreferencesReadStatus.Invalid);
                }
            }
            return new(new(autoEnable, language), PreferencesReadStatus.Valid);
        }
        catch (JsonException) { return new(new(), PreferencesReadStatus.Invalid); }
        catch (IOException) { return new(new(), PreferencesReadStatus.Unavailable); }
        catch (UnauthorizedAccessException) { return new(new(), PreferencesReadStatus.Unavailable); }
    }

    public AppPreferences Update(Func<AppPreferences, AppPreferences> change)
    {
        var read = Read();
        if (read.Status == PreferencesReadStatus.FutureVersion)
            throw new InvalidOperationException("Future preferences version");
        if (read.Status == PreferencesReadStatus.Unavailable)
            throw new IOException("Preferences cannot be read");
        var updated = change(read.Preferences) with { SchemaVersion = 1 };
        if (updated.Language is not (null or "de" or "ru" or "en"))
            throw new ArgumentException("Unsupported language");
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException();
        Directory.CreateDirectory(directory);
        if (read.Status == PreferencesReadStatus.Invalid && File.Exists(path))
        {
            var recovery = path + ".recovery-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            File.Copy(path, recovery);
        }
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(updated));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return updated;
    }
}

internal static class PreferencesStore
{
    private static readonly PreferencesFile Store = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotspotControl", "preferences.json"));
    public static PreferencesReadStatus LastReadStatus { get; private set; }
    public static AppPreferences Read()
    {
        var result = Store.Read(); LastReadStatus = result.Status; return result.Preferences;
    }
    public static AppPreferences Update(Func<AppPreferences, AppPreferences> change) => Store.Update(change);
}
