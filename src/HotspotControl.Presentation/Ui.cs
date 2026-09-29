using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotspotControl.Presentation;

public static class Ui
{
    public static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
    public static readonly Brush Accent = Brush("#075DD1");
    public static readonly Brush Muted = Brush("#607087");
    public static TextBlock Text(string text, double size = 14, Brush? color = null, bool bold = false) => new()
    {
        Text = text, FontSize = size, Foreground = color ?? Brush("#14253D"),
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap
    };
    public static TextBlock Label(string text) => Text(text, 10, Muted, true);
    public static Button Button(string text, Action action, bool secondary = false)
    {
        var button = new Button { Content = Text(text, 13, secondary ? Brush("#14253D") : Brush("#FFFFFF"), true) };
        if (secondary) button.SetResourceReference(FrameworkElement.StyleProperty, "Secondary");
        AutomationProperties.SetName(button, text);
        button.Click += (_, _) => action();
        return button;
    }
    public static Border Card(UIElement content, int padding = 20) => new()
    {
        Background = Brush("#FFFFFF"), BorderBrush = Brush("#DBE4F0"), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(18), Padding = new Thickness(padding), Child = content
    };
    public static StackPanel Stack(params UIElement[] children)
    {
        var panel = new StackPanel(); foreach (var child in children) panel.Children.Add(child); return panel;
    }
    public static T Margin<T>(T element, double top = 0, double bottom = 0) where T : FrameworkElement
    { element.Margin = new Thickness(0, top, 0, bottom); return element; }
    public static Grid Pair(UIElement left, UIElement right)
    {
        var grid = new Grid(); grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        grid.Children.Add(left); Grid.SetColumn(right, 1); grid.Children.Add(right); return grid;
    }
    public static ScrollViewer Scroll(UIElement body) => new()
    { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 6, 0) };
}

