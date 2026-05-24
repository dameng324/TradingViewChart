namespace TradingViewChart.Indicators;

public sealed class TradingIndicatorResult
{
    public TradingIndicatorResult(IReadOnlyList<TradingIndicatorSeries> series)
    {
        Series = series;
    }

    public IReadOnlyList<TradingIndicatorSeries> Series { get; }
}
