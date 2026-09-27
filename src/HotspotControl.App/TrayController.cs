using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;

namespace HotspotControl.App;

internal sealed class TrayController : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon tray;
    private readonly Icon activeIcon;
    private readonly Icon inactiveIcon;
    private readonly Window window;
    private readonly TextCatalog text;
    private readonly Action restore;
    private bool disposed;
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);

    public TrayController(Window window, TextCatalog text, Action restore)
    {
        this.window = window; this.text = text; this.restore = restore;
        Icon? active = null, inactive = null;
        System.Windows.Forms.NotifyIcon? notify = null;
        System.Windows.Forms.ContextMenuStrip? menu = null;
        try
        {
            active = CreateIcon(true);
            inactive = CreateIcon(false);
            notify = new System.Windows.Forms.NotifyIcon { Icon = inactive, Text = text["TrayUnknown"], Visible = true };
            menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add(text["TrayOpen"], null, (_, _) => Restore());
            menu.Items.Add(text["TrayExit"], null, (_, _) => window.Close());
            notify.ContextMenuStrip = menu;
            notify.MouseClick += (_, e) => { if (e.Button == System.Windows.Forms.MouseButtons.Left) Restore(); };
            activeIcon = active; inactiveIcon = inactive; tray = notify;
            window.StateChanged += OnStateChanged;
        }
        catch
        {
            if (notify is not null) notify.Visible = false;
            notify?.Dispose(); menu?.Dispose(); active?.Dispose(); inactive?.Dispose();
            throw;
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (window.WindowState == WindowState.Minimized) window.Hide();
    }
    private void Restore()
    {
        restore();
    }
    public void Update(HotspotState state)
    {
        if (disposed) return;
        tray.Icon = state == HotspotState.On ? activeIcon : inactiveIcon;
        tray.Text = state switch
        {
            HotspotState.On => text["TrayOn"],
            HotspotState.Off => text["TrayOff"],
            HotspotState.InTransition => text["TrayTransition"],
            _ => text["TrayUnknown"]
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
