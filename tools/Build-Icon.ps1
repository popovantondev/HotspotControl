param([string]$OutputPath = (Join-Path $PSScriptRoot '..\src\HotspotControl.App\Assets\Hotspot.ico'))
Add-Type -AssemblyName PresentationCore,WindowsBase
# Render the vector at each size. Small icons use a simpler, stronger stroke.
$frames = @()
foreach ($size in @(16,20,24,32,40,48,64,96,128,256)) {
 $visual = [System.Windows.Media.DrawingVisual]::new()
 $drawing = $visual.RenderOpen()
 $drawing.PushTransform([System.Windows.Media.ScaleTransform]::new($size / 256.0, $size / 256.0))
 $background = [System.Windows.Media.LinearGradientBrush]::new([System.Windows.Media.ColorConverter]::ConvertFromString('#17334C'), [System.Windows.Media.ColorConverter]::ConvertFromString('#086A7E'), 90)
 $drawing.DrawRoundedRectangle($background, $null, [System.Windows.Rect]::new(4,4,248,248), 52,52)
 $signal = [System.Windows.Media.LinearGradientBrush]::new([System.Windows.Media.ColorConverter]::ConvertFromString('#67FFE5'), [System.Windows.Media.ColorConverter]::ConvertFromString('#42BCE6'), 35)
 $pen = [System.Windows.Media.Pen]::new($signal, $(if ($size -le 24) { 23 } else { 20 }))
 $pen.StartLineCap = $pen.EndLineCap = [System.Windows.Media.PenLineCap]::Round
 $drawing.DrawGeometry($null, $pen, [System.Windows.Media.Geometry]::Parse('M 48,98 Q 128,25 208,98 M 80,137 Q 128,92 176,137'))
 $drawing.DrawEllipse($signal, $null, [System.Windows.Point]::new(128,183), 15,15)
 $drawing.Pop()
 $drawing.Close()
 $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[System.Windows.Media.PixelFormats]::Pbgra32)
 $bitmap.Render($visual)
 $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
 $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream = [System.IO.MemoryStream]::new()
 $encoder.Save($stream)
 $frames += ,@($size,$stream.ToArray())
 $stream.Dispose()
}
$output = [System.IO.File]::Create([System.IO.Path]::GetFullPath($OutputPath))
$writer = [System.IO.BinaryWriter]::new($output)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
 $dimension = if ($frame[0] -eq 256) { 0 } else { $frame[0] }
 $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
 $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
 $writer.Write([uint32]$frame[1].Length); $writer.Write([uint32]$offset)
 $offset += $frame[1].Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame[1]) }
$writer.Dispose()
