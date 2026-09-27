using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using HotspotControl.Localization;

namespace HotspotControl.Presentation;

public class StyledWindow : Window
{
    protected TextCatalog T { get; }
    protected ContentControl Body { get; } = new();
    public FrameworkElement RenderRoot { get; }
    public bool Demo { get; }
    public bool FitToScreen { get; set; } = true;
    private readonly double desiredWidth, desiredHeight;
    private UIElement? normalBody;
    private bool showingTooSmall;
    private Button closeButton = null!;
    private Button? minimizeButton;
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [StructLayout(LayoutKind.Sequential)] private struct RectNative { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public RectNative Monitor, Work; public uint Flags; }

    public StyledWindow(TextCatalog text, string title, double width, double height, bool demo = true, bool canMinimize = false)
    {
        T = text; Demo = demo;
        Title = title == "Hotspot Control" ? title : $"Hotspot Control — {title}" + (demo ? " · Demo" : "");
        desiredWidth = width; desiredHeight = height;
        Width = width; Height = height; ResizeMode = ResizeMode.NoResize; WindowStyle = WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.CenterScreen; FontFamily = new FontFamily("Segoe UI"); FontSize = 14;
        Foreground = Ui.Brush("#EBF8FB"); Background = Ui.Brush("#142D42"); UseLayoutRounding = true;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/HotspotControl.Presentation;component/Theme.xaml", UriKind.Relative) });
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/HotspotControl.Presentation;component/Assets/Hotspot.ico"));
        WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 40, ResizeBorderThickness = new Thickness(0), GlassFrameThickness = new Thickness(0), UseAeroCaptionButtons = false });
        var header = new Grid { Margin = new Thickness(14, 5, 8, 3) };
        header.Children.Add(Ui.Pair(new Image { Source = Icon, Width = 18, Height = 18, HorizontalAlignment = HorizontalAlignment.Left }, new StackPanel()));
        var brand = Ui.Text(demo ? "HOTSPOT CONTROL / DEMO" : "HOTSPOT CONTROL", 9, Ui.Muted, true);
        brand.VerticalAlignment = VerticalAlignment.Center; brand.Margin = new Thickness(28, 0, 85, 0); header.Children.Add(brand);
        var captions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        if (canMinimize)
        {
            minimizeButton = Caption("−", T["Minimize"], () => WindowState = WindowState.Minimized); captions.Children.Add(minimizeButton);
        }
        closeButton = Caption("×", T["Close"], Close);
        captions.Children.Add(closeButton); header.Children.Add(captions);
        var grid = new Grid(); grid.RowDefinitions.Add(new() { Height = new GridLength(40) }); grid.RowDefinitions.Add(new());
        grid.Children.Add(header); Grid.SetRow(Body, 1); grid.Children.Add(Body);
        RenderRoot = new Border { CornerRadius = new CornerRadius(16), BorderThickness = new Thickness(1), BorderBrush = Ui.Brush("#476472"), Background = new LinearGradientBrush(Color.FromRgb(20, 43, 63), Color.FromRgb(9, 102, 115), 90), Child = grid };
        Content = RenderRoot;
        SourceInitialized += (_, _) => { int round = 2; _ = DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 33, ref round, 4); Fit(); };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(Fit);
        LocationChanged += (_, _) => { if (IsLoaded && FitToScreen) Fit(); };
    }
    private Button Caption(string glyph, string label, Action action)
    {
        var symbol = new System.Windows.Shapes.Path { Data = Geometry.Parse(glyph == "×" ? "M1,1 L11,11 M11,1 L1,11" : "M0,6 L12,6"), Stroke = Ui.Brush("#C3DFE7"), StrokeThickness = 1.5, Width = 12, Height = 12 };
        var button = new Button { Content = symbol, ToolTip = label, Style = (Style)FindResource("Caption") };
        AutomationProperties.SetName(button, label); WindowChrome.SetIsHitTestVisibleInChrome(button, true);
        button.Click += (_, _) => action(); return button;
    }
    protected void UpdateChromeLanguage(TextCatalog text)
    {
        closeButton.ToolTip = text["Close"];
        AutomationProperties.SetName(closeButton, text["Close"]);
        if (minimizeButton is not null)
        {
            minimizeButton.ToolTip = text["Minimize"];
            AutomationProperties.SetName(minimizeButton, text["Minimize"]);
        }
    }
    private void Fit()
    {
        if (!FitToScreen) return;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var width = (info.Work.Right - info.Work.Left) / dpi.DpiScaleX;
        var height = (info.Work.Bottom - info.Work.Top) / dpi.DpiScaleY;
        if (width < 400 || height < 360)
        {
            if (!showingTooSmall)
            {
                Body.Content = Ui.Margin(Ui.Stack(Ui.Text(T["TooSmall"]), Ui.Margin(Ui.Button(T["Close"], Close), 14)), 20);
                showingTooSmall = true;
            }
        }
        else if (showingTooSmall) { Body.Content = normalBody; showingTooSmall = false; }
        Width = Math.Min(desiredWidth, Math.Max(1, width));
        Height = Math.Min(desiredHeight, Math.Max(1, height));
    }
    public void SetBody(UIElement content)
    { normalBody = content; if (!showingTooSmall) Body.Content = content; }
    public void ShowChild(StyledWindow child) { child.Owner = this; child.WindowStartupLocation = WindowStartupLocation.CenterOwner; child.ShowDialog(); }
}
