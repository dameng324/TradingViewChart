using Avalonia.Media;

namespace TradingViewChart.Demo;

public sealed class DemoTradingViewChart : global::TradingViewChart.TradingViewChart
{
    public event EventHandler? FrameRendered;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        FrameRendered?.Invoke(this, EventArgs.Empty);
    }
}
