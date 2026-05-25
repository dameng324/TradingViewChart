using Avalonia.Media;
using TradingViewChart.Models;

namespace TradingViewChart.Indicators;

public sealed class MacdIndicator : ITradingIndicator
{
    public MacdIndicator(int shortPeriod = 12, int longPeriod = 26, int signalPeriod = 9)
    {
        if (shortPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shortPeriod));
        }

        if (longPeriod <= shortPeriod)
        {
            throw new ArgumentOutOfRangeException(nameof(longPeriod));
        }

        if (signalPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(signalPeriod));
        }

        ShortPeriod = shortPeriod;
        LongPeriod = longPeriod;
        SignalPeriod = signalPeriod;
    }

    public int ShortPeriod { get; }

    public int LongPeriod { get; }

    public int SignalPeriod { get; }

    public string Id => "MACD";

    public string DisplayName => "MACD";

    public TradingIndicatorPane Pane => TradingIndicatorPane.Sub;

    public TradingIndicatorResult Calculate(IReadOnlyList<CandlePoint> data)
    {
        var diffValues = new double?[data.Count];
        var deaValues = new double?[data.Count];
        var histogramValues = new double?[data.Count];

        var shortMultiplier = 2d / (ShortPeriod + 1);
        var longMultiplier = 2d / (LongPeriod + 1);
        var signalMultiplier = 2d / (SignalPeriod + 1);

        double? shortEma = null;
        double? longEma = null;
        double? dea = null;

        for (var index = 0; index < data.Count; index++)
        {
            var close = data[index].Close;
            shortEma = shortEma.HasValue
                ? ((close - shortEma.Value) * shortMultiplier) + shortEma.Value
                : close;
            longEma = longEma.HasValue
                ? ((close - longEma.Value) * longMultiplier) + longEma.Value
                : close;

            var diff = shortEma.Value - longEma.Value;
            dea = dea.HasValue ? ((diff - dea.Value) * signalMultiplier) + dea.Value : diff;
            var histogram = (diff - dea.Value) * 2d;

            diffValues[index] = diff;
            deaValues[index] = dea.Value;
            histogramValues[index] = histogram;
        }

        return new TradingIndicatorResult([
            new TradingIndicatorSeries("DIFF", diffValues, Color.Parse("#F59E0B")),
            new TradingIndicatorSeries("DEA", deaValues, Color.Parse("#60A5FA")),
            new TradingIndicatorSeries(
                "MACD",
                histogramValues,
                Color.Parse("#9CA3AF"),
                IndicatorRenderStyle.Histogram,
                4d
            ),
        ]);
    }

    public string GetLegendText(int dataIndex, TradingIndicatorResult result)
    {
        var parts = new List<string>(result.Series.Count);

        foreach (var series in result.Series)
        {
            var value =
                dataIndex >= 0 && dataIndex < series.Values.Count ? series.Values[dataIndex] : null;
            parts.Add(value.HasValue ? $"{series.Name}: {value.Value:F3}" : $"{series.Name}: --");
        }

        return string.Join("  ", parts);
    }
}
