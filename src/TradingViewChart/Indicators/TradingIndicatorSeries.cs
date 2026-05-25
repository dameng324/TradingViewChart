using Avalonia.Media;

namespace TradingViewChart.Indicators;

public sealed class TradingIndicatorSeries
{
    public TradingIndicatorSeries(
        string name,
        IReadOnlyList<double?> values,
        Color stroke,
        IndicatorRenderStyle renderStyle = IndicatorRenderStyle.Line,
        double thickness = 1.5d,
        bool usePriceDirectionColors = false
    )
    {
        Name = name;
        Values = values;
        Stroke = stroke;
        RenderStyle = renderStyle;
        Thickness = thickness;
        UsePriceDirectionColors = usePriceDirectionColors;
    }

    public string Name { get; }

    public IReadOnlyList<double?> Values { get; }

    public Color Stroke { get; }

    public IndicatorRenderStyle RenderStyle { get; }

    public double Thickness { get; }

    public bool UsePriceDirectionColors { get; }
}
