using System.Windows;
using System.Windows.Controls;
using HotspotControl.Localization;

namespace HotspotControl.Presentation;

public sealed class LanguageView : StyledWindow
{
    public string SelectedLanguage { get; private set; }
    public bool Accepted { get; private set; }
    public LanguageView(TextCatalog text, bool demo = true) : base(text, text["ChooseLanguage"], 440, 570, demo: demo)
    { SelectedLanguage = text.Language; Build(); }
    private void Build()
    {
        var language = new TextCatalog(SelectedLanguage);
        UpdateChromeLanguage(language);
        Title = $"Hotspot Control — {language["ChooseLanguage"]}" + (Demo ? " · Demo" : "");
        var panel = Ui.Stack(Ui.Text("◎", 44, Ui.Accent), Ui.Margin(Ui.Label("HOTSPOT CONTROL"), 16),
            Ui.Margin(Ui.Text(language["ChooseLanguage"], 29, bold: true), 10), Ui.Margin(Ui.Text(language["Welcome"], 13, Ui.Muted), 8, 23));
        var names = new[] { "Deutsch", "Русский", "English" };
        for (int i = 0; i < names.Length; i++)
        {
            var code = TextCatalog.Languages[i]; bool selected = code == SelectedLanguage;
            var button = Ui.Button(names[i] + (selected ? " ✓" : ""), () => { SelectedLanguage = code; Build(); }, true);
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            button.BorderBrush = selected ? Ui.Accent : Ui.Brush("#3F6270");
            panel.Children.Add(Ui.Margin(button, bottom: 9));
        }
        panel.Children.Add(Ui.Margin(Ui.Button(language["Continue"], () => { Accepted = true; Close(); }), 12));
        if (Demo) panel.Children.Add(Ui.Margin(Ui.Text(language["Demo"], 10, Ui.Muted), 16));
        SetBody(Ui.Scroll(new Border { Padding = new Thickness(26, 12, 26, 24), Child = panel }));
    }
}
