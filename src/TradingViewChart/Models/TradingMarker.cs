using Avalonia.Media;

namespace TradingViewChart.Models;

public enum TradingMarkerShape
{
    UpArrow,
    DownArrow,
    Circle,
    Triangle,
    Star,
}

public enum TradingMarkerPlacement
{
    Above,
    Below,
}

public sealed class TradingMarker
{
    public DateTimeOffset Time { get; init; }

    public string? IndicatorId { get; init; }

    public string? SeriesName { get; init; }

    public TradingMarkerPlacement Placement { get; init; } = TradingMarkerPlacement.Above;

    public TradingMarkerShape Shape { get; init; } = TradingMarkerShape.Circle;

    public string? Note { get; init; }

    public Color? Color { get; init; }
}
