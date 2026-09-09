using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace HotspotControl.App;

public partial class App : Application
{
    private Mutex? instance;
    private EventWaitHandle? activation;
    private RegisteredWaitHandle? activationWait;
    private bool ownsInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        using var identity = WindowsIdentity.GetCurrent();
        var userKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.User!.Value)))[..16];
        var prefix = $@"Local\HotspotControl.{userKey}";
        instance = new Mutex(true, prefix + ".Instance", out ownsInstance);
        activation = new EventWaitHandle(false, EventResetMode.AutoReset, prefix + ".Activate");
        if (!ownsInstance)
        {
            activation.Set();
            Shutdown();
            return;
        }
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var window = new MainWindow(e.Args.Contains("--no-auto-start"));
        MainWindow = window;
        activationWait = ThreadPool.RegisterWaitForSingleObject(activation, (_, _) =>
        {
            if (!Dispatcher.HasShutdownStarted) Dispatcher.InvokeAsync(window.RestoreFromTray);
        }, null, Timeout.Infinite, false);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        activationWait?.Unregister(null);
        activation?.Dispose();
        if (ownsInstance) instance?.ReleaseMutex();
        instance?.Dispose();
        base.OnExit(e);
    }
}
