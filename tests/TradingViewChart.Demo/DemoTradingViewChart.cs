using System.Diagnostics;
using Avalonia.Media;

namespace TradingViewChart.Demo;

public sealed class DemoTradingViewChart : global::TradingViewChart.TradingViewChart
{
    public event EventHandler<RenderEventArgs>? FrameRendered;

    public override void Render(DrawingContext context)
    {
        var beginAllocated = GC.GetAllocatedBytesForCurrentThread();
        var beginTimeStamp = Stopwatch.GetTimestamp();
        base.Render(context);
        FrameRendered?.Invoke(this, new RenderEventArgs
        {
            FrameTime = Stopwatch.GetElapsedTime(beginTimeStamp),
            AllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beginAllocated
        });
    }
}

public struct RenderEventArgs
{
    public required TimeSpan FrameTime { get; init; }
    public long AllocatedBytes { get; init; }
}
/*
 * 1. 添加一个可绑定的Command[Parameter],当用户点击图表上的某个数据点时，触发该Command。
 * 2. 优化指标系统：图表可以添加一系列的指标类型，以表示图表支持的指标，用户可以通过某种可交互的方式添加这些指标，比如右上角有个指标按钮，点击后出现下拉列表选择指标。
 *    添加指标的时候可以输入该指标相关的参数，并配有默认参数（比如MACD的参数，以及MACD的默认参数），指标添加之后，用户可以修改这些指标的参数，比如点击title上这个指标，弹出一个对话框修改参数，并且可以修改是否隐藏。
 * 3. 图表需要可以绑定当前显示区域的时间范围（TwoWay），可以用两个值来分别表示最左边的时间和最右边的时间，用户可以通过绑定来修改这个时间，之前是只能通过鼠标拖拽的方式来调整，注意：这两个数值的设定不可以影响到缩放比例，只可以平移到这个时间点，缩放比例需要保持不变。图表需要添加一个方法，可以让用户直接平移几条数据，正数向右移，负数向左移。
 * 4. 图表需要可以绑定当前的缩放比例（TwoWay），以便修改缩放比例，之前是只能通过鼠标滚轮来实现缩放。
 * 5. demo项目创建一个单独的ViewModel来班定MainWindow的DataContext，ViewModel中需要包含所有TradingViewChart可班定的属性，并在界面中展示，以展示图表的所有支持的功能。
 * 6. 图表的渲染目前会有一些堆上的内存分配，请你使用SharedArrayPool/statckalloc/structs等方式来优化多次渲染时的内存分配，尽量做到每次渲染0堆内存分配。
 * 
 */