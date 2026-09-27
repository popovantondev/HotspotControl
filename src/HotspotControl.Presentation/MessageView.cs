using System.Windows;
using HotspotControl.Core.Contracts;
using HotspotControl.Localization;

namespace HotspotControl.Presentation;

public sealed class MessageView : StyledWindow
{
    public MessageView(TextCatalog text, MessageCode code) : base(text, text["Message"], 440, 390)
    {
        var panel = Ui.Stack(Ui.Label(T["MobileHotspot"]), Ui.Margin(Ui.Text(T["Message"], 25, bold: true), 10, 22),
            Ui.Card(Ui.Text(T[code.ToString()], 15)), Ui.Margin(Ui.Button(T["Close"], Close, true), 22));
        SetBody(Ui.Scroll(new System.Windows.Controls.Border { Padding = new Thickness(24, 16, 24, 24), Child = panel }));
    }
}
