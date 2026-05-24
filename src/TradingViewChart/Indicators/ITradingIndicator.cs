using TradingViewChart.Models;

namespace TradingViewChart.Indicators;

public interface ITradingIndicator
{
    string Id { get; }

    string DisplayName { get; }

    TradingIndicatorPane Pane { get; }

    TradingIndicatorResult Calculate(IReadOnlyList<CandlePoint> data);

    string GetLegendText(int dataIndex, TradingIndicatorResult result);
}
