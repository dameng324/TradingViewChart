using Avalonia.Media;
using SkiaSharp;

namespace TradingViewChart.Rendering;

internal static class TradingChartBrushExtensions
{
    public static SKColor ToSkColor(this IBrush? brush, SKColor fallback)
    {
        if (brush is ISolidColorBrush solidColorBrush)
        {
            return new SKColor(
                solidColorBrush.Color.R,
                solidColorBrush.Color.G,
                solidColorBrush.Color.B,
                solidColorBrush.Color.A
            );
        }

        return fallback;
    }
}
