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
    public required IReadOnlyList<TradingIndicatorTemplate> SupportedIndicators { get; init; }
    public required IReadOnlyList<TradingMarker> Markers { get; init; }
    public required IReadOnlySet<TradingSeriesKey> HiddenSeries { get; init; }
    public required int VisibleStartIndex { get; init; }
    public required int VisibleCount { get; init; }
    public required int CrosshairIndex { get; init; }
    public required int ActivePanelIndex { get; init; }
    public required bool ShowCrosshair { get; init; }
    public required ITradingIndicator? HoveredIndicator { get; init; }
    public required string? HoveredSeriesName { get; init; }
    public required Point PointerPosition { get; init; }
    public required CrosshairHintMode CrosshairHintMode { get; init; }
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
    public required TradingIndicatorItem? Item { get; init; }
    public required ITradingIndicator Indicator { get; init; }
    public required object OwnerKey { get; init; }
    public required TradingIndicatorResult Result { get; init; }

    public string DisplayName => Item?.DisplayName ?? Indicator.DisplayName;

    public bool IsHidden => Item?.IsHidden ?? false;

    public bool CanEdit => Item?.CanEdit ?? false;
}

internal readonly record struct TradingSeriesKey(object OwnerKey, string SeriesName);

internal readonly record struct TradingLegendHitTarget(TradingIndicatorItem? Item, object OwnerKey, string? SeriesName, TradingLegendAction Action);

internal enum TradingLegendAction
{
    ToggleSeries,
    IndicatorMenu
}

internal readonly record struct TradingOverlayHitTarget(TradingOverlayAction Action);

internal enum TradingOverlayAction
{
    AddIndicator
}

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
