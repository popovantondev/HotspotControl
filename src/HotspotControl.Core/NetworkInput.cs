using HotspotControl.Core.Contracts;

namespace HotspotControl.Core;

public static class NetworkInput
{
    public static MessageCode? Validate(string? ssid, string? password)
    {
        if (ssid is null || ssid.Length is < 1 or > 32 || ssid[0] == ' ' ||
            ssid[^1] == ' ' || ssid.Any(character => character is < ' ' or > '~'))
            return MessageCode.InvalidSsid;
        if (password is null || (password.Length > 0 &&
            (password.Length is < 8 or > 63 || password.Any(character => character is < ' ' or > '~'))))
            return MessageCode.InvalidPassword;
        return null;
    }

    public static bool IsSupportedBand(int band, IReadOnlyList<int> supported) =>
        band is >= 0 and <= 2 && supported.Contains(band);
}

public static class NetworkContext
{
    public static bool Matches(string? expected, string? actual) =>
        !string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(actual) &&
        string.Equals(expected, actual, StringComparison.Ordinal);
}

public static class NetworkSettingsGuard
{
    public static MessageCode? Check(string? expectedContext, string? currentContext,
        HotspotState state, string? ssid, string? password, int band, IReadOnlyList<int> supportedBands)
    {
        var input = NetworkInput.Validate(ssid, password);
        if (input is not null) return input;
        if (!NetworkContext.Matches(expectedContext, currentContext)) return MessageCode.ContextChanged;
        if (state != HotspotState.Off) return MessageCode.MustTurnOff;
        if (!NetworkInput.IsSupportedBand(band, supportedBands)) return MessageCode.UnsupportedBand;
        return null;
    }
}
