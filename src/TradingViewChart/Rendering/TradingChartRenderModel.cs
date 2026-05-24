using Avalonia;
using SkiaSharp;
using TradingViewChart.Indicators;
using TradingViewChart.Models;

namespace TradingViewChart.Rendering;

internal sealed class TradingChartRenderModel
{
    public required Rect Bounds { get; init; }
    public required TradingChartLayout Layout { get; init; }
    public required IReadOnlyList<CandlePoint> Data { get; init; }
    public required TradingChartSeriesMode SeriesMode { get; init; }
    public required IReadOnlyList<TradingIndicatorSnapshot> MainIndicators { get; init; }
    public required IReadOnlyList<TradingIndicatorSnapshot> SubIndicators { get; init; }
    public required IReadOnlyList<TradingIndicatorSnapshot> VisibleSubIndicators { get; init; }
    public required IReadOnlyList<TradingMarker> Markers { get; init; }
    public required IReadOnlySet<ITradingIndicator> HiddenIndicators { get; init; }
    public required IReadOnlySet<TradingSeriesKey> HiddenSeries { get; init; }
    public required int VisibleStartIndex { get; init; }
    public required int VisibleCount { get; init; }
    public required int CrosshairIndex { get; init; }
    public required int ActivePanelIndex { get; init; }
    public required bool ShowCrosshair { get; init; }
    public required ITradingIndicator? HoveredIndicator { get; init; }
    public required string? HoveredSeriesName { get; init; }
    public required Point PointerPosition { get; init; }
    public required CrosshairMode CrosshairMode { get; init; }
    public required CrosshairValueMode CrosshairValueMode { get; init; }
    public required TradingTooltipCorner TooltipCorner { get; init; }
    public required string XAxisLabelFormat { get; init; }
    public required SKColor BackgroundColor { get; init; }
    public required SKColor GridColor { get; init; }
    public required SKColor TextColor { get; init; }
    public required SKColor TooltipBackgroundColor { get; init; }
    public required SKColor TooltipTextColor { get; init; }
    public required SKColor UpColor { get; init; }
    public required SKColor DownColor { get; init; }
    public required SKColor LimitUpColor { get; init; }
    public required SKColor LimitDownColor { get; init; }
    public required IReadOnlySet<int> MarkedIndices { get; init; }
}

internal sealed class TradingIndicatorSnapshot
{
    public required ITradingIndicator Indicator { get; init; }
    public required TradingIndicatorResult Result { get; init; }
}

internal readonly record struct TradingSeriesKey(ITradingIndicator Indicator, string SeriesName);

internal readonly record struct TradingLegendHitTarget(ITradingIndicator Indicator, string? SeriesName, bool IsSubPanelToggle);

internal enum TradingChartSeriesMode
{
    Candle,
    PriceLine
}

internal enum TradingTooltipCorner
{
    LeftTop,
    RightTop
}
