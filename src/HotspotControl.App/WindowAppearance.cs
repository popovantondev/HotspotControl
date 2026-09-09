using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HotspotControl.App;

internal static class WindowAppearance
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    public static void Attach(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            // Let Windows draw smooth corners without a transparent window.
            int round = 2;
            _ = DwmSetWindowAttribute(new WindowInteropHelper(window).Handle, 33, ref round, sizeof(int));
        };
    }
}
