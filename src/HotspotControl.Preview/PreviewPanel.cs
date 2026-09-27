using System.Windows;
using System.Windows.Controls;
using HotspotControl.Localization;
using HotspotControl.Presentation;

namespace HotspotControl.Preview;

public sealed class PreviewPanel : StyledWindow
{
    public PreviewPanel(TextCatalog text, DemoScenario scenario, bool compact, Action<string, DemoScenario, bool> apply, Action chooseLanguage)
        : base(text, text["PreviewPanel"], 360, 560)
    {
        var language = new ComboBox();
        foreach (var label in new[] { "Deutsch", "Русский", "English" }) language.Items.Add(label);
        language.SelectedIndex = Array.IndexOf(TextCatalog.Languages, text.Language);
        var scenes = new ComboBox();
        foreach (var value in Enum.GetValues<DemoScenario>())
            scenes.Items.Add(new ComboBoxItem { Tag = value, Content = text[ScenarioKey(value)] });
        scenes.SelectedIndex = (int)scenario;
        var small = new CheckBox { Content = Ui.Text(text["Compact"], 13), IsChecked = compact };
        var panel = Ui.Stack(Ui.Label("HOTSPOT CONTROL / DEMO"), Ui.Margin(Ui.Text(text["PreviewPanel"], 25, bold: true), 10),
            Ui.Margin(Ui.Text(text["PreviewHelp"], 12, Ui.Muted), 14, 23), Ui.Text(text["Language"], 12), Ui.Margin(language, 7, 16),
            Ui.Text(text["Scenario"], 12), Ui.Margin(scenes, 7, 12), small,
            Ui.Margin(Ui.Button(text["Apply"], () => apply(TextCatalog.Languages[language.SelectedIndex], (DemoScenario)((ComboBoxItem)scenes.SelectedItem).Tag, small.IsChecked == true)), 15),
            Ui.Margin(Ui.Button(text["FirstLaunch"], chooseLanguage, true), 9));
        SetBody(Ui.Scroll(new Border { Padding = new Thickness(23, 12, 23, 23), Child = panel }));
    }
    public static string ScenarioKey(DemoScenario scenario) => scenario switch
    {
        DemoScenario.On => "On", DemoScenario.Off => "Off", DemoScenario.Empty => "Empty", DemoScenario.Unknown => "Unknown",
        DemoScenario.Reading => "Reading", DemoScenario.Transition => "Transition", DemoScenario.NoInternet => "NoInternetProfile",
        DemoScenario.Denied => "AccessDenied", DemoScenario.Failure => "NativeFailure", _ => "TimedOut"
    };
}
