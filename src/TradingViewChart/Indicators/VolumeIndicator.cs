using Avalonia.Media;
using TradingViewChart.Models;

namespace TradingViewChart.Indicators;

public sealed class VolumeIndicator : ITradingIndicator
{
    public string Id => "VOLUME";

    public string DisplayName => "VOLUME";

    public TradingIndicatorPane Pane => TradingIndicatorPane.Sub;

    public TradingIndicatorResult Calculate(IReadOnlyList<CandlePoint> data)
    {
        var values = new double?[data.Count];
        for (var i = 0; i < data.Count; i++)
        {
            values[i] = data[i].Volume;
        }

        return new TradingIndicatorResult(
        [
            new TradingIndicatorSeries(
                "VOL",
                values,
                Color.Parse("#94A3B8"),
                IndicatorRenderStyle.Histogram,
                4d,
                usePriceDirectionColors: true)
        ]);
    }

    public string GetLegendText(int dataIndex, TradingIndicatorResult result)
    {
        if (result.Series.Count == 0)
        {
            return "VOL: --";
        }

        var series = result.Series[0];
        var value = dataIndex >= 0 && dataIndex < series.Values.Count ? series.Values[dataIndex] : null;
        return value.HasValue ? $"VOL: {FormatVolume(value.Value)}" : "VOL: --";
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
}
