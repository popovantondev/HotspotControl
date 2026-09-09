using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HotspotControl.App;

internal static class UserStartup
{
    private static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup), "HotspotControl.UserStartup.lnk");

    public static bool IsEnabled => File.Exists(ShortcutPath);

    public static void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);
            return;
        }
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException();
        var assembly = Assembly.GetExecutingAssembly().Location;
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new NotSupportedException();
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic? shortcut = null;
        try
        {
            // A per-user shortcut needs no administrator rights and supports long paths.
            shortcut = shell.CreateShortcut(ShortcutPath);
            shortcut.TargetPath = executable;
            shortcut.Arguments = Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                ? $"\"{assembly}\"" : "";
            shortcut.WorkingDirectory = AppContext.BaseDirectory;
            shortcut.IconLocation = Path.ChangeExtension(assembly, ".exe") + ",0";
            shortcut.Description = "Hotspot Control bei der Windows-Anmeldung starten";
            shortcut.Save();
        }
        finally
        {
            if (shortcut is not null) Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
