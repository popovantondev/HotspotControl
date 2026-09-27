using System.Globalization;
using System.Resources;

namespace HotspotControl.Localization;

public sealed class TextCatalog
{
    public static readonly string[] Languages = ["de", "ru", "en"];
    public static readonly ResourceManager Resources = new("HotspotControl.Localization.Strings", typeof(TextCatalog).Assembly);
    public string Language { get; }
    public CultureInfo Culture { get; }
    public TextCatalog(string language)
    {
        Language = Languages.Contains(language) ? language : "en";
        Culture = CultureInfo.GetCultureInfo(Language);
    }
    public string this[string key] => Resources.GetString(key, Culture) ?? throw new MissingManifestResourceException(key);
}
