using Avalonia;
using SkiaSharp;
using TradingViewChart.Indicators;
using TradingViewChart.Models;

namespace TradingViewChart.Rendering;

internal sealed class TradingChartRenderer : IDisposable
{
    private readonly SKPaint _backgroundPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPaint _gridPaint = CreatePaint(SKPaintStyle.Stroke);
    private readonly SKPaint _axisTextPaint = CreateTextPaint(11f, SKTextAlign.Left);
    private readonly SKPaint _tooltipTextPaint = CreateTextPaint(12f, SKTextAlign.Left);
    private readonly SKPaint _tooltipBackgroundPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPaint _candleBodyPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPaint _wickPaint = CreatePaint(SKPaintStyle.Stroke);
    private readonly SKPaint _linePaint = CreatePaint(SKPaintStyle.Stroke);
    private readonly SKPaint _histogramPaint = CreatePaint(SKPaintStyle.Stroke);
    private readonly SKPaint _crosshairPaint = CreatePaint(SKPaintStyle.Stroke);
    private readonly SKPaint _crosshairPointPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPaint _markerPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPaint _markerTextPaint = CreateTextPaint(11f, SKTextAlign.Left);
    private readonly SKPaint _centerAxisTextPaint = CreateTextPaint(11f, SKTextAlign.Center);
    private readonly SKPaint _centerTooltipTextPaint = CreateTextPaint(12f, SKTextAlign.Center);
    private readonly SKPaint _legendBackgroundPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPaint _splitterPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPaint _valueTagBackgroundPaint = CreatePaint(SKPaintStyle.Fill);
    private readonly SKPath _linePath = new();
    private readonly SKPath _markerPath = new();

    public TradingChartRenderer()
    {
        _gridPaint.StrokeWidth = 1f;
        _gridPaint.PathEffect = SKPathEffect.CreateDash([4f, 4f], 0f);
        _wickPaint.IsAntialias = false;
        _wickPaint.StrokeWidth = 1f;
        _linePaint.IsAntialias = true;
        _histogramPaint.IsAntialias = true;
        _crosshairPaint.StrokeWidth = 1f;
        _crosshairPaint.PathEffect = SKPathEffect.CreateDash([6f, 4f], 0f);
    }

    public void Dispose()
    {
        _backgroundPaint.Dispose();
        _gridPaint.Dispose();
        _axisTextPaint.Dispose();
        _tooltipTextPaint.Dispose();
        _tooltipBackgroundPaint.Dispose();
        _candleBodyPaint.Dispose();
        _wickPaint.Dispose();
        _linePaint.Dispose();
        _histogramPaint.Dispose();
        _crosshairPaint.Dispose();
        _crosshairPointPaint.Dispose();
        _markerPaint.Dispose();
        _markerTextPaint.Dispose();
        _centerAxisTextPaint.Dispose();
        _centerTooltipTextPaint.Dispose();
        _legendBackgroundPaint.Dispose();
        _splitterPaint.Dispose();
        _valueTagBackgroundPaint.Dispose();
        _linePath.Dispose();
        _markerPath.Dispose();
    }

    public void Render(SKCanvas canvas, TradingChartRenderModel model)
    {
        var layout = model.Layout;
        using var _ = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate((float)model.Bounds.X, (float)model.Bounds.Y);
        canvas.ClipRect(SKRect.Create(0f, 0f, (float)model.Bounds.Width, (float)model.Bounds.Height));

        ConfigureCommonPaints(model);
        DrawBackground(canvas, layout);

        if (model.Data.Count == 0 || layout.Panels.Count == 0)
        {
            DrawEmptyState(canvas, layout);
            return;
        }

        var visibleStart = Math.Clamp(model.VisibleStartIndex, 0, Math.Max(0, model.Data.Count - 1));
        var visibleEnd = Math.Min(model.Data.Count - 1, visibleStart + model.VisibleCount - 1);
        var barWidth = (float)Math.Max(2d, Math.Min(18d, layout.Panels[0].BodyBounds.Width / Math.Max(1, model.VisibleCount) * 0.72d));

        var mainPanel = layout.Panels[0];
        var mainRange = CalculateMainRange(model, visibleStart, visibleEnd, 10);
        DrawGrid(canvas, mainPanel, mainRange);
        DrawMainPanelTitle(canvas, mainPanel, model);
        if (model.SeriesMode == TradingChartSeriesMode.Candle)
        {
            DrawCandles(canvas, mainPanel, mainRange, model, visibleStart, visibleEnd, barWidth);
        }
        else
        {
            DrawPriceSeries(canvas, mainPanel, mainRange, model, visibleStart, visibleEnd);
        }
        DrawMainIndicators(canvas, mainPanel, mainRange, model, visibleStart, visibleEnd);
        DrawMarkerFlags(canvas, mainPanel, mainRange, model, visibleStart, visibleEnd);
        DrawAxisValues(canvas, mainPanel, mainRange);
        DrawLatestValueTags(canvas, mainPanel, mainRange, GetLatestValueTags(model.MainIndicators, model, visibleEnd), model);

        for (var i = 0; i < model.VisibleSubIndicators.Count && i + 1 < layout.Panels.Count; i++)
        {
            var panel = layout.Panels[i + 1];
            var snapshot = model.VisibleSubIndicators[i];
            var range = CalculateIndicatorRange(snapshot, visibleStart, visibleEnd, 6);
            DrawGrid(canvas, panel, range);
            DrawSubPanelTitle(canvas, panel, model, snapshot);
            DrawSubPanel(canvas, panel, snapshot, range, model, visibleStart, visibleEnd, barWidth);
            DrawAxisValues(canvas, panel, range);
            DrawLatestValueTags(canvas, panel, range, GetLatestValueTags([snapshot], model, visibleEnd), model);
        }

        DrawMarkers(canvas, layout, mainRange, model, visibleStart, visibleEnd);
        DrawSplitters(canvas, layout);
        DrawXAxis(canvas, layout, model, visibleStart, visibleEnd);
        DrawCrosshair(canvas, layout, mainRange, model, visibleStart, visibleEnd);
        DrawTooltip(canvas, layout, model);
    }

