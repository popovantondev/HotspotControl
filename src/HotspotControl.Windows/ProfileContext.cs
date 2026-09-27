using Windows.Networking.Connectivity;

namespace HotspotControl.Windows;

internal static class ProfileContext
{
    public static string? From(ConnectionProfile profile)
    {
        var id = profile.NetworkAdapter?.NetworkAdapterId;
        return id is null || id == Guid.Empty || string.IsNullOrEmpty(profile.ProfileName)
            ? null : $"{id:D}|{profile.ProfileName}";
    }
}
