using System.Globalization;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using HotspotControl.Core;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;
using HotspotControl.Presentation;
using HotspotControl.Windows;

namespace HotspotControl.App;

public partial class App : Application
{
    private InstanceCoordinator? coordinator;
    private CancellationTokenSource? automaticStart;
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private TrayController? tray;
    private MainView? main;
    private bool closed;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        TextCatalog text = new(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var userKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.User!.Value)))[..16];
            var prefix = $@"Local\HotspotControl.{userKey}";
            coordinator = InstanceCoordinator.Acquire(prefix);
            if (!coordinator.IsFirst)
            {
                coordinator.NotifyFirst(); Shutdown(); return;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var saved = PreferencesStore.Read();
            var readStatus = PreferencesStore.LastReadStatus;
            string? language = saved.Language;
            if (language is null)
            {
                var chooser = new LanguageView(text, demo: false);
                chooser.ShowDialog();
                if (!chooser.Accepted) { Shutdown(); return; }
                language = chooser.SelectedLanguage;
                try { saved = PreferencesStore.Update(current => current with { Language = language }); }
                catch { saved = saved with { Language = language, AutoEnableHotspot = false }; readStatus = PreferencesReadStatus.Unavailable; }
            }
            text = new TextCatalog(language);
            var settings = new ProductionSettings(saved);
            var service = new WindowsHotspotService();
            main = new MainView(text, service, settings, demo: false, manualIntent: () => automaticStart?.Cancel());
            MainWindow = main;
            main.StatusChanged += state => tray?.Update(state);
            main.Closed += (_, _) =>
            {
                closed = true; refreshTimer.Stop(); automaticStart?.Cancel();
                tray?.Dispose(); tray = null;
            };
            tray = new TrayController(main, text, main.RestoreFromTray);
            var selectedMain = main;
            coordinator.Listen(() =>
            {
                if (!Dispatcher.HasShutdownStarted) Dispatcher.InvokeAsync(selectedMain.RestoreFromTray);
            });
            refreshTimer.Tick += async (_, _) =>
            {
                if (!closed && automaticStart is null && service.OperationState == OperationState.Idle &&
                    !selectedMain.OwnedWindows.Cast<Window>().Any(window => window.IsVisible))
                    await selectedMain.RefreshAsync();
            };
            main.Loaded += async (_, _) =>
            {
                await selectedMain.InitialRead;
                if (closed) return;
                refreshTimer.Start();
                if (readStatus is PreferencesReadStatus.Invalid or PreferencesReadStatus.FutureVersion or PreferencesReadStatus.Unavailable)
                    selectedMain.ShowMessage(MessageCode.PreferencesReadFailed);
                else if (StartupDecision.MayAutoStart(e.Args.Contains("--no-auto-start"), readStatus, saved))
                    await AutoStartAsync(service, selectedMain, text);
            };
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }
        catch (Exception error)
        {
            MessageBox.Show($"{text["StartupFailed"]} (0x{error.HResult:X8})", "Hotspot Control",
                MessageBoxButton.OK, MessageBoxImage.Error);
            main?.Close(); Shutdown(1);
        }
    }

    private async Task AutoStartAsync(IHotspotService service, MainView window, TextCatalog text)
    {
        automaticStart = new CancellationTokenSource();
        window.ShowNotice(text["StartupPreparing"]);
        try
        {
            var result = await AutoStartPolicy.RunAsync(async token =>
            {
                var status = await service.ReadAsync(token);
                if (!status.Result.Success)
                    return new(false, status.Result.Retryable, status.Result.Code);
                if (status.Data?.State == HotspotState.On)
                    return new(true, false, MessageCode.AlreadyInRequestedState);
                if (status.Data?.State != HotspotState.Off)
                    return new(false, true, MessageCode.OperationInProgress);
                var started = await service.SetEnabledAsync(true, token);
                return new(started.Success, started.Retryable, started.Code);
            }, Task.Delay, attempt =>
            {
                if (!closed) window.ShowNotice(string.Format(text.Culture, text["StartupWaiting"], attempt + 1, 12));
            }, cancellationToken: automaticStart.Token);
            if (closed) return;
            await window.RefreshAsync();
            window.ShowMessage(result.Success && window.CurrentState != HotspotState.On
                ? MessageCode.NativeFailure : result.Code);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!closed) window.ShowMessage(MessageCode.NativeFailure); }
        finally { automaticStart?.Dispose(); automaticStart = null; }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        refreshTimer.Stop(); tray?.Dispose(); coordinator?.Dispose();
        base.OnExit(e);
    }
}
