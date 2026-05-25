using Avalonia.Media;
using TradingViewChart.Models;

namespace TradingViewChart.Indicators;

public sealed class MaIndicator : ITradingIndicator
{
    private static readonly Color[] DefaultPalette =
    [
        Color.Parse("#F59E0B"),
        Color.Parse("#60A5FA"),
        Color.Parse("#A78BFA"),
        Color.Parse("#34D399"),
    ];

    public MaIndicator(params int[] periods)
    {
        Periods = periods is { Length: > 0 } ? periods : [5, 10, 20];
    }

    public IReadOnlyList<int> Periods { get; }

    public string Id => "MA";

    public string DisplayName => "MA";

    public TradingIndicatorPane Pane => TradingIndicatorPane.Main;

    public TradingIndicatorResult Calculate(IReadOnlyList<CandlePoint> data)
    {
        var series = new List<TradingIndicatorSeries>(Periods.Count);

        for (var i = 0; i < Periods.Count; i++)
        {
            var period = Periods[i];
            var values = new double?[data.Count];
            double sum = 0d;

            for (var index = 0; index < data.Count; index++)
            {
                sum += data[index].Close;

                if (index >= period)
                {
                    sum -= data[index - period].Close;
                }

                if (index >= period - 1)
                {
                    values[index] = sum / period;
                }
            }

            series.Add(
                new TradingIndicatorSeries(
                    $"MA{period}",
                    values,
                    DefaultPalette[i % DefaultPalette.Length]
                )
            );
        }

        return new TradingIndicatorResult(series);
    }

    public string GetLegendText(int dataIndex, TradingIndicatorResult result)
    {
        var parts = new List<string>(result.Series.Count);

        foreach (var series in result.Series)
        {
            var value =
                dataIndex >= 0 && dataIndex < series.Values.Count ? series.Values[dataIndex] : null;
            parts.Add(value.HasValue ? $"{series.Name}: {value.Value:F2}" : $"{series.Name}: --");
        }

        return string.Join("  ", parts);
    }
}
