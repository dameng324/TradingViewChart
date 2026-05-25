using TradingViewChart.Indicators;

namespace TradingViewChart;

public interface ITradingChartIndicatorEditor
{
    Task<bool> EditAsync(
        TradingViewChart chart,
        TradingIndicatorEditorRequest request,
        CancellationToken cancellationToken = default
    );
}
