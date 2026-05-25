using Avalonia.Media;
using TradingViewChart.Indicators;
using TradingViewChart.Models;

namespace TradingViewChart.Demo.Indicators;

internal sealed class MomentumIndicator : ITradingIndicator
{
    private readonly int _period;

    public MomentumIndicator(int period)
    {
        _period = Math.Max(1, period);
        Id = $"MOM-{_period}";
        DisplayName = $"MOM({_period})";
    }

    public string Id { get; }

    public string DisplayName { get; }

    public TradingIndicatorPane Pane => TradingIndicatorPane.Sub;

    public TradingIndicatorResult Calculate(IReadOnlyList<CandlePoint> data)
    {
        var values = new double?[data.Count];
        var zeroLine = new double?[data.Count];
        for (var i = 0; i < data.Count; i++)
        {
            zeroLine[i] = 0d;
            if (i < _period)
            {
                continue;
            }

            values[i] = data[i].Close - data[i - _period].Close;
        }

        return new TradingIndicatorResult([
            new TradingIndicatorSeries("MOM", values, Color.Parse("#F59E0B")),
            new TradingIndicatorSeries("Zero", zeroLine, Color.Parse("#94A3B8"), thickness: 1d),
        ]);
    }

    public string GetLegendText(int dataIndex, TradingIndicatorResult result)
    {
        var value =
            dataIndex >= 0 && dataIndex < result.Series[0].Values.Count
                ? result.Series[0].Values[dataIndex]
                : null;
        return value.HasValue ? $"{DisplayName}: {value.Value:F2}" : $"{DisplayName}: --";
    }
}
