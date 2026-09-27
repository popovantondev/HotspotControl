using System.IO;
using System.Windows;
using HotspotControl.Localization;
using HotspotControl.Presentation;

namespace HotspotControl.Preview;

public partial class App : Application
{
    private PreviewPanel? panel;
    private readonly PreviewPreferences preferences = new();
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        string? Option(string name) { var index = Array.IndexOf(e.Args, name); return index >= 0 && index + 1 < e.Args.Length ? e.Args[index + 1] : null; }
        if (Option("--export") is { } destination)
        {
            try { await PreviewVerification.RunAsync(Path.GetFullPath(destination)); Shutdown(0); }
            catch (Exception error)
            {
                Directory.CreateDirectory(destination);
                File.WriteAllText(Path.Combine(destination, "verification-failure.txt"), error.ToString()); Shutdown(1);
            }
            return;
        }
        string language = Option("--language") ?? "de";
        preferences.Language = language;
        if (Option("--language") is null)
        {
            var system = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var chooser = new LanguageView(new TextCatalog(system)); chooser.ShowDialog();
            if (!chooser.Accepted) { Shutdown(); return; }
            language = chooser.SelectedLanguage; preferences.Language = language;
        }
        var scenario = Enum.TryParse<DemoScenario>(Option("--scenario"), true, out var value) ? value : DemoScenario.On;
        Open(language, scenario, e.Args.Contains("--compact"));
    }
    private void Open(string language, DemoScenario scenario, bool compact)
    {
        var previous = MainWindow;
        var oldPanel = panel;
        var text = new TextCatalog(language);
        preferences.Language = language;
        var main = new MainView(text, new DemoHotspotService(scenario), preferences, compact);
        MainWindow = main;
        main.Closed += (_, _) => { if (ReferenceEquals(MainWindow, main)) { panel?.Close(); Shutdown(); } };
        panel = new PreviewPanel(text, scenario, compact, Open, () =>
        {
            var chooser = new LanguageView(text) { Owner = main }; chooser.ShowDialog();
            if (chooser.Accepted) Open(chooser.SelectedLanguage, scenario, compact);
        });
        if (previous?.IsLoaded == true) previous.Close();
        oldPanel?.Close();
        main.Show();
        panel.Left = Math.Max(0, main.Left - panel.Width - 14); panel.Top = Math.Max(0, main.Top + 20);
        panel.WindowStartupLocation = WindowStartupLocation.Manual; panel.Show(); main.Activate();
    }
}
