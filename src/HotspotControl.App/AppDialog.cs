using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Shapes;

namespace HotspotControl.App;

// Shared frame for every child window.
public abstract class AppDialog : Window
{
    protected AppDialog(Window owner, string title, double height)
    {
        Owner = owner;
        WindowAppearance.Attach(this);
        Title = title;
        Width = 440;
        Height = height;
        Icon = owner.Icon;
        FontFamily = owner.FontFamily;
        Foreground = owner.Foreground;
        Background = owner.Background;
        Resources = owner.Resources;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        UseLayoutRounding = true;
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 42,
            ResizeBorderThickness = new Thickness(0),
            GlassFrameThickness = new Thickness(0),
            UseAeroCaptionButtons = false
        });
    }

    protected void SetBody(UIElement body)
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = new Grid { Margin = new Thickness(12, 4, 8, 4) };
        header.Children.Add(new Image { Source = Icon, Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left });
        var close = new Button
        {
            Name = "DialogCloseButton",
            Style = (Style)FindResource("CaptionButton"),
            HorizontalAlignment = HorizontalAlignment.Right,
            ToolTip = "Schließen",
            Content = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(214, 235, 244)),
                StrokeThickness = 1.6,
                Data = Geometry.Parse("M1,1 L11,11 M11,1 L1,11")
            }
        };
        AutomationProperties.SetAutomationId(close, "DialogCloseButton");
        AutomationProperties.SetName(close, "Schließen");
        close.Click += (_, _) => Close();
        header.Children.Add(close);
        root.Children.Add(header);
        Grid.SetRow(body, 1);
        root.Children.Add(body);
        Content = root;
    }
}

