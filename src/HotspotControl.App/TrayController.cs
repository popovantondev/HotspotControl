using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using HotspotControl.Windows;

namespace HotspotControl.App;

internal sealed class TrayController : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon tray;
    private readonly Icon activeIcon;
    private readonly Icon inactiveIcon;
    private readonly MainWindow window;
    private bool disposed;
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);

    public TrayController(MainWindow window)
    {
        this.window = window;
        activeIcon = CreateIcon(true);
        inactiveIcon = CreateIcon(false);
        tray = new System.Windows.Forms.NotifyIcon { Icon = inactiveIcon, Text = "Hotspot Control – Status unbekannt", Visible = true };
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Öffnen", null, (_, _) => Restore());
        menu.Items.Add("Beenden", null, (_, _) => window.Close());
        tray.ContextMenuStrip = menu;
        tray.MouseClick += (_, e) => { if (e.Button == System.Windows.Forms.MouseButtons.Left) Restore(); };
        window.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (window.WindowState == WindowState.Minimized) window.Hide();
    }
    private void Restore()
    {
        window.RestoreFromTray();
    }
    public void Update(HotspotState state)
    {
        if (disposed) return;
        tray.Icon = state == HotspotState.On ? activeIcon : inactiveIcon;
        tray.Text = state switch
        {
            HotspotState.On => "Hotspot Control – Eingeschaltet",
            HotspotState.Off => "Hotspot Control – Ausgeschaltet",
            HotspotState.InTransition => "Hotspot Control – Status wird geändert",
            _ => "Hotspot Control – Status unbekannt"
        };
    }
    private static Icon CreateIcon(bool active)
    {
        var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Hotspot.ico"));
        using var stream = resource.Stream;
        using var source = new Icon(stream, 32, 32);
        using var bitmap = source.ToBitmap();
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.G > 100 && pixel.G > pixel.R * 1.3)
                    bitmap.SetPixel(x, y, active ? Color.FromArgb(pixel.A, 65, 225, 155) : Color.FromArgb(pixel.A, 155, 168, 178));
            }
        var handle = bitmap.GetHicon();
        try { using var temporary = Icon.FromHandle(handle); return (Icon)temporary.Clone(); }
        finally { DestroyIcon(handle); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        window.StateChanged -= OnStateChanged;
        tray.Visible = false;
        tray.ContextMenuStrip?.Dispose(); tray.Dispose(); activeIcon.Dispose(); inactiveIcon.Dispose();
    }
}
