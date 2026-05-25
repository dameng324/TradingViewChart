using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace TradingViewChart.Rendering;

internal readonly struct TradingChartDrawOperation : ICustomDrawOperation
{
    private readonly TradingChartRenderer _renderer;
    private readonly TradingChartRenderModel _model;

    public TradingChartDrawOperation(TradingChartRenderer renderer, TradingChartRenderModel model)
    {
        _renderer = renderer;
        _model = model;
    }

    public Rect Bounds => _model.Bounds;

    public void Dispose() { }

    public bool HitTest(Point p) => Bounds.Contains(p);

    public void Render(ImmediateDrawingContext context)
    {
        if (
            context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature))
            is not ISkiaSharpApiLeaseFeature leaseFeature
        )
        {
            return;
        }

        using var lease = leaseFeature.Lease();
        _renderer.Render(lease.SkCanvas, _model);
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return false;
    }
}
