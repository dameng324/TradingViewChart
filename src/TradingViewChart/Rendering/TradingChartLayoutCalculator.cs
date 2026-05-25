using Avalonia;

namespace TradingViewChart.Rendering;

internal static class TradingChartLayoutCalculator
{
    private const double OuterPadding = 8d;
    private const double YAxisWidth = 72d;
    private const double XAxisHeight = 24d;
    private const double SplitterHeight = 6d;
    private const double HeaderHeight = 22d;
    private const double MinPanelHeight = 40d;

    public static TradingChartLayout Calculate(Size size, IReadOnlyList<double> panelWeights)
    {
        var width = Math.Max(size.Width, 200d);
        var height = Math.Max(size.Height, 160d);
        var plotWidth = Math.Max(32d, width - (OuterPadding * 2d) - YAxisWidth);
        var panelAreaHeight = Math.Max(48d, height - (OuterPadding * 2d) - XAxisHeight);
        var normalizedWeights = NormalizeWeights(panelWeights);
        var panelCount = normalizedWeights.Count;
        var splitterCount = panelCount > 1 ? 1 : 0;
        var availablePanelHeight = Math.Max(
            MinPanelHeight,
            panelAreaHeight - (splitterCount * SplitterHeight)
        );
        var heights = CalculatePanelHeights(normalizedWeights, availablePanelHeight);

        var panels = new List<TradingChartPanelLayout>(panelCount);
        var splitters = new List<TradingChartSplitterLayout>(splitterCount);
        var currentTop = OuterPadding;

        for (var panelIndex = 0; panelIndex < panelCount; panelIndex++)
        {
            var panelHeight = heights[panelIndex];
            var bounds = new Rect(OuterPadding, currentTop, plotWidth, panelHeight);
            var bodyTop = bounds.Y + HeaderHeight;
            var bodyHeight = Math.Max(8d, bounds.Height - HeaderHeight);
            panels.Add(
                new TradingChartPanelLayout(
                    panelIndex,
                    bounds,
                    new Rect(bounds.X, bodyTop, bounds.Width, bodyHeight),
                    new Rect(bounds.Right, bounds.Y, YAxisWidth, bounds.Height)
                )
            );

            currentTop += panelHeight;
            if (panelIndex == 0 && splitterCount > 0)
            {
                var splitterBounds = new Rect(
                    OuterPadding,
                    currentTop,
                    plotWidth + YAxisWidth,
                    SplitterHeight
                );
                splitters.Add(new TradingChartSplitterLayout(0, splitterBounds, 0, 1));
                currentTop += SplitterHeight;
            }
        }

        var xAxisBounds = new Rect(
            OuterPadding,
            OuterPadding + panelAreaHeight,
            plotWidth,
            XAxisHeight
        );
        return new TradingChartLayout(
            new Rect(0d, 0d, width, height),
            panels,
            splitters,
            xAxisBounds,
            YAxisWidth,
            HeaderHeight,
            OuterPadding,
            SplitterHeight
        );
    }

    private static IReadOnlyList<double> NormalizeWeights(IReadOnlyList<double> panelWeights)
    {
        if (panelWeights.Count == 0)
        {
            return [1d];
        }

        var normalized = new double[panelWeights.Count];
        for (var i = 0; i < panelWeights.Count; i++)
        {
            normalized[i] = panelWeights[i] > 0d ? panelWeights[i] : 1d;
        }

        return normalized;
    }

    private static double[] CalculatePanelHeights(
        IReadOnlyList<double> weights,
        double availablePanelHeight
    )
    {
        if (weights.Count == 1)
        {
            return [availablePanelHeight];
        }

        var heights = new double[weights.Count];
        var totalWeight = 0d;
        for (var i = 0; i < weights.Count; i++)
        {
            totalWeight += weights[i];
        }

        for (var i = 0; i < weights.Count; i++)
        {
            heights[i] = availablePanelHeight * (weights[i] / totalWeight);
        }

        var subCount = weights.Count - 1;
        var maxMainHeight = Math.Max(
            MinPanelHeight,
            availablePanelHeight - (subCount * MinPanelHeight)
        );
        var minMainHeight = Math.Min(availablePanelHeight * 0.5d, maxMainHeight);
        heights[0] = Math.Clamp(heights[0], minMainHeight, maxMainHeight);

        var remainingHeight = Math.Max(
            availablePanelHeight - heights[0],
            MinPanelHeight * subCount
        );
        var equalHeight = remainingHeight / subCount;
        for (var i = 1; i < heights.Length; i++)
        {
            heights[i] = equalHeight;
        }

        var minSubHeight = Math.Min(MinPanelHeight, remainingHeight / subCount);
        var deficit = 0d;
        for (var i = 1; i < heights.Length; i++)
        {
            if (heights[i] < minSubHeight)
            {
                deficit += minSubHeight - heights[i];
                heights[i] = minSubHeight;
            }
        }

        if (deficit > 0d)
        {
            for (var i = 1; i < heights.Length && deficit > 0d; i++)
            {
                var spare = heights[i] - minSubHeight;
                if (spare <= 0d)
                {
                    continue;
                }

                var reduction = Math.Min(spare, deficit);
                heights[i] -= reduction;
                deficit -= reduction;
            }
        }

        var actualSum = 0d;
        for (var i = 0; i < heights.Length; i++)
        {
            actualSum += heights[i];
        }

        heights[0] += availablePanelHeight - actualSum;
        return heights;
    }
}

internal sealed class TradingChartLayout
{
    public TradingChartLayout(
        Rect controlBounds,
        IReadOnlyList<TradingChartPanelLayout> panels,
        IReadOnlyList<TradingChartSplitterLayout> splitters,
        Rect xAxisBounds,
        double yAxisWidth,
        double headerHeight,
        double outerPadding,
        double splitterHeight
    )
    {
        ControlBounds = controlBounds;
        Panels = panels;
        Splitters = splitters;
        XAxisBounds = xAxisBounds;
        YAxisWidth = yAxisWidth;
        HeaderHeight = headerHeight;
        OuterPadding = outerPadding;
        SplitterHeight = splitterHeight;
    }

    public Rect ControlBounds { get; }

    public IReadOnlyList<TradingChartPanelLayout> Panels { get; }

    public IReadOnlyList<TradingChartSplitterLayout> Splitters { get; }

    public Rect XAxisBounds { get; }

    public double YAxisWidth { get; }

    public double HeaderHeight { get; }

    public double OuterPadding { get; }

    public double SplitterHeight { get; }
}

internal readonly record struct TradingChartPanelLayout(
    int Index,
    Rect Bounds,
    Rect BodyBounds,
    Rect AxisBounds
);

internal readonly record struct TradingChartSplitterLayout(
    int Index,
    Rect Bounds,
    int UpperPanelIndex,
    int LowerPanelIndex
);
