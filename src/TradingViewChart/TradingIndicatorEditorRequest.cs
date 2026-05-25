using TradingViewChart.Indicators;

namespace TradingViewChart;

public sealed class TradingIndicatorEditorRequest
{
    public required string Title { get; init; }

    public required bool IsNewItem { get; init; }

    public required TradingIndicatorItem Item { get; init; }
}