    public TradingLegendHitTarget? HitTestLegend(TradingChartRenderModel model, Point position)
    {
        if (model.Bounds.Width <= 0d || model.Bounds.Height <= 0d)
        {
            return null;
        }

        ConfigureCommonPaints(model);
        var items = BuildLegendItems(model);
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Bounds.Contains((float)position.X, (float)position.Y))
            {
                return new TradingLegendHitTarget(items[i].Indicator, items[i].SeriesName, items[i].IsSubPanelToggle);
            }
        }

        return null;
    }

    public TradingLegendHitTarget? HitTestSeries(TradingChartRenderModel model, Point position)
    {
        if (model.Data.Count == 0 || model.Layout.Panels.Count == 0)
        {
            return null;
        }

        var visibleStart = Math.Clamp(model.VisibleStartIndex, 0, Math.Max(0, model.Data.Count - 1));
        var visibleEnd = Math.Min(model.Data.Count - 1, visibleStart + model.VisibleCount - 1);
        var mainRange = CalculateMainRange(model, visibleStart, visibleEnd, 10);

        for (var snapshotIndex = 0; snapshotIndex < model.MainIndicators.Count; snapshotIndex++)
        {
            var snapshot = model.MainIndicators[snapshotIndex];
            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                if (series.RenderStyle != IndicatorRenderStyle.Line ||
                    model.HiddenSeries.Contains(new TradingSeriesKey(snapshot.Indicator, series.Name)))
                {
                    continue;
                }

                if (HitTestSeriesLine(position, model.Layout.Panels[0].BodyBounds, mainRange, series, visibleStart, visibleEnd, 6f))
                {
                    return new TradingLegendHitTarget(snapshot.Indicator, series.Name, false);
                }
            }
        }

        for (var panelIndex = 0; panelIndex < model.VisibleSubIndicators.Count && panelIndex + 1 < model.Layout.Panels.Count; panelIndex++)
        {
            var snapshot = model.VisibleSubIndicators[panelIndex];
            var panel = model.Layout.Panels[panelIndex + 1];
            var range = CalculateIndicatorRange(snapshot, visibleStart, visibleEnd, 6);
            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                if (series.RenderStyle != IndicatorRenderStyle.Line)
                {
                    continue;
                }

                if (HitTestSeriesLine(position, panel.BodyBounds, range, series, visibleStart, visibleEnd, 6f))
                {
                    return new TradingLegendHitTarget(snapshot.Indicator, series.Name, false);
                }
            }
        }

        return null;
    }

    private static SKPaint CreatePaint(SKPaintStyle style)
    {
        return new SKPaint
        {
            Style = style,
            IsAntialias = true
        };
    }

    private static SKPaint CreateTextPaint(float size, SKTextAlign align)
    {
        return new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            TextSize = size,
            TextAlign = align
        };
    }

    private void ConfigureCommonPaints(TradingChartRenderModel model)
    {
        _backgroundPaint.Color = model.BackgroundColor;
        _gridPaint.Color = model.GridColor;
        _axisTextPaint.Color = model.TextColor;
        _centerAxisTextPaint.Color = model.TextColor;
        _tooltipTextPaint.Color = model.TooltipTextColor;
        _centerTooltipTextPaint.Color = model.TooltipTextColor;
        _tooltipBackgroundPaint.Color = model.TooltipBackgroundColor;
        _crosshairPaint.Color = model.TextColor.WithAlpha(180);
        _crosshairPointPaint.Color = model.TextColor;
        _markerPaint.Color = SKColors.Gold;
        _markerTextPaint.Color = model.TextColor;
        _splitterPaint.Color = model.GridColor.WithAlpha(140);
    }

    private void DrawBackground(SKCanvas canvas, TradingChartLayout layout)
    {
        canvas.DrawRect(SKRect.Create(0f, 0f, (float)layout.ControlBounds.Width, (float)layout.ControlBounds.Height), _backgroundPaint);
    }

    private void DrawEmptyState(SKCanvas canvas, TradingChartLayout layout)
    {
        canvas.DrawText("No trading data", (float)(layout.ControlBounds.Width / 2d), (float)(layout.ControlBounds.Height / 2d), _centerAxisTextPaint);
    }

    private void DrawGrid(SKCanvas canvas, TradingChartPanelLayout panel, TradingValueRange range)
    {
        var body = panel.BodyBounds;
        var xStep = body.Width / 4d;
        for (var i = 0; i <= 4; i++)
        {
            var x = (float)(body.X + (xStep * i));
            canvas.DrawLine(x, (float)body.Top, x, (float)body.Bottom, _gridPaint);
        }

        for (var i = 0; i < range.Ticks.Count; i++)
        {
            var y = GetY(body, range, range.Ticks[i]);
            canvas.DrawLine((float)body.Left, y, (float)body.Right, y, _gridPaint);
        }

        canvas.DrawLine((float)panel.AxisBounds.Left, (float)panel.Bounds.Top, (float)panel.AxisBounds.Left, (float)panel.Bounds.Bottom, _gridPaint);
    }

    private void DrawMainPanelTitle(SKCanvas canvas, TradingChartPanelLayout panel, TradingChartRenderModel model)
    {
        var y = (float)panel.Bounds.Top + 15f;
        _axisTextPaint.Color = model.TextColor;
        canvas.DrawText("PRICE", (float)panel.Bounds.Left + 4f, y, _axisTextPaint);

        var items = BuildLegendItems(model);
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].PanelIndex == 0)
            {
                DrawLegendItem(canvas, items[i], model, y);
            }
        }
    }

    private void DrawSubPanelTitle(SKCanvas canvas, TradingChartPanelLayout panel, TradingChartRenderModel model, TradingIndicatorSnapshot snapshot)
    {
        var items = BuildLegendItems(model);
        var baselineY = (float)panel.Bounds.Top + 15f;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].PanelIndex == panel.Index && ReferenceEquals(items[i].Indicator, snapshot.Indicator))
            {
                DrawLegendItem(canvas, items[i], model, baselineY);
                break;
            }
        }
    }

    private void DrawLegendItem(SKCanvas canvas, LegendItem item, TradingChartRenderModel model, float baselineY)
    {
        var isHovered = ReferenceEquals(model.HoveredIndicator, item.Indicator) &&
            string.Equals(model.HoveredSeriesName, item.SeriesName, StringComparison.Ordinal);
        var baseColor = item.IsHidden ? new SKColor(148, 163, 184, 60) : new SKColor(148, 163, 184, 28);
        var hoverColor = item.IsHidden ? new SKColor(148, 163, 184, 92) : new SKColor(148, 163, 184, 54);
        _legendBackgroundPaint.Color = isHovered ? hoverColor : baseColor;
        canvas.DrawRoundRect(item.Bounds, 5f, 5f, _legendBackgroundPaint);

        var textX = item.Bounds.Left + 6f;
        for (var i = 0; i < item.Segments.Count; i++)
        {
            _axisTextPaint.Color = item.Segments[i].Color;
            _axisTextPaint.FakeBoldText = item.Segments[i].IsBold;
            canvas.DrawText(item.Segments[i].Text, textX, baselineY, _axisTextPaint);
            textX += _axisTextPaint.MeasureText(item.Segments[i].Text);
            if (i < item.Segments.Count - 1)
            {
                textX += 8f;
            }
        }

        _axisTextPaint.FakeBoldText = false;
        _axisTextPaint.Color = model.TextColor;
    }

    private void DrawCandles(
        SKCanvas canvas,
        TradingChartPanelLayout panel,
        TradingValueRange range,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd,
        float barWidth)
    {
        for (var index = visibleStart; index <= visibleEnd; index++)
        {
            var point = model.Data[index];
            var x = GetX(panel.BodyBounds, visibleStart, model.VisibleCount, index);
            var openY = GetY(panel.BodyBounds, range, point.Open);
            var closeY = GetY(panel.BodyBounds, range, point.Close);
            var highY = GetY(panel.BodyBounds, range, point.High);
            var lowY = GetY(panel.BodyBounds, range, point.Low);
            var color = ResolveCandleColor(model, point);
            _candleBodyPaint.Color = color;
            _wickPaint.Color = color;

            canvas.DrawLine(x, highY, x, lowY, _wickPaint);

            var top = Math.Min(openY, closeY);
            var bottom = Math.Max(openY, closeY);
            var height = Math.Max(1f, bottom - top);
            canvas.DrawRect(SKRect.Create(x - (barWidth / 2f), top, barWidth, height), _candleBodyPaint);
        }
    }

    private void DrawPriceSeries(
        SKCanvas canvas,
        TradingChartPanelLayout panel,
        TradingValueRange range,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd)
    {
        _linePaint.Color = model.TextColor.WithAlpha(220);
        _linePaint.StrokeWidth = 1.8f;
        _linePath.Rewind();
        var started = false;

        for (var index = visibleStart; index <= visibleEnd; index++)
        {
            var point = model.Data[index];
            var x = GetX(panel.BodyBounds, visibleStart, model.VisibleCount, index);
            var y = GetY(panel.BodyBounds, range, point.Close);
            if (!started)
            {
                _linePath.MoveTo(x, y);
                started = true;
            }
            else
            {
                _linePath.LineTo(x, y);
            }
        }

        if (started)
        {
            canvas.DrawPath(_linePath, _linePaint);
        }
    }

    private void DrawMainIndicators(
        SKCanvas canvas,
        TradingChartPanelLayout panel,
        TradingValueRange range,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd)
    {
        for (var snapshotIndex = 0; snapshotIndex < model.MainIndicators.Count; snapshotIndex++)
        {
            var snapshot = model.MainIndicators[snapshotIndex];
            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                if (series.RenderStyle != IndicatorRenderStyle.Line || model.HiddenSeries.Contains(new TradingSeriesKey(snapshot.Indicator, series.Name)))
                {
                    continue;
                }

                DrawSeriesLine(
                    canvas,
                    panel.BodyBounds,
                    range,
                    series,
                    visibleStart,
                    visibleEnd);
            }
        }
    }

    private void DrawSubPanel(
        SKCanvas canvas,
        TradingChartPanelLayout panel,
        TradingIndicatorSnapshot snapshot,
        TradingValueRange range,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd,
        float barWidth)
    {
        for (var i = 0; i < snapshot.Result.Series.Count; i++)
        {
            var series = snapshot.Result.Series[i];
            if (series.RenderStyle == IndicatorRenderStyle.Histogram)
            {
                DrawHistogram(canvas, panel.BodyBounds, range, series, model, visibleStart, visibleEnd, barWidth);
            }
            else
            {
                DrawSeriesLine(
                    canvas,
                    panel.BodyBounds,
                    range,
                    series,
                    visibleStart,
                    visibleEnd);
            }
        }
    }

    private void DrawSeriesLine(
        SKCanvas canvas,
        Rect bodyBounds,
        TradingValueRange range,
        TradingIndicatorSeries series,
        int visibleStart,
        int visibleEnd)
    {
        _linePaint.Color = ToSkColor(series.Stroke);
        _linePaint.StrokeWidth = (float)Math.Max(1d, series.Thickness);
        _linePath.Rewind();
        var started = false;

        for (var index = visibleStart; index <= visibleEnd && index < series.Values.Count; index++)
        {
            var value = series.Values[index];
            if (!value.HasValue)
            {
                continue;
            }

            var x = GetX(bodyBounds, visibleStart, visibleEnd - visibleStart + 1, index);
            var y = GetY(bodyBounds, range, value.Value);
            if (!started)
            {
                _linePath.MoveTo(x, y);
                started = true;
            }
            else
            {
                _linePath.LineTo(x, y);
            }
        }

        if (started)
        {
            canvas.DrawPath(_linePath, _linePaint);
        }
    }

    private void DrawHistogram(
        SKCanvas canvas,
        Rect bodyBounds,
        TradingValueRange range,
        TradingIndicatorSeries series,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd,
        float barWidth)
    {
        var zeroY = GetY(bodyBounds, range, 0d);
        _histogramPaint.StrokeWidth = Math.Max(1f, barWidth);

        for (var index = visibleStart; index <= visibleEnd && index < series.Values.Count; index++)
        {
            var value = series.Values[index];
            if (!value.HasValue)
            {
                continue;
            }

            var x = GetX(bodyBounds, visibleStart, visibleEnd - visibleStart + 1, index);
            var y = GetY(bodyBounds, range, value.Value);
            _histogramPaint.Color = series.UsePriceDirectionColors
                ? ResolveCandleColor(model, model.Data[index])
                : value.Value >= 0d ? model.UpColor : model.DownColor;
            canvas.DrawLine(x, zeroY, x, y, _histogramPaint);
        }
    }

    private void DrawMarkerFlags(
        SKCanvas canvas,
        TradingChartPanelLayout panel,
        TradingValueRange range,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd)
    {
        foreach (var index in model.MarkedIndices)
        {
            if (index < visibleStart || index > visibleEnd)
            {
                continue;
            }

            var x = GetX(panel.BodyBounds, visibleStart, model.VisibleCount, index);
            var y = GetY(panel.BodyBounds, range, model.Data[index].High) - 8f;
            _markerPath.Rewind();
            _markerPath.MoveTo(x, y);
            _markerPath.LineTo(x - 5f, y - 9f);
            _markerPath.LineTo(x + 5f, y - 9f);
            _markerPath.Close();
            canvas.DrawPath(_markerPath, _markerPaint);
        }
    }

    private void DrawMarkers(
        SKCanvas canvas,
        TradingChartLayout layout,
        TradingValueRange mainRange,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd)
    {
        if (model.Markers.Count == 0)
        {
            return;
        }

        for (var i = 0; i < model.Markers.Count; i++)
        {
            var marker = model.Markers[i];
            var index = FindIndexByTime(model.Data, marker.Time, visibleStart, visibleEnd);
            if (index < 0)
            {
                continue;
            }

            var anchor = ResolveMarkerAnchor(layout, mainRange, model, marker, index, visibleStart, visibleEnd);
            if (!anchor.HasValue)
            {
                continue;
            }

            var x = GetX(layout.Panels[0].BodyBounds, visibleStart, model.VisibleCount, index);
            var offset = marker.Placement == TradingMarkerPlacement.Above ? -16f : 16f;
            var y = anchor.Value.Y + offset;
            var color = marker.Color.HasValue ? ToSkColor(marker.Color.Value) : new SKColor(245, 158, 11);
            _markerPaint.Color = color;
            DrawMarkerShape(canvas, marker.Shape, x, y, 6f);

            if (!string.IsNullOrWhiteSpace(marker.Note))
            {
                var noteY = marker.Placement == TradingMarkerPlacement.Above ? y - 8f : y + 14f;
                canvas.DrawText(marker.Note, x + 8f, noteY, _markerTextPaint);
            }
        }
    }

    private void DrawAxisValues(SKCanvas canvas, TradingChartPanelLayout panel, TradingValueRange range)
    {
        var axisX = (float)panel.AxisBounds.Left + 6f;
        for (var i = 0; i < range.Ticks.Count; i++)
        {
            var value = range.Ticks[i];
            var y = GetY(panel.BodyBounds, range, value);
            var baseline = y + (i == 0 ? 12f : i == range.Ticks.Count - 1 ? -2f : 4f);
            canvas.DrawText(FormatAxisValue(value, range.Step), axisX, baseline, _axisTextPaint);
        }
    }

    private void DrawLatestValueTags(
        SKCanvas canvas,
        TradingChartPanelLayout panel,
        TradingValueRange range,
        IReadOnlyList<LatestValueTag> tags,
        TradingChartRenderModel model)
    {
        if (tags.Count == 0)
        {
            return;
        }

        var placements = new List<LatestValueTagPlacement>(tags.Count);
        for (var i = 0; i < tags.Count; i++)
        {
            placements.Add(new LatestValueTagPlacement(tags[i], GetY(panel.BodyBounds, range, tags[i].Value)));
        }

        placements.Sort((left, right) => left.Y.CompareTo(right.Y));
        const float minGap = 20f;
        var minY = (float)panel.BodyBounds.Top + 9f;
        var maxY = (float)panel.BodyBounds.Bottom - 9f;

        for (var i = 0; i < placements.Count; i++)
        {
            var y = Math.Clamp(placements[i].Y, minY, maxY);
            if (i > 0 && y - placements[i - 1].Y < minGap)
            {
                y = Math.Min(maxY, placements[i - 1].Y + minGap);
            }

            placements[i] = placements[i] with { Y = y };
        }

        for (var i = placements.Count - 2; i >= 0; i--)
        {
            if (placements[i + 1].Y - placements[i].Y < minGap)
            {
                placements[i] = placements[i] with { Y = Math.Max(minY, placements[i + 1].Y - minGap) };
            }
        }

        for (var i = 0; i < placements.Count; i++)
        {
            var label = placements[i].Tag.Text;
            var width = _tooltipTextPaint.MeasureText(label) + 10f;
            var rect = SKRect.Create((float)panel.AxisBounds.Left + 2f, placements[i].Y - 9f, width, 18f);
            _valueTagBackgroundPaint.Color = placements[i].Tag.Color;
            canvas.DrawRoundRect(rect, 4f, 4f, _valueTagBackgroundPaint);
            _tooltipTextPaint.Color = SKColors.White;
            _tooltipTextPaint.FakeBoldText = false;
            canvas.DrawText(label, rect.Left + 5f, rect.Bottom - 5f, _tooltipTextPaint);
        }

        _tooltipTextPaint.FakeBoldText = false;
        _tooltipTextPaint.Color = model.TooltipTextColor;
    }

    private void DrawSplitters(SKCanvas canvas, TradingChartLayout layout)
    {
        for (var i = 0; i < layout.Splitters.Count; i++)
        {
            var splitter = layout.Splitters[i].Bounds;
            var lineY = (float)(splitter.Y + (splitter.Height / 2d));
            canvas.DrawLine((float)splitter.Left + 20f, lineY, (float)splitter.Right - 20f, lineY, _splitterPaint);
        }
    }

    private void DrawXAxis(SKCanvas canvas, TradingChartLayout layout, TradingChartRenderModel model, int visibleStart, int visibleEnd)
    {
        var tickCount = Math.Min(5, visibleEnd - visibleStart + 1);
        if (tickCount <= 0)
        {
            return;
        }

        for (var tick = 0; tick < tickCount; tick++)
        {
            var relative = tickCount == 1 ? 0d : tick / (double)(tickCount - 1);
            var index = visibleStart + (int)Math.Round((visibleEnd - visibleStart) * relative);
            index = Math.Clamp(index, visibleStart, visibleEnd);
            var x = GetX(layout.XAxisBounds, visibleStart, model.VisibleCount, index);
            var label = model.Data[index].Time.ToString(model.XAxisLabelFormat);
            _axisTextPaint.TextAlign = tick == 0 ? SKTextAlign.Left : tick == tickCount - 1 ? SKTextAlign.Right : SKTextAlign.Center;
            canvas.DrawText(label, x, (float)layout.XAxisBounds.Bottom - 6f, _axisTextPaint);
        }

        _axisTextPaint.TextAlign = SKTextAlign.Left;
    }

    private void DrawCrosshair(
        SKCanvas canvas,
        TradingChartLayout layout,
        TradingValueRange mainRange,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd)
    {
        if (!model.ShowCrosshair || model.CrosshairIndex < visibleStart || model.CrosshairIndex > visibleEnd || layout.Panels.Count == 0)
        {
            return;
        }

        var mainPanel = layout.Panels[0];
        var x = GetX(mainPanel.BodyBounds, visibleStart, model.VisibleCount, model.CrosshairIndex);
        var target = ResolveCrosshairTarget(layout, mainRange, model, visibleStart, visibleEnd);
        if (!target.HasValue)
        {
            return;
        }

        canvas.DrawLine(x, (float)layout.Panels[0].BodyBounds.Top, x, (float)layout.XAxisBounds.Top, _crosshairPaint);
        canvas.DrawLine((float)target.Value.Panel.BodyBounds.Left, target.Value.Y, (float)target.Value.Panel.BodyBounds.Right, target.Value.Y, _crosshairPaint);
        canvas.DrawCircle(x, target.Value.Y, 3.5f, _crosshairPointPaint);

        var label = model.Data[model.CrosshairIndex].Time.ToString(model.XAxisLabelFormat);
        var textWidth = _tooltipTextPaint.MeasureText(label);
        var rect = SKRect.Create(x - (textWidth / 2f) - 6f, (float)layout.XAxisBounds.Top + 2f, textWidth + 12f, 18f);
        canvas.DrawRect(rect, _tooltipBackgroundPaint);
        canvas.DrawText(label, rect.MidX, rect.Bottom - 5f, _centerTooltipTextPaint);

        var valueLabel = FormatValue(target.Value.Value);
        var valueWidth = _tooltipTextPaint.MeasureText(valueLabel);
        var valueRect = SKRect.Create((float)(target.Value.Panel.AxisBounds.Left + 2d), target.Value.Y - 9f, valueWidth + 10f, 18f);
        canvas.DrawRect(valueRect, _tooltipBackgroundPaint);
        canvas.DrawText(valueLabel, valueRect.Left + 5f, valueRect.Bottom - 5f, _tooltipTextPaint);
    }

    private void DrawTooltip(SKCanvas canvas, TradingChartLayout layout, TradingChartRenderModel model)
    {
        if (!model.ShowCrosshair || model.CrosshairIndex < 0 || model.CrosshairIndex >= model.Data.Count)
        {
            return;
        }

        var point = model.Data[model.CrosshairIndex];
        if (model.SeriesMode == TradingChartSeriesMode.PriceLine)
        {
            Span<string> priceLines =
            [
                $"Time  {point.Time.ToString(model.XAxisLabelFormat)}",
                $"Price  {point.Close:F2}"
            ];

            var priceWidth = 0f;
            for (var i = 0; i < priceLines.Length; i++)
            {
                priceWidth = Math.Max(priceWidth, _tooltipTextPaint.MeasureText(priceLines[i]));
            }

            priceWidth += 18f;
            var priceHeight = 16f + (priceLines.Length * 16f);
            var pricePosition = ResolveTooltipPosition(model, layout, priceWidth, priceHeight);
            var priceRect = SKRect.Create(pricePosition.X, pricePosition.Y, priceWidth, priceHeight);
            canvas.DrawRoundRect(priceRect, 8f, 8f, _tooltipBackgroundPaint);

            for (var i = 0; i < priceLines.Length; i++)
            {
                canvas.DrawText(priceLines[i], priceRect.Left + 9f, priceRect.Top + 18f + (i * 16f), _tooltipTextPaint);
            }

            return;
        }

        var baseline = ResolveBaseline(model.Data, model.CrosshairIndex);
        var changePercentage = baseline == 0d ? 0d : ((point.Close - baseline) / baseline) * 100d;
        Span<string> lines =
        [
            $"Time  {point.Time.ToString(model.XAxisLabelFormat)}",
            $"O/H/L/C  {point.Open:F2} / {point.High:F2} / {point.Low:F2} / {point.Close:F2}",
            $"Change  {changePercentage:+0.00;-0.00;0.00}%",
            $"Turnover  {FormatVolume(point.Turnover)}",
            $"Volume  {FormatVolume(point.Volume)}"
        ];

        var width = 0f;
        for (var i = 0; i < lines.Length; i++)
        {
            width = Math.Max(width, _tooltipTextPaint.MeasureText(lines[i]));
        }

        width += 18f;
        var height = 16f + (lines.Length * 16f);
        var position = ResolveTooltipPosition(model, layout, width, height);
        var rect = SKRect.Create(position.X, position.Y, width, height);
        canvas.DrawRoundRect(rect, 8f, 8f, _tooltipBackgroundPaint);

        for (var i = 0; i < lines.Length; i++)
        {
            canvas.DrawText(lines[i], rect.Left + 9f, rect.Top + 18f + (i * 16f), _tooltipTextPaint);
        }
    }

    private static SKPoint ResolveTooltipPosition(TradingChartRenderModel model, TradingChartLayout layout, float width, float height)
    {
        return model.CrosshairMode switch
        {
            CrosshairMode.FollowMouse => new SKPoint(
                (float)Math.Clamp(model.PointerPosition.X + 16d, 4d, layout.ControlBounds.Width - width - 4d),
                (float)Math.Clamp(model.PointerPosition.Y + 16d, 34d, layout.ControlBounds.Height - height - 4d)),
            _ => new SKPoint(10f, 34f)
        };
    }

    private List<LegendItem> BuildLegendItems(TradingChartRenderModel model)
    {
        var items = new List<LegendItem>();
        if (model.Layout.Panels.Count == 0)
        {
            return items;
        }

        var mainPanel = model.Layout.Panels[0];
        var x = (float)mainPanel.Bounds.Left + 42f;
        var top = (float)mainPanel.Bounds.Top + 3f;

        for (var snapshotIndex = 0; snapshotIndex < model.MainIndicators.Count; snapshotIndex++)
        {
            var snapshot = model.MainIndicators[snapshotIndex];
            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                var key = new TradingSeriesKey(snapshot.Indicator, series.Name);
                var segments = new[]
                {
                    new LegendSegment(
                        BuildSeriesValueText(series, model.CrosshairIndex),
                        model.HiddenSeries.Contains(key) ? InactiveColor : ToSkColor(series.Stroke),
                        false)
                };
                var width = MeasureLegendSegments(segments) + 12f;
                var bounds = SKRect.Create(x, top, width, 18f);
                items.Add(new LegendItem(0, snapshot.Indicator, series.Name, false, model.HiddenSeries.Contains(key), bounds, segments));
                x = bounds.Right + 6f;
            }
        }

        for (var subIndex = 0; subIndex < model.SubIndicators.Count; subIndex++)
        {
            var snapshot = model.SubIndicators[subIndex];
            if (!model.HiddenIndicators.Contains(snapshot.Indicator))
            {
                continue;
            }

            var segments = new[] { new LegendSegment(snapshot.Indicator.DisplayName, InactiveColor, false) };
            var width = MeasureLegendSegments(segments) + 12f;
            var bounds = SKRect.Create(x, top, width, 18f);
            items.Add(new LegendItem(0, snapshot.Indicator, null, true, true, bounds, segments));
            x = bounds.Right + 6f;
        }

        for (var panelIndex = 0; panelIndex < model.VisibleSubIndicators.Count && panelIndex + 1 < model.Layout.Panels.Count; panelIndex++)
        {
            var panel = model.Layout.Panels[panelIndex + 1];
            var snapshot = model.VisibleSubIndicators[panelIndex];
            var segments = BuildSubPanelSegments(snapshot, model);
            var width = MeasureLegendSegments(segments) + 12f;
            var bounds = SKRect.Create((float)panel.Bounds.Left + 4f, (float)panel.Bounds.Top + 3f, width, 18f);
            items.Add(new LegendItem(panel.Index, snapshot.Indicator, null, true, false, bounds, segments));
        }

        return items;
    }

    private LegendSegment[] BuildSubPanelSegments(TradingIndicatorSnapshot snapshot, TradingChartRenderModel model)
    {
        var segments = new LegendSegment[snapshot.Result.Series.Count];
        for (var i = 0; i < snapshot.Result.Series.Count; i++)
        {
            var series = snapshot.Result.Series[i];
            segments[i] = new LegendSegment(
                BuildSeriesValueText(series, model.CrosshairIndex),
                ToSkColor(series.Stroke),
                false);
        }

        return segments;
    }

    private List<LatestValueTag> GetLatestValueTags(IReadOnlyList<TradingIndicatorSnapshot> snapshots, TradingChartRenderModel model, int dataIndex)
    {
        var tags = new List<LatestValueTag>();
        for (var snapshotIndex = 0; snapshotIndex < snapshots.Count; snapshotIndex++)
        {
            var snapshot = snapshots[snapshotIndex];
            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                if (dataIndex < 0 || dataIndex >= series.Values.Count)
                {
                    continue;
                }

                var value = series.Values[dataIndex];
                if (!value.HasValue)
                {
                    continue;
                }

                tags.Add(new LatestValueTag(
                    value.Value,
                    BuildSeriesValueText(series, dataIndex),
                    ToSkColor(series.Stroke),
                    false));
            }
        }

        return tags;
    }

    private static string BuildSeriesValueText(TradingIndicatorSeries series, int dataIndex)
    {
        var value = dataIndex >= 0 && dataIndex < series.Values.Count ? series.Values[dataIndex] : null;
        return value.HasValue ? $"{series.Name}: {FormatSeriesValue(value.Value)}" : $"{series.Name}: --";
    }

    private static string FormatSeriesValue(double value)
    {
        return Math.Abs(value) switch
        {
            >= 1000d => value.ToString("F0"),
            >= 100d => value.ToString("F1"),
            >= 1d => value.ToString("F2"),
            _ => value.ToString("F3")
        };
    }

    private float MeasureLegendSegments(IReadOnlyList<LegendSegment> segments)
    {
        var width = 0f;
        for (var i = 0; i < segments.Count; i++)
        {
            width += _axisTextPaint.MeasureText(segments[i].Text);
            if (i < segments.Count - 1)
            {
                width += 8f;
            }
        }

        return width;
    }

    private CrosshairTarget? ResolveCrosshairTarget(
        TradingChartLayout layout,
        TradingValueRange mainRange,
        TradingChartRenderModel model,
        int visibleStart,
        int visibleEnd)
    {
        var panelIndex = Math.Clamp(model.ActivePanelIndex, 0, layout.Panels.Count - 1);
        if (panelIndex == 0)
        {
            if (model.CrosshairValueMode == CrosshairValueMode.FollowPointer)
            {
                var y = (float)Math.Clamp(model.PointerPosition.Y, layout.Panels[0].BodyBounds.Top, layout.Panels[0].BodyBounds.Bottom);
                return new CrosshairTarget(layout.Panels[0], y, GetValueAtY(layout.Panels[0].BodyBounds, mainRange, y));
            }

            var value = model.Data[model.CrosshairIndex].Close;
            return new CrosshairTarget(layout.Panels[0], GetY(layout.Panels[0].BodyBounds, mainRange, value), value);
        }

        if (panelIndex - 1 >= model.VisibleSubIndicators.Count)
        {
            return null;
        }

        var snapshot = model.VisibleSubIndicators[panelIndex - 1];
        var panel = layout.Panels[panelIndex];
        var range = CalculateIndicatorRange(snapshot, visibleStart, visibleEnd, 6);
        if (model.CrosshairValueMode == CrosshairValueMode.FollowPointer)
        {
            var y = (float)Math.Clamp(model.PointerPosition.Y, panel.BodyBounds.Top, panel.BodyBounds.Bottom);
            return new CrosshairTarget(panel, y, GetValueAtY(panel.BodyBounds, range, y));
        }

        var bestDistance = double.MaxValue;
        double? bestValue = null;
        float bestY = 0f;

        for (var i = 0; i < snapshot.Result.Series.Count; i++)
        {
            var series = snapshot.Result.Series[i];
            if (model.CrosshairIndex >= series.Values.Count)
            {
                continue;
            }

            var value = series.Values[model.CrosshairIndex];
            if (!value.HasValue)
            {
                continue;
            }

            var y = GetY(panel.BodyBounds, range, value.Value);
            var distance = Math.Abs(y - (float)model.PointerPosition.Y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestValue = value.Value;
                bestY = y;
            }
        }

        return bestValue.HasValue ? new CrosshairTarget(panel, bestY, bestValue.Value) : null;
    }

    private static TradingValueRange CalculateMainRange(TradingChartRenderModel model, int visibleStart, int visibleEnd, int maxTickCount)
    {
        var min = double.MaxValue;
        var max = double.MinValue;
        for (var index = visibleStart; index <= visibleEnd; index++)
        {
            var point = model.Data[index];
            min = Math.Min(min, point.Low);
            max = Math.Max(max, point.High);
        }

        for (var snapshotIndex = 0; snapshotIndex < model.MainIndicators.Count; snapshotIndex++)
        {
            var snapshot = model.MainIndicators[snapshotIndex];
            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                if (model.HiddenSeries.Contains(new TradingSeriesKey(snapshot.Indicator, series.Name)))
                {
                    continue;
                }

                for (var index = visibleStart; index <= visibleEnd && index < series.Values.Count; index++)
                {
                    var value = series.Values[index];
                    if (value.HasValue)
                    {
                        min = Math.Min(min, value.Value);
                        max = Math.Max(max, value.Value);
                    }
                }
            }
        }

        return TradingValueRange.Create(min, max, maxTickCount);
    }

    private static TradingValueRange CalculateIndicatorRange(TradingIndicatorSnapshot snapshot, int visibleStart, int visibleEnd, int maxTickCount)
    {
        var min = double.MaxValue;
        var max = double.MinValue;
        var includeZero = false;

        for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
        {
            var series = snapshot.Result.Series[seriesIndex];
            if (series.RenderStyle == IndicatorRenderStyle.Histogram)
            {
                includeZero = true;
            }

            for (var index = visibleStart; index <= visibleEnd && index < series.Values.Count; index++)
            {
                var value = series.Values[index];
                if (value.HasValue)
                {
                    min = Math.Min(min, value.Value);
                    max = Math.Max(max, value.Value);
                }
            }
        }

        if (includeZero)
        {
            min = Math.Min(min, 0d);
            max = Math.Max(max, 0d);
        }

        return TradingValueRange.Create(min, max, maxTickCount);
    }

    private static string FormatValue(double value)
    {
        return Math.Abs(value) switch
        {
            >= 1000d => value.ToString("F0"),
            >= 100d => value.ToString("F1"),
            _ => value.ToString("F2")
        };
    }

    private static string FormatAxisValue(double value, double step)
    {
        var absStep = Math.Abs(step);
        var decimals = absStep >= 10d
            ? 0
            : absStep >= 1d
                ? 1
                : absStep >= 0.1d
                    ? 2
                    : absStep >= 0.01d
                        ? 3
                        : 4;
        return value.ToString($"F{decimals}");
    }

    private static string FormatVolume(double value)
    {
        var abs = Math.Abs(value);
        return abs switch
        {
            >= 1_000_000_000d => $"{value / 1_000_000_000d:F2}B",
            >= 1_000_000d => $"{value / 1_000_000d:F2}M",
            >= 1_000d => $"{value / 1_000d:F2}K",
            _ => value.ToString("F0")
        };
    }

    private static double ResolveBaseline(IReadOnlyList<CandlePoint> data, int index)
    {
        if (data[index].PreviousClose.HasValue)
        {
            return data[index].PreviousClose ?? data[index].Open;
        }

        return index > 0 ? data[index - 1].Close : data[index].Open;
    }

    private static SKColor ResolveCandleColor(TradingChartRenderModel model, CandlePoint point)
    {
        var isRising = point.Close >= point.Open;
        if (point.IsLimitUp && isRising)
        {
            return model.LimitUpColor;
        }

        if (point.IsLimitDown && !isRising)
        {
            return model.LimitDownColor;
        }

        return isRising ? model.UpColor : model.DownColor;
    }

    private CrosshairTarget? ResolveMarkerAnchor(
        TradingChartLayout layout,
        TradingValueRange mainRange,
        TradingChartRenderModel model,
        TradingMarker marker,
        int dataIndex,
        int visibleStart,
        int visibleEnd)
    {
        if (string.IsNullOrWhiteSpace(marker.IndicatorId))
        {
            var panel = layout.Panels[0];
            var candle = model.Data[dataIndex];
            var value = marker.Placement == TradingMarkerPlacement.Above ? candle.High : candle.Low;
            return new CrosshairTarget(panel, GetY(panel.BodyBounds, mainRange, value), value);
        }

        for (var panelIndex = 0; panelIndex < model.VisibleSubIndicators.Count; panelIndex++)
        {
            var snapshot = model.VisibleSubIndicators[panelIndex];
            if (!string.Equals(snapshot.Indicator.Id, marker.IndicatorId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var panel = layout.Panels[panelIndex + 1];
            var range = CalculateIndicatorRange(snapshot, visibleStart, visibleEnd, 6);
            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                if (!string.Equals(series.Name, marker.SeriesName, StringComparison.OrdinalIgnoreCase) || dataIndex >= series.Values.Count)
                {
                    continue;
                }

                var value = series.Values[dataIndex];
                if (!value.HasValue)
                {
                    return null;
                }

                return new CrosshairTarget(panel, GetY(panel.BodyBounds, range, value.Value), value.Value);
            }
        }

        for (var snapshotIndex = 0; snapshotIndex < model.MainIndicators.Count; snapshotIndex++)
        {
            var snapshot = model.MainIndicators[snapshotIndex];
            if (!string.Equals(snapshot.Indicator.Id, marker.IndicatorId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            for (var seriesIndex = 0; seriesIndex < snapshot.Result.Series.Count; seriesIndex++)
            {
                var series = snapshot.Result.Series[seriesIndex];
                if (!string.Equals(series.Name, marker.SeriesName, StringComparison.OrdinalIgnoreCase) || dataIndex >= series.Values.Count)
                {
                    continue;
                }

                var value = series.Values[dataIndex];
                if (!value.HasValue)
                {
                    return null;
                }

                return new CrosshairTarget(layout.Panels[0], GetY(layout.Panels[0].BodyBounds, mainRange, value.Value), value.Value);
            }
        }

        return null;
    }

    private static int FindIndexByTime(IReadOnlyList<CandlePoint> data, DateTimeOffset time, int visibleStart, int visibleEnd)
    {
        for (var i = visibleStart; i <= visibleEnd; i++)
        {
            if (data[i].Time == time)
            {
                return i;
            }
        }

        return -1;
    }

    private void DrawMarkerShape(SKCanvas canvas, TradingMarkerShape shape, float x, float y, float size)
    {
        _markerPath.Rewind();
        switch (shape)
        {
            case TradingMarkerShape.UpArrow:
                _markerPath.MoveTo(x, y - size);
                _markerPath.LineTo(x - size, y + size * 0.4f);
                _markerPath.LineTo(x - size * 0.35f, y + size * 0.4f);
                _markerPath.LineTo(x - size * 0.35f, y + size);
                _markerPath.LineTo(x + size * 0.35f, y + size);
                _markerPath.LineTo(x + size * 0.35f, y + size * 0.4f);
                _markerPath.LineTo(x + size, y + size * 0.4f);
                _markerPath.Close();
                canvas.DrawPath(_markerPath, _markerPaint);
                break;
            case TradingMarkerShape.DownArrow:
                _markerPath.MoveTo(x, y + size);
                _markerPath.LineTo(x - size, y - size * 0.4f);
                _markerPath.LineTo(x - size * 0.35f, y - size * 0.4f);
                _markerPath.LineTo(x - size * 0.35f, y - size);
                _markerPath.LineTo(x + size * 0.35f, y - size);
                _markerPath.LineTo(x + size * 0.35f, y - size * 0.4f);
                _markerPath.LineTo(x + size, y - size * 0.4f);
                _markerPath.Close();
                canvas.DrawPath(_markerPath, _markerPaint);
                break;
            case TradingMarkerShape.Triangle:
                _markerPath.MoveTo(x, y - size);
                _markerPath.LineTo(x - size, y + size);
                _markerPath.LineTo(x + size, y + size);
                _markerPath.Close();
                canvas.DrawPath(_markerPath, _markerPaint);
                break;
            case TradingMarkerShape.Star:
                for (var i = 0; i < 10; i++)
                {
                    var radius = i % 2 == 0 ? size : size * 0.45f;
                    var angle = (Math.PI / 5d * i) - (Math.PI / 2d);
                    var px = x + (float)(Math.Cos(angle) * radius);
                    var py = y + (float)(Math.Sin(angle) * radius);
                    if (i == 0)
                    {
                        _markerPath.MoveTo(px, py);
                    }
                    else
                    {
                        _markerPath.LineTo(px, py);
                    }
                }
                _markerPath.Close();
                canvas.DrawPath(_markerPath, _markerPaint);
                break;
            default:
                canvas.DrawCircle(x, y, size, _markerPaint);
                break;
        }
    }

    private static SKColor ToSkColor(Avalonia.Media.Color color) => new(color.R, color.G, color.B, color.A);

    private static float GetX(Rect bodyBounds, int visibleStart, int visibleCount, int index)
    {
        var slotWidth = bodyBounds.Width / Math.Max(1, visibleCount);
        return (float)(bodyBounds.X + ((index - visibleStart + 0.5d) * slotWidth));
    }

    private static float GetY(Rect bodyBounds, TradingValueRange range, double value)
    {
        var normalized = (value - range.Min) / range.Span;
        return (float)(bodyBounds.Bottom - (normalized * bodyBounds.Height));
    }

    private static double GetValueAtY(Rect bodyBounds, TradingValueRange range, float y)
    {
        var normalized = (bodyBounds.Bottom - y) / bodyBounds.Height;
        return range.Min + (normalized * range.Span);
    }

    private static bool IsSeriesHovered(TradingChartRenderModel model, ITradingIndicator indicator, string seriesName)
    {
        return ReferenceEquals(model.HoveredIndicator, indicator) &&
            string.Equals(model.HoveredSeriesName, seriesName, StringComparison.Ordinal);
    }

    private static float DistanceToSegment(float px, float py, float x1, float y1, float x2, float y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        if (Math.Abs(dx) < float.Epsilon && Math.Abs(dy) < float.Epsilon)
        {
            return MathF.Sqrt(((px - x1) * (px - x1)) + ((py - y1) * (py - y1)));
        }

        var t = (((px - x1) * dx) + ((py - y1) * dy)) / ((dx * dx) + (dy * dy));
        t = Math.Clamp(t, 0f, 1f);
        var cx = x1 + (t * dx);
        var cy = y1 + (t * dy);
        return MathF.Sqrt(((px - cx) * (px - cx)) + ((py - cy) * (py - cy)));
    }

    private static bool HitTestSeriesLine(Point point, Rect bodyBounds, TradingValueRange range, TradingIndicatorSeries series, int visibleStart, int visibleEnd, float tolerance)
    {
        if (!bodyBounds.Contains(point))
        {
            return false;
        }

        SKPoint? previous = null;
        for (var index = visibleStart; index <= visibleEnd && index < series.Values.Count; index++)
        {
            var value = series.Values[index];
            if (!value.HasValue)
            {
                previous = null;
                continue;
            }

            var current = new SKPoint(
                GetX(bodyBounds, visibleStart, visibleEnd - visibleStart + 1, index),
                GetY(bodyBounds, range, value.Value));

            if (previous.HasValue &&
                DistanceToSegment((float)point.X, (float)point.Y, previous.Value.X, previous.Value.Y, current.X, current.Y) <= tolerance)
            {
                return true;
            }

            previous = current;
        }

        return false;
    }

    private static readonly SKColor InactiveColor = new(148, 163, 184, 220);

    private readonly record struct LegendSegment(string Text, SKColor Color, bool IsBold);

    private readonly record struct LegendItem(
        int PanelIndex,
        ITradingIndicator Indicator,
        string? SeriesName,
        bool IsSubPanelToggle,
        bool IsHidden,
        SKRect Bounds,
        IReadOnlyList<LegendSegment> Segments);

    private readonly record struct CrosshairTarget(TradingChartPanelLayout Panel, float Y, double Value);

    private readonly record struct LatestValueTag(double Value, string Text, SKColor Color, bool IsHovered);

    private readonly record struct LatestValueTagPlacement(LatestValueTag Tag, float Y);

    private readonly record struct TradingValueRange(double Min, double Max, double Step, IReadOnlyList<double> Ticks)
    {
        public double Span => Max - Min;

        public static TradingValueRange Create(double min, double max, int maxTickCount)
        {
            if (!double.IsFinite(min) || !double.IsFinite(max))
            {
                return new TradingValueRange(0d, 1d, 1d, [0d, 1d]);
            }

            if (Math.Abs(max - min) < 0.0000001d)
            {
                var padding = Math.Abs(max) * 0.02d;
                padding = padding <= 0d ? 1d : padding;
                min -= padding;
                max += padding;
            }

            var step = SelectNiceStep(min, max, maxTickCount);
            var niceMin = Math.Floor(min / step) * step;
            var niceMax = Math.Ceiling(max / step) * step;
            var ticks = new List<double>();
            for (var value = niceMin; value <= niceMax + (step * 0.5d); value += step)
            {
                ticks.Add(Math.Round(value, 8));
            }

            return new TradingValueRange(niceMin, niceMax, step, ticks);
        }

        private static double SelectNiceStep(double min, double max, int maxTickCount)
        {
            var range = Math.Max(0.0000001d, max - min);
            var safeTickCount = Math.Max(2, maxTickCount);
            var rawStep = range / Math.Max(1d, safeTickCount - 1d);
            var candidates = BuildStepCandidates(rawStep);
            var bestStep = candidates[0];
            var bestScore = double.MaxValue;

            for (var i = 0; i < candidates.Count; i++)
            {
                var step = candidates[i];
                if (step <= 0d)
                {
                    continue;
                }

                var niceMin = Math.Floor(min / step) * step;
                var niceMax = Math.Ceiling(max / step) * step;
                var tickCount = (int)Math.Round((niceMax - niceMin) / step) + 1;
                var rangePenalty = tickCount <= safeTickCount ? 0d : 200d;
                var score = rangePenalty + (Math.Abs(tickCount - Math.Min(safeTickCount, 6)) * 10d) + Math.Abs(step - rawStep);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestStep = step;
                }
            }

            return bestStep;
        }

        private static List<double> BuildStepCandidates(double rawStep)
        {
            var exponent = (int)Math.Floor(Math.Log10(rawStep));
            var factors = new[] { 1d, 2d, 5d, 10d };
            var candidates = new HashSet<double>();
            for (var exp = exponent - 1; exp <= exponent + 1; exp++)
            {
                var scale = Math.Pow(10d, exp);
                for (var i = 0; i < factors.Length; i++)
                {
                    candidates.Add(factors[i] * scale);
                }
            }

            var values = new List<double>(candidates);
            values.Sort();
            return values;
        }
    }
}
