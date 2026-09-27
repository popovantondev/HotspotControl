using System.Collections;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;
using HotspotControl.Presentation;

namespace HotspotControl.Preview;

// Export our own WPF views, not screenshots of the user's desktop.
internal static class PreviewVerification
{
    private static int checks, images;
    private static void Check(bool value, string description)
    { if (!value) throw new InvalidOperationException(description); checks++; }
    public static async Task RunAsync(string destination)
    {
        Directory.CreateDirectory(destination);
        var neutral = TextCatalog.Resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var keys = neutral.Cast<DictionaryEntry>().Select(x => (string)x.Key).Order().ToArray();
        foreach (var language in TextCatalog.Languages)
        {
            var culture = language == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(language);
            var set = TextCatalog.Resources.GetResourceSet(culture, true, false)!;
            Check(keys.SequenceEqual(set.Cast<DictionaryEntry>().Select(x => (string)x.Key).Order()), $"Resource keys: {language}");
            foreach (var code in Enum.GetValues<MessageCode>()) Check(!string.IsNullOrWhiteSpace(new TextCatalog(language)[code.ToString()]), $"Message translation: {code}");
        }
        var demo = new DemoHotspotService(DemoScenario.Off);
        var invalid = await demo.SaveSettingsAsync("demo-context", " Bad", "", 0);
        Check(!invalid.Success && invalid.Code == MessageCode.InvalidSsid, "SSID edge spaces");
        Check((await demo.ReadSettingsAsync()).Data!.Ssid == "Demo-WLAN", "Invalid save leaves data unchanged");
        Check((await demo.SaveSettingsAsync("different-context", "Demo", "", 0)).Code == MessageCode.ContextChanged, "Context guard");
        Check((await demo.SaveSettingsAsync("demo-context", "Demo", "short", 0)).Code == MessageCode.InvalidPassword, "Password validation");
        var running = demo.SetEnabledAsync(true);
        Check((await demo.SetEnabledAsync(false)).Code == MessageCode.Busy, "Concurrent command rejected");
        Check((await running).Success, "Simulated power change");
        Check((await demo.SaveSettingsAsync("demo-context", "Demo", "", 0)).Code == MessageCode.MustTurnOff, "Save while on rejected");
        demo.Select(DemoScenario.Off);
        using (var cancellation = new CancellationTokenSource())
        {
            var pending = demo.SetEnabledAsync(true, cancellation.Token); cancellation.Cancel();
            try { await pending; Check(false, "Expected cancellation"); } catch (OperationCanceledException) { checks++; }
            Check((await demo.ReadAsync()).Data!.State == HotspotState.Off && demo.OperationState == OperationState.Idle, "Cancellation has no late effect");
        }
        Check((await demo.SaveSettingsAsync("demo-context", "Demo-Changed", "", 1)).Success, "Demo save succeeds while off");
        Check((await demo.ReadSettingsAsync()).Data!.Ssid == "Demo-Changed", "Session settings retained");
        demo.Select(DemoScenario.Denied);
        Check(!(await demo.ReadAsync()).Result.Success, "Permission scenario");
        demo.Select(DemoScenario.Timeout); await demo.ReadAsync();
        Check(demo.OperationState == OperationState.PendingAfterTimeout, "Timeout stays pending");
        await Task.Delay(5200);
        Check(demo.OperationState == OperationState.Idle && (await demo.ReadAsync()).Data!.State == HotspotState.Off, "Late completion recovers");

        foreach (var language in TextCatalog.Languages)
        {
            var text = new TextCatalog(language);
            foreach (var scale in new[] { 1.0, 1.25, 1.5, 2.0 })
            {
                var main = new MainView(text, new DemoHotspotService(), new PreviewPreferences { Language = language }); await main.RefreshAsync();
                Capture(main, destination, $"{language}-main-{scale * 100:0}", scale);
                var small = new MainView(text, new DemoHotspotService(), new PreviewPreferences { Language = language }, true); await small.RefreshAsync();
                Capture(small, destination, $"{language}-compact-{scale * 100:0}", scale);
                var settings = new SettingsView(text, new DemoHotspotService(DemoScenario.Off), new PreviewPreferences { Language = language }); await settings.InitializeAsync();
                Capture(settings, destination, $"{language}-settings-{scale * 100:0}", scale);
                var advanced = new SettingsView(text, new DemoHotspotService(DemoScenario.Off), new PreviewPreferences { Language = language }, true); await advanced.InitializeAsync();
                Capture(advanced, destination, $"{language}-advanced-{scale * 100:0}", scale);
                var devices = new DevicesView(text, new DemoHotspotService()); await devices.InitializeAsync();
                Capture(devices, destination, $"{language}-devices-{scale * 100:0}", scale);
                var empty = new DevicesView(text, new DemoHotspotService(DemoScenario.Empty)); await empty.InitializeAsync();
                Capture(empty, destination, $"{language}-empty-{scale * 100:0}", scale);
                Capture(new LanguageView(text), destination, $"{language}-language-{scale * 100:0}", scale);
                Capture(new MessageView(text, MessageCode.TimedOut), destination, $"{language}-message-{scale * 100:0}", scale);
            }
            foreach (var scenario in Enum.GetValues<DemoScenario>())
            {
                var main = new MainView(text, new DemoHotspotService(scenario), new PreviewPreferences { Language = language }); await main.RefreshAsync();
                Capture(main, destination, $"{language}-scenario-{scenario}", 1);
            }
            var productMain = new MainView(text, new DemoHotspotService(), new PreviewPreferences { Language = language }, demo: false);
            await productMain.RefreshAsync();
            Capture(productMain, destination, $"{language}-product-frame-main", 1);
            var productSettings = new SettingsView(text, new DemoHotspotService(DemoScenario.Off),
                new PreviewPreferences { Language = language }, demo: false);
            await productSettings.InitializeAsync();
            Capture(productSettings, destination, $"{language}-product-frame-settings", 1);
        }
        File.WriteAllText(Path.Combine(destination, "verification.txt"), $"{checks} checks passed. {images} WPF renders exported.\nThese are application renders at 96/120/144/192 DPI, not desktop screenshots. Actual monitor DPI changes, focus and native window chrome require manual verification.\nProduct-frame renders also use fictional demo data. No Windows hotspot service or user storage is used.\n");
    }
    private static void Capture(StyledWindow window, string directory, string name, double scale)
    {
        window.FitToScreen = false;
        var root = window.RenderRoot;
        root.Width = window.Width; root.Height = window.Height;
        root.Measure(new Size(window.Width, window.Height)); root.Arrange(new Rect(0, 0, window.Width, window.Height)); root.UpdateLayout();
        var image = new RenderTargetBitmap((int)Math.Ceiling(window.Width * scale), (int)Math.Ceiling(window.Height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        image.Render(root);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(image));
        using (var stream = File.Create(Path.Combine(directory, name + ".png"))) png.Save(stream);
        images++; window.Close();
    }
}
