using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HotspotControl.App;

internal static class UserStartup
{
    private static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup), "HotspotControl.UserStartup.lnk");

    private static (string Target, string Arguments) Expected()
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException();
        var assembly = Assembly.GetExecutingAssembly().Location;
        var arguments = Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            ? $"\"{assembly}\"" : "";
        return (executable, arguments);
    }

    public static StartupShortcutState GetState()
    {
        if (!File.Exists(ShortcutPath)) return StartupShortcutState.Disabled;
        try
        {
            var (target, arguments) = Expected();
            var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new NotSupportedException();
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic? shortcut = null;
            try
            {
                shortcut = shell.CreateShortcut(ShortcutPath);
                return StartupShortcutPolicy.Evaluate(true, (string)shortcut.TargetPath,
                    (string)shortcut.Arguments, target, arguments, File.Exists);
            }
            finally
            {
                if (shortcut is not null) Marshal.FinalReleaseComObject(shortcut);
                Marshal.FinalReleaseComObject(shell);
            }
        }
        catch { return StartupShortcutState.Unavailable; }
    }

    public static bool IsEnabled => GetState() == StartupShortcutState.Enabled;

    public static void SetEnabled(bool enabled, string description)
    {
        if (!enabled)
        {
            if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
            return;
        }
        var (executable, arguments) = Expected();
        var assembly = Assembly.GetExecutingAssembly().Location;
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new NotSupportedException();
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic? shortcut = null;
        try
        {
            // A per-user shortcut needs no administrator rights and supports long paths.
            shortcut = shell.CreateShortcut(ShortcutPath);
            shortcut.TargetPath = executable;
            shortcut.Arguments = arguments;
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.IconLocation = Path.ChangeExtension(assembly, ".exe") + ",0";
            shortcut.Description = description;
            shortcut.Save();
        }
        finally
        {
            if (shortcut is not null) Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
