using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using TradingViewChart.Indicators;
using TradingViewChart.Models;
using TradingViewChart.Rendering;

namespace TradingViewChart;

public class TradingViewChart : Control
{
    private static readonly object MainPanelKey = new();
    private static readonly Cursor DefaultArrowCursor = new(StandardCursorType.Arrow);
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
    private static readonly Cursor VerticalResizeCursor = new(StandardCursorType.SizeNorthSouth);

    public static readonly StyledProperty<IReadOnlyList<CandlePoint>?> CandleSourceProperty =
        AvaloniaProperty.Register<TradingViewChart, IReadOnlyList<CandlePoint>?>(nameof(CandleSource));

    public static readonly StyledProperty<IReadOnlyList<PricePoint>?> PriceSourceProperty =
        AvaloniaProperty.Register<TradingViewChart, IReadOnlyList<PricePoint>?>(nameof(PriceSource));

    public static readonly StyledProperty<IReadOnlyList<TradingMarker>?> MarkersProperty =
        AvaloniaProperty.Register<TradingViewChart, IReadOnlyList<TradingMarker>?>(nameof(Markers));

    public static readonly StyledProperty<IBrush?> UpBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(UpBrush));

    public static readonly StyledProperty<IBrush?> DownBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(DownBrush));

    public static readonly StyledProperty<IBrush?> LimitUpBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(LimitUpBrush));

    public static readonly StyledProperty<IBrush?> LimitDownBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(LimitDownBrush));

    public static readonly StyledProperty<IBrush?> ChartBackgroundBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(ChartBackgroundBrush));

    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(GridLineBrush));

    public static readonly StyledProperty<IBrush?> AxisTextBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(AxisTextBrush));

    public static readonly StyledProperty<IBrush?> TooltipBackgroundBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(TooltipBackgroundBrush));

    public static readonly StyledProperty<IBrush?> TooltipTextBrushProperty =
        AvaloniaProperty.Register<TradingViewChart, IBrush?>(nameof(TooltipTextBrush));

    public static readonly StyledProperty<CrosshairMode> CrosshairModeProperty =
        AvaloniaProperty.Register<TradingViewChart, CrosshairMode>(nameof(CrosshairMode), CrosshairMode.FixedCorner);

    public static readonly StyledProperty<CrosshairValueMode> CrosshairValueModeProperty =
        AvaloniaProperty.Register<TradingViewChart, CrosshairValueMode>(nameof(CrosshairValueMode), CrosshairValueMode.SnapToData);

    public static readonly StyledProperty<string> XAxisLabelFormatProperty =
        AvaloniaProperty.Register<TradingViewChart, string>(nameof(XAxisLabelFormat), "yyyy-MM-dd");

    public static readonly StyledProperty<DateTimeOffset?> HoveredTimeProperty =
        AvaloniaProperty.Register<TradingViewChart, DateTimeOffset?>(
            nameof(HoveredTime),
            defaultBindingMode: BindingMode.TwoWay);

    private readonly TradingChartRenderer _renderer = new();
    private readonly VolumeIndicator _defaultVolumeIndicator = new();
    private readonly ObservableCollection<ITradingIndicator> _indicators = [];
    private readonly Dictionary<ITradingIndicator, TradingIndicatorResult> _indicatorCache = new();
    private readonly List<TradingIndicatorSnapshot> _mainIndicatorSnapshots = [];
    private readonly List<TradingIndicatorSnapshot> _subIndicatorSnapshots = [];
    private readonly HashSet<ITradingIndicator> _hiddenIndicators = [];
    private readonly HashSet<TradingSeriesKey> _hiddenSeries = [];
    private readonly Dictionary<object, double> _panelWeights = [];
    private readonly List<CandlePoint> _renderData = [];
    private readonly HashSet<INotifyPropertyChanged> _subscribedDataItems = [];
    private INotifyCollectionChanged? _activeSourceNotifier;
    private INotifyCollectionChanged? _markersNotifier;
    private TradingChartLayout? _cachedLayout;
    private Size _cachedLayoutSize;
    private int _cachedVisibleSubCount = -1;
    private bool _indicatorCacheDirty = true;
    private bool _renderDataDirty = true;
    private bool _isPanning;
    private bool _isResizingSplitter;
    private bool _isKeyboardCrosshairControl;
    private bool _hasPhysicalPointerPosition;
    private bool _isSwitchingSource;
    private Point _lastPointerPosition;
    private Point _lastPhysicalPointerPosition;
    private int _visibleStartIndex;
    private int _visibleCount;
    private int _crosshairIndex = -1;
    private int _activePanelIndex;
    private int _panStartVisibleIndex;
    private Point _panStartPosition;
    private double _resizeStartY;
    private double _resizeUpperStartHeight;
    private double _resizeLowerStartHeight;
    private int _activeSplitterIndex = -1;
    private ITradingIndicator? _hoveredIndicator;
    private string? _hoveredSeriesName;
    private TradingChartSeriesMode _seriesMode = TradingChartSeriesMode.Candle;
    private TradingTooltipCorner _tooltipCorner = TradingTooltipCorner.LeftTop;

    static TradingViewChart()
    {
        FocusableProperty.OverrideDefaultValue<TradingViewChart>(true);
        AffectsRender<TradingViewChart>(
            CandleSourceProperty,
            PriceSourceProperty,
            MarkersProperty,
            UpBrushProperty,
            DownBrushProperty,
            LimitUpBrushProperty,
            LimitDownBrushProperty,
            ChartBackgroundBrushProperty,
            GridLineBrushProperty,
            AxisTextBrushProperty,
            TooltipBackgroundBrushProperty,
            TooltipTextBrushProperty,
            CrosshairModeProperty,
            CrosshairValueModeProperty,
            XAxisLabelFormatProperty);

        CandleSourceProperty.Changed.AddClassHandler<TradingViewChart>((chart, _) => chart.OnCandleSourceChanged());
        PriceSourceProperty.Changed.AddClassHandler<TradingViewChart>((chart, _) => chart.OnPriceSourceChanged());
        MarkersProperty.Changed.AddClassHandler<TradingViewChart>((chart, _) => chart.OnMarkersChanged());
    }

    public TradingViewChart()
    {
        _indicators.CollectionChanged += OnIndicatorsChanged;
    }

    public IReadOnlyList<CandlePoint>? CandleSource
    {
        get => GetValue(CandleSourceProperty);
        set => SetValue(CandleSourceProperty, value);
    }

    public IReadOnlyList<PricePoint>? PriceSource
    {
        get => GetValue(PriceSourceProperty);
        set => SetValue(PriceSourceProperty, value);
    }

    public IReadOnlyList<TradingMarker>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    public ObservableCollection<ITradingIndicator> Indicators => _indicators;

    public IBrush? UpBrush
    {
        get => GetValue(UpBrushProperty);
        set => SetValue(UpBrushProperty, value);
    }

    public IBrush? DownBrush
    {
        get => GetValue(DownBrushProperty);
        set => SetValue(DownBrushProperty, value);
    }

    public IBrush? LimitUpBrush
    {
        get => GetValue(LimitUpBrushProperty);
        set => SetValue(LimitUpBrushProperty, value);
    }

    public IBrush? LimitDownBrush
    {
        get => GetValue(LimitDownBrushProperty);
        set => SetValue(LimitDownBrushProperty, value);
    }

    public IBrush? ChartBackgroundBrush
    {
        get => GetValue(ChartBackgroundBrushProperty);
        set => SetValue(ChartBackgroundBrushProperty, value);
    }

    public IBrush? GridLineBrush
    {
        get => GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public IBrush? AxisTextBrush
    {
        get => GetValue(AxisTextBrushProperty);
        set => SetValue(AxisTextBrushProperty, value);
    }

    public IBrush? TooltipBackgroundBrush
    {
        get => GetValue(TooltipBackgroundBrushProperty);
        set => SetValue(TooltipBackgroundBrushProperty, value);
    }

    public IBrush? TooltipTextBrush
    {
        get => GetValue(TooltipTextBrushProperty);
        set => SetValue(TooltipTextBrushProperty, value);
    }

    public CrosshairMode CrosshairMode
    {
        get => GetValue(CrosshairModeProperty);
        set => SetValue(CrosshairModeProperty, value);
    }

    public CrosshairValueMode CrosshairValueMode
    {
        get => GetValue(CrosshairValueModeProperty);
        set => SetValue(CrosshairValueModeProperty, value);
    }

    public string XAxisLabelFormat
    {
        get => GetValue(XAxisLabelFormatProperty);
        set => SetValue(XAxisLabelFormatProperty, value);
    }

    public DateTimeOffset? HoveredTime
    {
        get => GetValue(HoveredTimeProperty);
        set => SetValue(HoveredTimeProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        EnsureRenderData();
        EnsureIndicatorCache();

        if (Bounds.Width <= 0d || Bounds.Height <= 0d)
        {
            return;
        }

        if (_visibleCount <= 0 && _renderData.Count > 0)
        {
            ResetViewport(_renderData.Count);
        }

        context.Custom(new TradingChartDrawOperation(_renderer, CreateRenderModel(_renderData)));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var position = e.GetPosition(this);
        _lastPointerPosition = position;
        _lastPhysicalPointerPosition = position;
        _hasPhysicalPointerPosition = true;
        _isKeyboardCrosshairControl = false;

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            UpdateCrosshair(position);
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var legendTarget = TryGetLegendHitTarget(position);
            if (legendTarget.HasValue)
            {
                ToggleLegendTarget(legendTarget.Value);
                e.Handled = true;
                return;
            }

            var layout = GetCurrentLayout();
            var splitter = HitTestSplitter(layout, position);
            if (splitter.HasValue)
            {
                BeginSplitterResize(splitter.Value, position);
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }

            _isPanning = true;
            _panStartPosition = position;
            _panStartVisibleIndex = _visibleStartIndex;
            Cursor = HandCursor;
            e.Pointer.Capture(this);
            UpdateCrosshair(position);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
        _isResizingSplitter = false;
        _activeSplitterIndex = -1;
        e.Pointer.Capture(null);
        UpdatePointerFeedback(e.GetPosition(this));
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);
        var pointerActuallyMoved = !_hasPhysicalPointerPosition || HasPointerMoved(position, _lastPhysicalPointerPosition);
        _lastPhysicalPointerPosition = position;
        _hasPhysicalPointerPosition = true;

        if (pointerActuallyMoved)
        {
            _lastPointerPosition = position;
            _isKeyboardCrosshairControl = false;
        }

        if (_isResizingSplitter)
        {
            ResizeSplitter(position);
        }
        else if (_isKeyboardCrosshairControl && !pointerActuallyMoved)
        {
            e.Handled = true;
            return;
        }
        else
        {
            UpdatePointerFeedback(position);

            if (_isPanning)
            {
                PanTo(position);
            }
        }

        UpdateCrosshair(position);
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (!IsFocused)
        {
            _crosshairIndex = -1;
            _activePanelIndex = 0;
            HoveredTime = null;
            _isKeyboardCrosshairControl = false;
            _hoveredIndicator = null;
            _hoveredSeriesName = null;
            Cursor = DefaultArrowCursor;
            InvalidateVisual();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var position = e.GetPosition(this);
        _lastPointerPosition = position;
        _lastPhysicalPointerPosition = position;
        _hasPhysicalPointerPosition = true;
        _isKeyboardCrosshairControl = false;
        Zoom(position, e.Delta.Y);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_renderData.Count == 0)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                _isKeyboardCrosshairControl = true;
                MoveCrosshair(-1);
                e.Handled = true;
                break;
            case Key.Right:
                _isKeyboardCrosshairControl = true;
                MoveCrosshair(1);
                e.Handled = true;
                break;
        }
    }

    private void OnCandleSourceChanged()
    {
        if (_isSwitchingSource)
        {
            return;
        }

        if (CandleSource is not null && PriceSource is not null)
        {
            try
            {
                _isSwitchingSource = true;
                SetCurrentValue(PriceSourceProperty, null);
            }
            finally
            {
                _isSwitchingSource = false;
            }
        }

        RefreshSourceBinding(resetViewport: true);
    }

    private void OnPriceSourceChanged()
    {
        if (_isSwitchingSource)
        {
            return;
        }

        if (PriceSource is not null && CandleSource is not null)
        {
            try
            {
                _isSwitchingSource = true;
                SetCurrentValue(CandleSourceProperty, null);
            }
            finally
            {
                _isSwitchingSource = false;
            }
        }

        RefreshSourceBinding(resetViewport: true);
    }

    private void OnMarkersChanged()
    {
        if (_markersNotifier is not null)
        {
            _markersNotifier.CollectionChanged -= OnMarkersCollectionChanged;
            _markersNotifier = null;
        }

        if (Markers is INotifyCollectionChanged notifier)
        {
            _markersNotifier = notifier;
            _markersNotifier.CollectionChanged += OnMarkersCollectionChanged;
        }

        InvalidateVisual();
    }

    private void OnMarkersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnActiveSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSourceItemSubscriptions(e);
        _renderDataDirty = true;
        _indicatorCacheDirty = true;

        var nextCount = GetActiveSourceCount();
        if (e.Action == NotifyCollectionChangedAction.Add && nextCount > 0)
        {
            var wasPinnedToRight = _visibleCount <= 0 || _visibleStartIndex + _visibleCount >= nextCount - (e.NewItems?.Count ?? 0);
            if (_visibleCount <= 0)
            {
                ResetViewport(nextCount);
            }
            else
            {
                _visibleCount = Math.Min(Math.Max(1, _visibleCount), nextCount);
                if (wasPinnedToRight)
                {
                    _visibleStartIndex = Math.Max(0, nextCount - _visibleCount);
                }

                _crosshairIndex = Math.Clamp(_crosshairIndex, 0, nextCount - 1);
                SyncHoveredTime();
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Replace)
        {
            _crosshairIndex = nextCount <= 0 ? -1 : Math.Clamp(_crosshairIndex, 0, nextCount - 1);
            _visibleStartIndex = nextCount <= 0 ? 0 : Math.Clamp(_visibleStartIndex, 0, Math.Max(0, nextCount - Math.Max(1, _visibleCount)));
            SyncHoveredTime();
        }
        else
        {
            ResetViewport(nextCount);
        }

        _cachedLayout = null;
        InvalidateVisual();
    }

    private void UpdateSourceItemSubscriptions(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var item in _subscribedDataItems)
            {
                item.PropertyChanged -= OnSourceItemPropertyChanged;
            }

            _subscribedDataItems.Clear();
            foreach (var item in EnumerateActiveSourceItems())
            {
                if (item is INotifyPropertyChanged propertyChanged && _subscribedDataItems.Add(propertyChanged))
                {
                    propertyChanged.PropertyChanged += OnSourceItemPropertyChanged;
                }
            }

            return;
        }

        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is INotifyPropertyChanged propertyChanged && _subscribedDataItems.Remove(propertyChanged))
                {
                    propertyChanged.PropertyChanged -= OnSourceItemPropertyChanged;
                }
            }
        }

        if (e.NewItems is null)
        {
            return;
        }

        foreach (var item in e.NewItems)
        {
            if (item is INotifyPropertyChanged propertyChanged && _subscribedDataItems.Add(propertyChanged))
            {
                propertyChanged.PropertyChanged += OnSourceItemPropertyChanged;
            }
        }
    }

    private void OnSourceItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _renderDataDirty = true;
        _indicatorCacheDirty = true;
        InvalidateVisual();
    }

    private void RefreshSourceBinding(bool resetViewport)
    {
        DetachActiveSource();
        AttachActiveSource();
        _renderDataDirty = true;
        _indicatorCacheDirty = true;
        _cachedLayout = null;

        if (resetViewport)
        {
            ResetViewport(GetActiveSourceCount());
        }
        else
        {
            _crosshairIndex = _renderData.Count <= 0 ? -1 : Math.Clamp(_crosshairIndex, 0, _renderData.Count - 1);
            _visibleStartIndex = _renderData.Count <= 0 ? 0 : Math.Clamp(_visibleStartIndex, 0, Math.Max(0, _renderData.Count - Math.Max(1, _visibleCount)));
            SyncHoveredTime();
        }

        InvalidateVisual();
    }

    private void AttachActiveSource()
    {
        if (GetActiveSourceList() is INotifyCollectionChanged notifier)
        {
            _activeSourceNotifier = notifier;
            _activeSourceNotifier.CollectionChanged += OnActiveSourceCollectionChanged;
        }

        foreach (var item in EnumerateActiveSourceItems())
        {
            if (item is INotifyPropertyChanged propertyChanged && _subscribedDataItems.Add(propertyChanged))
            {
                propertyChanged.PropertyChanged += OnSourceItemPropertyChanged;
            }
        }
    }

    private void DetachActiveSource()
    {
        if (_activeSourceNotifier is not null)
        {
            _activeSourceNotifier.CollectionChanged -= OnActiveSourceCollectionChanged;
            _activeSourceNotifier = null;
        }

        foreach (var item in _subscribedDataItems)
        {
            item.PropertyChanged -= OnSourceItemPropertyChanged;
        }

        _subscribedDataItems.Clear();
    }

    private IEnumerable<object> EnumerateActiveSourceItems()
    {
        if (PriceSource is not null)
        {
            foreach (var item in PriceSource)
            {
                yield return item;
            }

            yield break;
        }

        if (CandleSource is null)
        {
            yield break;
        }

        foreach (var item in CandleSource)
        {
            yield return item;
        }
    }

    private object? GetActiveSourceList()
    {
        return PriceSource is not null ? PriceSource : CandleSource;
    }

    private int GetActiveSourceCount()
    {
        return PriceSource?.Count ?? CandleSource?.Count ?? 0;
    }

    private void OnIndicatorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _hiddenIndicators.RemoveWhere(indicator => !_indicators.Contains(indicator));
        _hiddenSeries.RemoveWhere(series => !_indicators.Contains(series.Indicator));
        var activeIndicators = new HashSet<ITradingIndicator>(_indicators) { _defaultVolumeIndicator };
        var staleKeys = new List<object>();
        foreach (var key in _panelWeights.Keys)
        {
            if (ReferenceEquals(key, MainPanelKey))
            {
                continue;
            }

            if (key is not ITradingIndicator indicator || !activeIndicators.Contains(indicator))
            {
                staleKeys.Add(key);
            }
        }

        for (var i = 0; i < staleKeys.Count; i++)
        {
            _panelWeights.Remove(staleKeys[i]);
        }

        _indicatorCacheDirty = true;
        _cachedLayout = null;
        InvalidateVisual();
    }

    private void ResetViewport(int dataCount)
    {
        if (dataCount <= 0)
        {
            _visibleStartIndex = 0;
            _visibleCount = 0;
            _crosshairIndex = -1;
            _activePanelIndex = 0;
            HoveredTime = null;
            return;
        }

        _visibleCount = Math.Min(120, dataCount);
        _visibleCount = Math.Max(20, _visibleCount);
        _visibleCount = Math.Min(_visibleCount, dataCount);
        _visibleStartIndex = Math.Max(0, dataCount - _visibleCount);
        _crosshairIndex = dataCount - 1;
        _activePanelIndex = 0;
        SyncHoveredTime();
    }

    private void EnsureRenderData()
    {
        if (!_renderDataDirty)
        {
            return;
        }

        _renderData.Clear();
        if (PriceSource is not null)
        {
            _seriesMode = TradingChartSeriesMode.PriceLine;
            for (var i = 0; i < PriceSource.Count; i++)
            {
                var point = PriceSource[i];
                _renderData.Add(new CandlePoint
                {
                    Time = point.Time,
                    Open = point.Price,
                    High = point.Price,
                    Low = point.Price,
                    Close = point.Price,
                    PreviousClose = i > 0 ? PriceSource[i - 1].Price : point.Price
                });
            }
        }
        else
        {
            _seriesMode = TradingChartSeriesMode.Candle;
            if (CandleSource is not null)
            {
                for (var i = 0; i < CandleSource.Count; i++)
                {
                    _renderData.Add(CandleSource[i]);
                }
            }
        }

        _renderDataDirty = false;
    }

    private void EnsureIndicatorCache()
    {
        EnsureRenderData();
        if (!_indicatorCacheDirty)
        {
            return;
        }

        _indicatorCache.Clear();
        _mainIndicatorSnapshots.Clear();
        _subIndicatorSnapshots.Clear();
        var data = (IReadOnlyList<CandlePoint>)_renderData;

        if (_seriesMode == TradingChartSeriesMode.Candle && !ContainsVolumeIndicator())
        {
            AddIndicatorSnapshot(_defaultVolumeIndicator, data);
        }

        foreach (var indicator in _indicators)
        {
            AddIndicatorSnapshot(indicator, data);
        }

        _indicatorCacheDirty = false;
    }

    private TradingChartRenderModel CreateRenderModel(IReadOnlyList<CandlePoint> data)
    {
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        return new TradingChartRenderModel
        {
            Bounds = Bounds,
            Layout = GetCurrentLayout(),
            Data = data,
            SeriesMode = _seriesMode,
            MainIndicators = _mainIndicatorSnapshots,
            SubIndicators = _subIndicatorSnapshots,
            VisibleSubIndicators = GetVisibleSubIndicatorSnapshots(),
            Markers = Markers ?? [],
            HiddenIndicators = _hiddenIndicators,
            HiddenSeries = _hiddenSeries,
            VisibleStartIndex = _visibleStartIndex,
            VisibleCount = _visibleCount == 0 && data.Count > 0 ? data.Count : Math.Max(0, _visibleCount),
            CrosshairIndex = _crosshairIndex >= 0 ? _crosshairIndex : Math.Max(0, data.Count - 1),
            ActivePanelIndex = _activePanelIndex,
            ShowCrosshair = _crosshairIndex >= 0,
            HoveredIndicator = _hoveredIndicator,
            HoveredSeriesName = _hoveredSeriesName,
            PointerPosition = _lastPointerPosition,
            CrosshairMode = CrosshairMode,
            CrosshairValueMode = CrosshairValueMode,
            TooltipCorner = _tooltipCorner,
            XAxisLabelFormat = string.IsNullOrWhiteSpace(XAxisLabelFormat) ? "yyyy-MM-dd" : XAxisLabelFormat,
            BackgroundColor = ChartBackgroundBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(11, 18, 32) : new SkiaSharp.SKColor(248, 250, 252)),
            GridColor = GridLineBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(71, 95, 122, 100) : new SkiaSharp.SKColor(51, 65, 85, 32)),
            TextColor = AxisTextBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(226, 232, 240) : new SkiaSharp.SKColor(15, 23, 42)),
            TooltipBackgroundColor = TooltipBackgroundBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(30, 41, 59, 242) : new SkiaSharp.SKColor(255, 255, 255, 242)),
            TooltipTextColor = TooltipTextBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(248, 250, 252) : new SkiaSharp.SKColor(15, 23, 42)),
            UpColor = UpBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(255, 95, 122) : new SkiaSharp.SKColor(225, 29, 72)),
            DownColor = DownBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(52, 211, 153) : new SkiaSharp.SKColor(22, 163, 74)),
            LimitUpColor = LimitUpBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(255, 45, 85) : new SkiaSharp.SKColor(185, 28, 28)),
            LimitDownColor = LimitDownBrush.ToSkColor(isDark ? new SkiaSharp.SKColor(16, 185, 129) : new SkiaSharp.SKColor(22, 101, 52)),
            MarkedIndices = new HashSet<int>()
        };
    }

    private void UpdateCrosshair(Point position)
    {
        if (_renderData.Count == 0 || Bounds.Width <= 0d || Bounds.Height <= 0d)
        {
            return;
        }

        var layout = GetCurrentLayout();
        TradingChartPanelLayout? targetPanel = null;
        for (var i = 0; i < layout.Panels.Count; i++)
        {
            var panel = layout.Panels[i];
            if (panel.BodyBounds.Contains(position))
            {
                targetPanel = panel;
                break;
            }
        }

        if (targetPanel is null)
        {
            return;
        }

        var plotBounds = targetPanel.Value.BodyBounds;
        var slotWidth = plotBounds.Width / Math.Max(1, _visibleCount);
        var relativeIndex = (int)Math.Floor((position.X - plotBounds.X) / Math.Max(1d, slotWidth));
        var nextCrosshairIndex = Math.Clamp(_visibleStartIndex + relativeIndex, 0, _renderData.Count - 1);
        var previousTooltipCorner = _tooltipCorner;
        UpdateTooltipCorner(layout, nextCrosshairIndex);

        if (nextCrosshairIndex == _crosshairIndex &&
            previousTooltipCorner == _tooltipCorner &&
            _activePanelIndex == targetPanel.Value.Index)
        {
            return;
        }

        _crosshairIndex = nextCrosshairIndex;
        _activePanelIndex = targetPanel.Value.Index;
        SyncHoveredTime();
        InvalidateVisual();
    }

    private void UpdateTooltipCorner(TradingChartLayout layout, int crosshairIndex)
    {
        _tooltipCorner = TradingTooltipCorner.LeftTop;
    }

    private void PanTo(Point position)
    {
        if (_renderData.Count <= _visibleCount || Bounds.Width <= 0d)
        {
            return;
        }

        var layout = GetCurrentLayout();
        var plotBounds = layout.Panels[0].BodyBounds;
        var slotWidth = plotBounds.Width / Math.Max(1, _visibleCount);
        if (slotWidth <= 0d)
        {
            return;
        }

        var deltaBars = (int)Math.Round((position.X - _panStartPosition.X) / slotWidth);
        var nextVisibleStartIndex = Math.Clamp(_panStartVisibleIndex - deltaBars, 0, _renderData.Count - _visibleCount);
        if (nextVisibleStartIndex == _visibleStartIndex)
        {
            return;
        }

        _visibleStartIndex = nextVisibleStartIndex;
        InvalidateVisual();
    }

    private void Zoom(Point pointer, double delta)
    {
        if (_renderData.Count == 0)
        {
            return;
        }

        var oldCount = Math.Max(1, _visibleCount);
        var nextCount = delta > 0d
            ? Math.Max(10, oldCount - Math.Max(2, oldCount / 8))
            : Math.Min(_renderData.Count, oldCount + Math.Max(2, oldCount / 8));

        if (nextCount == oldCount)
        {
            return;
        }

        var layout = GetCurrentLayout();
        var plotBounds = layout.Panels[0].BodyBounds;

        var anchorRelative = Math.Clamp((pointer.X - plotBounds.X) / Math.Max(1d, plotBounds.Width), 0d, 1d);
        var anchorIndex = Math.Clamp(_visibleStartIndex + (int)Math.Round(anchorRelative * (oldCount - 1)), 0, _renderData.Count - 1);
        var proposedStart = anchorIndex - (int)Math.Round(anchorRelative * (nextCount - 1));

        var nextVisibleStartIndex = Math.Clamp(proposedStart, 0, Math.Max(0, _renderData.Count - nextCount));
        var nextCrosshairIndex = Math.Clamp(anchorIndex, 0, _renderData.Count - 1);
        if (nextCount == _visibleCount && nextVisibleStartIndex == _visibleStartIndex && nextCrosshairIndex == _crosshairIndex)
        {
            return;
        }

        _visibleCount = nextCount;
        _visibleStartIndex = nextVisibleStartIndex;
        _crosshairIndex = nextCrosshairIndex;
        SyncHoveredTime();
        InvalidateVisual();
    }

    private void MoveCrosshair(int direction)
    {
        if (_renderData.Count == 0)
        {
            return;
        }

        int nextCrosshairIndex;
        if (_crosshairIndex < 0)
        {
            nextCrosshairIndex = Math.Max(0, _renderData.Count - 1);
        }
        else
        {
            nextCrosshairIndex = Math.Clamp(_crosshairIndex + direction, 0, _renderData.Count - 1);
        }

        var nextVisibleStartIndex = _visibleStartIndex;
        if (nextCrosshairIndex < _visibleStartIndex)
        {
            nextVisibleStartIndex = nextCrosshairIndex;
        }
        else if (nextCrosshairIndex >= _visibleStartIndex + _visibleCount)
        {
            nextVisibleStartIndex = Math.Max(0, nextCrosshairIndex - _visibleCount + 1);
        }

        if (nextCrosshairIndex == _crosshairIndex && nextVisibleStartIndex == _visibleStartIndex)
        {
            return;
        }

        _crosshairIndex = nextCrosshairIndex;
        _visibleStartIndex = nextVisibleStartIndex;
        SyncHoveredTime();
        InvalidateVisual();
    }

    private void UpdatePointerFeedback(Point position)
    {
        EnsureIndicatorCache();
        var layout = GetCurrentLayout();
        var splitter = HitTestSplitter(layout, position);
        var legendTarget = splitter.HasValue || _isPanning ? null : TryGetLegendHitTarget(position);
        var hoveredIndicator = legendTarget?.Indicator;
        var hoveredSeriesName = legendTarget?.SeriesName;
        var cursor = _isPanning
            ? HandCursor
            : splitter.HasValue
            ? VerticalResizeCursor
            : legendTarget.HasValue
                ? HandCursor
                : DefaultArrowCursor;

        if (_hoveredIndicator == hoveredIndicator &&
            _hoveredSeriesName == hoveredSeriesName &&
            Equals(Cursor, cursor))
        {
            return;
        }

        _hoveredIndicator = hoveredIndicator;
        _hoveredSeriesName = hoveredSeriesName;
        Cursor = cursor;
        InvalidateVisual();
    }

    private TradingLegendHitTarget? TryGetLegendHitTarget(Point position)
    {
        EnsureIndicatorCache();
        if (_renderData.Count == 0)
        {
            return null;
        }

        return _renderer.HitTestLegend(CreateRenderModel(_renderData), position);
    }

    private void ToggleLegendTarget(TradingLegendHitTarget target)
    {
        if (target.IsSubPanelToggle)
        {
            if (!_hiddenIndicators.Add(target.Indicator))
            {
                _hiddenIndicators.Remove(target.Indicator);
            }

            _cachedLayout = null;
            NormalizeActivePanelIndex();
        }
        else if (!string.IsNullOrWhiteSpace(target.SeriesName))
        {
            var key = new TradingSeriesKey(target.Indicator, target.SeriesName);
            if (!_hiddenSeries.Add(key))
            {
                _hiddenSeries.Remove(key);
            }
        }

        InvalidateVisual();
    }

    private void SyncHoveredTime()
    {
        if (_crosshairIndex < 0 || _crosshairIndex >= _renderData.Count)
        {
            HoveredTime = null;
            return;
        }

        HoveredTime = _renderData[_crosshairIndex].Time;
    }

    private TradingChartLayout GetCurrentLayout()
    {
        EnsureIndicatorCache();
        var size = Bounds.Size;
        var visiblePanels = GetVisiblePanelEntries();
        if (_cachedLayout is null || _cachedLayoutSize != size || _cachedVisibleSubCount != visiblePanels.Count - 1)
        {
            _cachedLayout = TradingChartLayoutCalculator.Calculate(size, BuildPanelWeights(visiblePanels));
            _cachedLayoutSize = size;
            _cachedVisibleSubCount = visiblePanels.Count - 1;
        }

        return _cachedLayout;
    }

    private bool ContainsVolumeIndicator()
    {
        for (var i = 0; i < _indicators.Count; i++)
        {
            if (_indicators[i].Pane == TradingIndicatorPane.Sub && string.Equals(_indicators[i].Id, _defaultVolumeIndicator.Id, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void AddIndicatorSnapshot(ITradingIndicator indicator, IReadOnlyList<CandlePoint> data)
    {
        var result = indicator.Calculate(data);
        _indicatorCache[indicator] = result;

        var snapshot = new TradingIndicatorSnapshot
        {
            Indicator = indicator,
            Result = result
        };

        if (indicator.Pane == TradingIndicatorPane.Sub)
        {
            _subIndicatorSnapshots.Add(snapshot);
        }
        else
        {
            _mainIndicatorSnapshots.Add(snapshot);
        }
    }

    private List<TradingIndicatorSnapshot> GetVisibleSubIndicatorSnapshots()
    {
        var visible = new List<TradingIndicatorSnapshot>(_subIndicatorSnapshots.Count);
        for (var i = 0; i < _subIndicatorSnapshots.Count; i++)
        {
            if (!_hiddenIndicators.Contains(_subIndicatorSnapshots[i].Indicator))
            {
                visible.Add(_subIndicatorSnapshots[i]);
            }
        }

        return visible;
    }

    private List<PanelEntry> GetVisiblePanelEntries()
    {
        var entries = new List<PanelEntry> { new(MainPanelKey, null) };
        for (var i = 0; i < _subIndicatorSnapshots.Count; i++)
        {
            var snapshot = _subIndicatorSnapshots[i];
            if (_hiddenIndicators.Contains(snapshot.Indicator))
            {
                continue;
            }

            entries.Add(new PanelEntry(snapshot.Indicator, snapshot));
        }

        return entries;
    }

    private IReadOnlyList<double> BuildPanelWeights(IReadOnlyList<PanelEntry> visiblePanels)
    {
        if (visiblePanels.Count == 1)
        {
            _panelWeights[MainPanelKey] = 1d;
            return [1d];
        }

        var weights = new double[visiblePanels.Count];
        var subPanelCount = visiblePanels.Count - 1;
        var subEqualWeight = 0d;
        for (var i = 0; i < visiblePanels.Count; i++)
        {
            var key = visiblePanels[i].Key;
            if (!_panelWeights.TryGetValue(key, out var weight) || weight <= 0d)
            {
                weight = i == 0 ? 7d : 3d / Math.Max(1, subPanelCount);
                _panelWeights[key] = weight;
            }

            if (i == 0)
            {
                weights[i] = weight;
            }
            else
            {
                subEqualWeight += weight;
            }
        }

        if (subPanelCount > 0)
        {
            var averageSubWeight = Math.Max(1d, subEqualWeight / subPanelCount);
            for (var i = 1; i < weights.Length; i++)
            {
                weights[i] = averageSubWeight;
            }
        }

        return weights;
    }

    private TradingChartSplitterLayout? HitTestSplitter(TradingChartLayout layout, Point position)
    {
        for (var i = 0; i < layout.Splitters.Count; i++)
        {
            if (layout.Splitters[i].Bounds.Inflate(2d).Contains(position))
            {
                return layout.Splitters[i];
            }
        }

        return null;
    }

    private void BeginSplitterResize(TradingChartSplitterLayout splitter, Point position)
    {
        var layout = GetCurrentLayout();
        _isResizingSplitter = true;
        _activeSplitterIndex = splitter.Index;
        _resizeStartY = position.Y;
        _resizeUpperStartHeight = layout.Panels[splitter.UpperPanelIndex].Bounds.Height;
        _resizeLowerStartHeight = 0d;
        for (var i = splitter.LowerPanelIndex; i < layout.Panels.Count; i++)
        {
            _resizeLowerStartHeight += layout.Panels[i].Bounds.Height;
        }
        Cursor = VerticalResizeCursor;
    }

    private void ResizeSplitter(Point position)
    {
        var layout = GetCurrentLayout();
        if (_activeSplitterIndex < 0 || _activeSplitterIndex >= layout.Splitters.Count)
        {
            return;
        }

        var splitter = layout.Splitters[_activeSplitterIndex];
        var visiblePanels = GetVisiblePanelEntries();
        if (splitter.UpperPanelIndex >= visiblePanels.Count || splitter.LowerPanelIndex >= visiblePanels.Count)
        {
            return;
        }

        var availablePanelHeight = 0d;
        for (var i = 0; i < layout.Panels.Count; i++)
        {
            availablePanelHeight += layout.Panels[i].Bounds.Height;
        }

        var upperMin = splitter.UpperPanelIndex == 0 && visiblePanels.Count > 1
            ? Math.Min(availablePanelHeight * 0.5d, Math.Max(40d, availablePanelHeight - ((visiblePanels.Count - 1) * 40d)))
            : 40d;
        var lowerMin = 40d;
        var pairTotal = _resizeUpperStartHeight + _resizeLowerStartHeight;
        var delta = position.Y - _resizeStartY;
        var upperHeight = Math.Clamp(_resizeUpperStartHeight + delta, upperMin, pairTotal - lowerMin);
        var lowerHeight = pairTotal - upperHeight;
        var upperKey = visiblePanels[splitter.UpperPanelIndex].Key;
        _panelWeights[upperKey] = Math.Max(1d, upperHeight);
        var subPanelCount = visiblePanels.Count - 1;
        if (subPanelCount > 0)
        {
            var subHeightPerPanel = Math.Max(1d, lowerHeight / subPanelCount);
            for (var i = 1; i < visiblePanels.Count; i++)
            {
                _panelWeights[visiblePanels[i].Key] = subHeightPerPanel;
            }
        }

        _cachedLayout = null;
        InvalidateVisual();
    }

    private void NormalizeActivePanelIndex()
    {
        var visiblePanelCount = GetVisiblePanelEntries().Count;
        _activePanelIndex = Math.Clamp(_activePanelIndex, 0, Math.Max(0, visiblePanelCount - 1));
    }

    private double GetCrosshairX(Rect bodyBounds, int index)
    {
        var visibleCount = Math.Max(1, _visibleCount);
        var slotWidth = bodyBounds.Width / visibleCount;
        return bodyBounds.X + ((index - _visibleStartIndex + 0.5d) * slotWidth);
    }

    private static bool HasPointerMoved(Point current, Point previous)
    {
        return Math.Abs(current.X - previous.X) > 0.5d || Math.Abs(current.Y - previous.Y) > 0.5d;
    }

    private readonly record struct PanelEntry(object Key, TradingIndicatorSnapshot? Snapshot);
}
