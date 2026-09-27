using System.IO;

namespace HotspotControl.App;

public enum StartupShortcutState { Disabled, Enabled, NeedsRepair, Unavailable }

public static class StartupShortcutPolicy
{
    public static StartupShortcutState Evaluate(bool shortcutExists, string? actualTarget,
        string? actualArguments, string? expectedTarget, string expectedArguments, Func<string, bool> fileExists)
    {
        if (!shortcutExists) return StartupShortcutState.Disabled;
        if (string.IsNullOrWhiteSpace(expectedTarget) || string.IsNullOrWhiteSpace(actualTarget))
            return StartupShortcutState.Unavailable;
        return fileExists(actualTarget) &&
            string.Equals(Path.GetFullPath(actualTarget), Path.GetFullPath(expectedTarget), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(actualArguments?.Trim() ?? "", expectedArguments.Trim(), StringComparison.Ordinal)
            ? StartupShortcutState.Enabled : StartupShortcutState.NeedsRepair;
    }
}
