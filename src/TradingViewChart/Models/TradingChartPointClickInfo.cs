namespace TradingViewChart.Models;

public sealed class TradingChartPointClickInfo
{
    public required int Index { get; init; }

    public required DateTimeOffset Time { get; init; }

    public required CandlePoint Candle { get; init; }

    public object? SourceItem { get; init; }
}
