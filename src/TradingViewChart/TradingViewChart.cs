using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    public static readonly StyledProperty<CrosshairHintMode> CrosshairHintModeProperty =
        AvaloniaProperty.Register<TradingViewChart, CrosshairHintMode>(nameof(CrosshairHintMode), CrosshairHintMode.FixedCorner);

    public static readonly StyledProperty<CrosshairHintMode> CrosshairModeProperty = CrosshairHintModeProperty;

    public static readonly StyledProperty<CrosshairValueMode> CrosshairValueModeProperty =
        AvaloniaProperty.Register<TradingViewChart, CrosshairValueMode>(nameof(CrosshairValueMode), CrosshairValueMode.SnapToData);

    public static readonly StyledProperty<string> XAxisLabelFormatProperty =
        AvaloniaProperty.Register<TradingViewChart, string>(nameof(XAxisLabelFormat), "yyyy-MM-dd");

    public static readonly StyledProperty<DateTimeOffset?> HoveredTimeProperty =
        AvaloniaProperty.Register<TradingViewChart, DateTimeOffset?>(
            nameof(HoveredTime),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList<TradingIndicatorItem>?> IndicatorItemsProperty =
        AvaloniaProperty.Register<TradingViewChart, IList<TradingIndicatorItem>?>(
            nameof(IndicatorItems),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyList<TradingIndicatorTemplate>?> SupportedIndicatorsProperty =
        AvaloniaProperty.Register<TradingViewChart, IReadOnlyList<TradingIndicatorTemplate>?>(nameof(SupportedIndicators));

    public static readonly StyledProperty<DateTimeOffset?> VisibleStartTimeProperty =
        AvaloniaProperty.Register<TradingViewChart, DateTimeOffset?>(
            nameof(VisibleStartTime),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTimeOffset?> VisibleEndTimeProperty =
        AvaloniaProperty.Register<TradingViewChart, DateTimeOffset?>(
            nameof(VisibleEndTime),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> ZoomRatioProperty =
        AvaloniaProperty.Register<TradingViewChart, double>(
            nameof(ZoomRatio),
            1d,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> PointClickCommandProperty =
        AvaloniaProperty.Register<TradingViewChart, ICommand?>(nameof(PointClickCommand));

    public static readonly StyledProperty<object?> PointClickCommandParameterProperty =
        AvaloniaProperty.Register<TradingViewChart, object?>(nameof(PointClickCommandParameter));

    public static readonly StyledProperty<ITradingChartIndicatorEditor?> IndicatorEditorProperty =
        AvaloniaProperty.Register<TradingViewChart, ITradingChartIndicatorEditor?>(nameof(IndicatorEditor));

    private readonly TradingChartRenderer _renderer = new();
    private readonly VolumeIndicator _defaultVolumeIndicator = new();
    private readonly ObservableCollection<ITradingIndicator> _indicators = [];
    private readonly List<TradingIndicatorItem> _legacyIndicatorItems = [];
    private readonly Dictionary<ITradingIndicator, TradingIndicatorResult> _indicatorCache = new();
    private readonly List<TradingIndicatorSnapshot> _mainIndicatorSnapshots = [];
    private readonly List<TradingIndicatorSnapshot> _subIndicatorSnapshots = [];
    private readonly HashSet<TradingSeriesKey> _hiddenSeries = [];
    private readonly Dictionary<object, double> _panelWeights = [];
    private readonly List<CandlePoint> _renderData = [];
    private readonly HashSet<INotifyPropertyChanged> _subscribedDataItems = [];
    private readonly HashSet<TradingIndicatorItem> _subscribedIndicatorItems = [];
    private static readonly IReadOnlyList<TradingMarker> EmptyMarkers = Array.Empty<TradingMarker>();
    private static readonly IReadOnlySet<int> EmptyMarkedIndices = new HashSet<int>();
    private INotifyCollectionChanged? _activeSourceNotifier;
    private INotifyCollectionChanged? _markersNotifier;
    private INotifyCollectionChanged? _indicatorItemsNotifier;
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
    private bool _isUpdatingViewportBindings;
    private bool _isPotentialPointClick;
    private bool _didPanViewport;
    private Point _lastPointerPosition;
    private Point _lastPhysicalPointerPosition;
    private Point _pointerPressedPosition;
    private int _visibleStartIndex;
    private int _visibleCount;
    private int _crosshairIndex = -1;
    private int _activePanelIndex;
    private int _panStartVisibleIndex;
    private int _pressedDataIndex = -1;
    private Point _panStartPosition;
    private double _resizeStartY;
    private double _resizeUpperStartHeight;
    private double _resizeLowerStartHeight;
    private int _activeSplitterIndex = -1;
    private ITradingIndicator? _hoveredIndicator;
    private string? _hoveredSeriesName;
    private ContextMenu? _indicatorActionMenu;
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
            CrosshairHintModeProperty,
            CrosshairValueModeProperty,
            XAxisLabelFormatProperty,
            SupportedIndicatorsProperty);

        CandleSourceProperty.Changed.AddClassHandler<TradingViewChart>((chart, _) => chart.OnCandleSourceChanged());
        PriceSourceProperty.Changed.AddClassHandler<TradingViewChart>((chart, _) => chart.OnPriceSourceChanged());
        MarkersProperty.Changed.AddClassHandler<TradingViewChart>((chart, _) => chart.OnMarkersChanged());
        IndicatorItemsProperty.Changed.AddClassHandler<TradingViewChart>((chart, _) => chart.OnIndicatorItemsChanged());
        VisibleStartTimeProperty.Changed.AddClassHandler<TradingViewChart>((chart, args) => chart.OnVisibleStartTimeChanged((DateTimeOffset?)args.NewValue));
        VisibleEndTimeProperty.Changed.AddClassHandler<TradingViewChart>((chart, args) => chart.OnVisibleEndTimeChanged((DateTimeOffset?)args.NewValue));
        ZoomRatioProperty.Changed.AddClassHandler<TradingViewChart>((chart, args) => chart.OnZoomRatioChanged((double)args.NewValue!));
    }

    public TradingViewChart()
    {
        _indicators.CollectionChanged += OnIndicatorsChanged;
        RebuildLegacyIndicatorItems();
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

    public IList<TradingIndicatorItem>? IndicatorItems
    {
        get => GetValue(IndicatorItemsProperty);
        set => SetValue(IndicatorItemsProperty, value);
    }

    public IReadOnlyList<TradingIndicatorTemplate>? SupportedIndicators
    {
        get => GetValue(SupportedIndicatorsProperty);
        set => SetValue(SupportedIndicatorsProperty, value);
    }

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

    public CrosshairHintMode CrosshairHintMode
    {
        get => GetValue(CrosshairHintModeProperty);
        set => SetValue(CrosshairHintModeProperty, value);
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

    public DateTimeOffset? VisibleStartTime
    {
        get => GetValue(VisibleStartTimeProperty);
        set => SetValue(VisibleStartTimeProperty, value);
    }

    public DateTimeOffset? VisibleEndTime
    {
        get => GetValue(VisibleEndTimeProperty);
        set => SetValue(VisibleEndTimeProperty, value);
    }

    public double ZoomRatio
    {
        get => GetValue(ZoomRatioProperty);
        set => SetValue(ZoomRatioProperty, value);
    }

    public ICommand? PointClickCommand
    {
        get => GetValue(PointClickCommandProperty);
        set => SetValue(PointClickCommandProperty, value);
    }

    public object? PointClickCommandParameter
    {
        get => GetValue(PointClickCommandParameterProperty);
        set => SetValue(PointClickCommandParameterProperty, value);
    }

    public ITradingChartIndicatorEditor? IndicatorEditor
    {
        get => GetValue(IndicatorEditorProperty);
        set => SetValue(IndicatorEditorProperty, value);
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

    public void PanByBars(int barOffset)
    {
        EnsureRenderData();
        if (_renderData.Count == 0 || _visibleCount <= 0)
        {
            return;
        }

        var maxStart = Math.Max(0, _renderData.Count - _visibleCount);
        var nextVisibleStartIndex = Math.Clamp(_visibleStartIndex + barOffset, 0, maxStart);
        if (nextVisibleStartIndex == _visibleStartIndex)
        {
            return;
        }

        _visibleStartIndex = nextVisibleStartIndex;
        PublishViewportBindings();
        InvalidateVisual();
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
            var overlayTarget = TryGetOverlayHitTarget(position);
            if (overlayTarget.HasValue)
            {
                if (overlayTarget.Value.Action == TradingOverlayAction.AddIndicator)
                {
                    OpenIndicatorPicker();
                }

                e.Handled = true;
                return;
            }

            var legendTarget = TryGetLegendHitTarget(position);
            if (legendTarget.HasValue)
            {
                HandleLegendTarget(legendTarget.Value);
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
            _pointerPressedPosition = position;
            _pressedDataIndex = TryHitTestDataIndex(position);
            _isPotentialPointClick = _pressedDataIndex >= 0;
            _didPanViewport = false;
            Cursor = HandCursor;
            e.Pointer.Capture(this);
            UpdateCrosshair(position);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isPanning && _didPanViewport)
        {
            SyncViewportState();
        }

        if (_isPotentialPointClick && e.InitialPressMouseButton == MouseButton.Left)
        {
            ExecutePointClick(_pressedDataIndex);
        }

        _isPanning = false;
        _isResizingSplitter = false;
        _isPotentialPointClick = false;
        _didPanViewport = false;
        _pressedDataIndex = -1;
        _activeSplitterIndex = -1;
        e.Pointer.Capture(null);
        UpdatePointerFeedback(e.GetPosition(this));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (string.Equals(change.Property.Name, nameof(ActualThemeVariant), StringComparison.Ordinal) ||
            string.Equals(change.Property.Name, "RequestedThemeVariant", StringComparison.Ordinal))
        {
            InvalidateVisual();
        }
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
                if (_isPotentialPointClick && HasPointerMoved(position, _pointerPressedPosition))
                {
                    _isPotentialPointClick = false;
                }

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

    private void OnIndicatorItemsChanged()
    {
        if (_indicatorItemsNotifier is not null)
        {
            _indicatorItemsNotifier.CollectionChanged -= OnIndicatorItemsCollectionChanged;
            _indicatorItemsNotifier = null;
        }

        if (IndicatorItems is INotifyCollectionChanged notifier)
        {
            _indicatorItemsNotifier = notifier;
            _indicatorItemsNotifier.CollectionChanged += OnIndicatorItemsCollectionChanged;
        }

        RebuildIndicatorSubscriptions();
        _indicatorCacheDirty = true;
        _cachedLayout = null;
        InvalidateVisual();
    }

    private void OnIndicatorItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildIndicatorSubscriptions();
        _indicatorCacheDirty = true;
        _cachedLayout = null;
        InvalidateVisual();
    }

    private void RebuildIndicatorSubscriptions()
    {
        foreach (var item in _subscribedIndicatorItems)
        {
            item.PropertyChanged -= OnIndicatorItemPropertyChanged;
        }

        _subscribedIndicatorItems.Clear();
        foreach (var item in GetIndicatorItems())
        {
            item.PropertyChanged += OnIndicatorItemPropertyChanged;
            _subscribedIndicatorItems.Add(item);
        }
    }

    private void OnIndicatorItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _indicatorCacheDirty = true;
        _cachedLayout = null;
        InvalidateVisual();
    }

    private void OnVisibleStartTimeChanged(DateTimeOffset? value)
    {
        if (_isUpdatingViewportBindings || !value.HasValue)
        {
            return;
        }

        EnsureRenderData();
        if (_renderData.Count == 0 || _visibleCount <= 0)
        {
            return;
        }

        var index = FindNearestIndex(value.Value);
        var nextVisibleStartIndex = Math.Clamp(index, 0, Math.Max(0, _renderData.Count - _visibleCount));
        if (nextVisibleStartIndex == _visibleStartIndex)
        {
            return;
        }

        _visibleStartIndex = nextVisibleStartIndex;
        PublishViewportBindings();
        InvalidateVisual();
    }

    private void OnVisibleEndTimeChanged(DateTimeOffset? value)
    {
        if (_isUpdatingViewportBindings || !value.HasValue)
        {
            return;
        }

        EnsureRenderData();
        if (_renderData.Count == 0 || _visibleCount <= 0)
        {
            return;
        }

        var index = FindNearestIndex(value.Value);
        var nextVisibleStartIndex = Math.Clamp(index - _visibleCount + 1, 0, Math.Max(0, _renderData.Count - _visibleCount));
        if (nextVisibleStartIndex == _visibleStartIndex)
        {
            return;
        }

        _visibleStartIndex = nextVisibleStartIndex;
        PublishViewportBindings();
        InvalidateVisual();
    }

    private void OnZoomRatioChanged(double value)
    {
        if (_isUpdatingViewportBindings)
        {
            return;
        }

        EnsureRenderData();
        if (_renderData.Count == 0)
        {
            return;
        }

        var nextVisibleCount = ConvertZoomRatioToVisibleCount(value, _renderData.Count);
        ApplyVisibleCount(nextVisibleCount);
    }

    private void PublishViewportBindings()
    {
        SyncHoveredTime();
        SyncViewportBindings();
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
                PublishViewportBindings();
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Replace)
        {
            _crosshairIndex = nextCount <= 0 ? -1 : Math.Clamp(_crosshairIndex, 0, nextCount - 1);
            _visibleStartIndex = nextCount <= 0 ? 0 : Math.Clamp(_visibleStartIndex, 0, Math.Max(0, nextCount - Math.Max(1, _visibleCount)));
            PublishViewportBindings();
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
            PublishViewportBindings();
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

    private IReadOnlyList<TradingIndicatorItem> GetIndicatorItems()
    {
        if (IndicatorItems is IReadOnlyList<TradingIndicatorItem> readOnlyList)
        {
            return readOnlyList;
        }

        if (IndicatorItems is not null)
        {
            return IndicatorItems.Cast<TradingIndicatorItem>().ToList();
        }

        return _legacyIndicatorItems;
    }

    private void RebuildLegacyIndicatorItems()
    {
        if (IndicatorItems is not null)
        {
            return;
        }

        _legacyIndicatorItems.Clear();
        for (var i = 0; i < _indicators.Count; i++)
        {
            _legacyIndicatorItems.Add(TradingIndicatorItem.FromIndicator(_indicators[i]));
        }

        RebuildIndicatorSubscriptions();
    }

    private void OnIndicatorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildLegacyIndicatorItems();
        var activeIndicators = new HashSet<object>(_legacyIndicatorItems.Cast<object>()) { _defaultVolumeIndicator };
        var staleKeys = new List<object>();
        foreach (var key in _panelWeights.Keys)
        {
            if (ReferenceEquals(key, MainPanelKey))
            {
                continue;
            }

            if (!activeIndicators.Contains(key))
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
            SyncViewportBindings();
            return;
        }

        _visibleCount = Math.Min(120, dataCount);
        _visibleCount = Math.Max(20, _visibleCount);
        _visibleCount = Math.Min(_visibleCount, dataCount);
        _visibleStartIndex = Math.Max(0, dataCount - _visibleCount);
        _crosshairIndex = dataCount - 1;
        _activePanelIndex = 0;
        PublishViewportBindings();
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
            AddIndicatorSnapshot(null, _defaultVolumeIndicator, _defaultVolumeIndicator, data);
        }

        var indicatorItems = GetIndicatorItems();
        for (var i = 0; i < indicatorItems.Count; i++)
        {
            var item = indicatorItems[i];
            var indicator = item.BuildIndicator();
            AddIndicatorSnapshot(item, indicator, item, data);
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
            SupportedIndicators = SupportedIndicators ?? TradingIndicatorTemplates.Default,
            Markers = Markers ?? EmptyMarkers,
            HiddenSeries = _hiddenSeries,
            VisibleStartIndex = _visibleStartIndex,
            VisibleCount = _visibleCount == 0 && data.Count > 0 ? data.Count : Math.Max(0, _visibleCount),
            CrosshairIndex = _crosshairIndex >= 0 ? _crosshairIndex : Math.Max(0, data.Count - 1),
            ActivePanelIndex = _activePanelIndex,
            ShowCrosshair = _crosshairIndex >= 0,
            HoveredIndicator = _hoveredIndicator,
            HoveredSeriesName = _hoveredSeriesName,
            PointerPosition = _lastPointerPosition,
            CrosshairHintMode = CrosshairHintMode,
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
            MarkedIndices = EmptyMarkedIndices
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
        _didPanViewport = true;
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
        PublishViewportBindings();
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
        PublishViewportBindings();
        InvalidateVisual();
    }

    private void UpdatePointerFeedback(Point position)
    {
        EnsureIndicatorCache();
        var layout = GetCurrentLayout();
        var splitter = HitTestSplitter(layout, position);
        var legendTarget = splitter.HasValue || _isPanning ? null : TryGetLegendHitTarget(position);
        var hoveredIndicator = default(ITradingIndicator);
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

    private TradingOverlayHitTarget? TryGetOverlayHitTarget(Point position)
    {
        EnsureIndicatorCache();
        return _renderer.HitTestOverlay(CreateRenderModel(_renderData), position);
    }

    private void ShowIndicatorActionMenu(TradingIndicatorItem item)
    {
        if (_indicatorActionMenu is not null)
        {
            _indicatorActionMenu.Close();
        }

        var hideShowItem = new MenuItem
        {
            Header = item.IsHidden ? "Show" : "Hide"
        };
        hideShowItem.Click += (_, _) =>
        {
            item.IsHidden = !item.IsHidden;
            _indicatorCacheDirty = true;
            _cachedLayout = null;
            NormalizeActivePanelIndex();
            PublishViewportBindings();
            InvalidateVisual();
        };

        var editItem = new MenuItem
        {
            Header = "Edit",
            IsEnabled = item.CanEdit && IndicatorEditor is not null
        };
        editItem.Click += async (_, _) => await EditIndicatorAsync(item, isNewItem: false);

        var deleteItem = new MenuItem
        {
            Header = "Delete",
            IsEnabled = CanDeleteIndicatorItem(item)
        };
        deleteItem.Click += (_, _) => DeleteIndicatorItem(item);

        _indicatorActionMenu = new ContextMenu
        {
            Placement = PlacementMode.AnchorAndGravity,
            PlacementRect = new Rect(_lastPointerPosition, new Size(1d, 1d)),
            ItemsSource = new[] { hideShowItem, editItem, deleteItem }
        };
        _indicatorActionMenu.Open(this);
    }

    private async Task EditIndicatorAsync(TradingIndicatorItem item, bool isNewItem)
    {
        var draft = item.Clone();
        var title = isNewItem ? $"Add {draft.DisplayName}" : $"Edit {draft.DisplayName}";
        if (IndicatorEditor is null ||
            !await IndicatorEditor.EditAsync(this, new TradingIndicatorEditorRequest
            {
                Title = title,
                IsNewItem = isNewItem,
                Item = draft
            }))
        {
            return;
        }

        if (isNewItem)
        {
            if (IndicatorItems is not null)
            {
                IndicatorItems.Add(draft);
            }
            else
            {
                _indicators.Add(draft.BuildIndicator());
            }
        }
        else
        {
            item.CopyFrom(draft);
        }

        _indicatorCacheDirty = true;
        _cachedLayout = null;
        NormalizeActivePanelIndex();
        PublishViewportBindings();
        InvalidateVisual();
    }

    private void OpenIndicatorPicker()
    {
        var templates = SupportedIndicators ?? TradingIndicatorTemplates.Default;
        if (templates.Count == 0)
        {
            return;
        }

        var menu = new ContextMenu();
        var items = new List<MenuItem>(templates.Count);
        for (var i = 0; i < templates.Count; i++)
        {
            var template = templates[i];
            var menuItem = new MenuItem
            {
                Header = template.DisplayName
            };
            menuItem.Click += async (_, _) => await EditIndicatorAsync(template.CreateDefaultItem(), isNewItem: true);
            items.Add(menuItem);
        }

        menu.ItemsSource = items;
        menu.Open(this);
    }

    private bool CanDeleteIndicatorItem(TradingIndicatorItem item)
    {
        return IndicatorItems?.Contains(item) == true;
    }

    private void DeleteIndicatorItem(TradingIndicatorItem item)
    {
        if (IndicatorItems?.Contains(item) != true)
        {
            return;
        }

        IndicatorItems.Remove(item);
        _hiddenSeries.RemoveWhere(series => ReferenceEquals(series.OwnerKey, item));
        _panelWeights.Remove(item);
        _indicatorCacheDirty = true;
        _cachedLayout = null;
        NormalizeActivePanelIndex();
        PublishViewportBindings();
        InvalidateVisual();
    }

    private int TryHitTestDataIndex(Point position)
    {
        if (_renderData.Count == 0)
        {
            return -1;
        }

        var layout = GetCurrentLayout();
        for (var i = 0; i < layout.Panels.Count; i++)
        {
            var panel = layout.Panels[i];
            if (!panel.BodyBounds.Contains(position))
            {
                continue;
            }

            var slotWidth = panel.BodyBounds.Width / Math.Max(1, _visibleCount);
            var relativeIndex = (int)Math.Floor((position.X - panel.BodyBounds.X) / Math.Max(1d, slotWidth));
            return Math.Clamp(_visibleStartIndex + relativeIndex, 0, _renderData.Count - 1);
        }

        return -1;
    }

    private void ExecutePointClick(int index)
    {
        if (index < 0 || index >= _renderData.Count || PointClickCommand is null)
        {
            return;
        }

        var parameter = PointClickCommandParameter ?? new TradingChartPointClickInfo
        {
            Index = index,
            Time = _renderData[index].Time,
            Candle = _renderData[index],
            SourceItem = GetActiveSourceItem(index)
        };

        if (PointClickCommand.CanExecute(parameter))
        {
            PointClickCommand.Execute(parameter);
        }
    }

    private void HandleLegendTarget(TradingLegendHitTarget target)
    {
        if (target.Action == TradingLegendAction.IndicatorMenu)
        {
            if (target.Item is not null)
            {
                ShowIndicatorActionMenu(target.Item);
            }
        }
        else if (!string.IsNullOrWhiteSpace(target.SeriesName))
        {
            var key = new TradingSeriesKey(target.OwnerKey, target.SeriesName);
            if (!_hiddenSeries.Add(key))
            {
                _hiddenSeries.Remove(key);
            }

            InvalidateVisual();
        }
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
        var indicatorItems = GetIndicatorItems();
        for (var i = 0; i < indicatorItems.Count; i++)
        {
            if (indicatorItems[i].Pane == TradingIndicatorPane.Sub &&
                string.Equals(indicatorItems[i].Id, _defaultVolumeIndicator.Id, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void AddIndicatorSnapshot(TradingIndicatorItem? item, ITradingIndicator indicator, object ownerKey, IReadOnlyList<CandlePoint> data)
    {
        var result = indicator.Calculate(data);
        _indicatorCache[indicator] = result;

        var snapshot = new TradingIndicatorSnapshot
        {
            Item = item,
            Indicator = indicator,
            OwnerKey = ownerKey,
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
            if (!_subIndicatorSnapshots[i].IsHidden)
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
            if (snapshot.IsHidden)
            {
                continue;
            }

            entries.Add(new PanelEntry(snapshot.OwnerKey, snapshot));
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

    private object? GetActiveSourceItem(int index)
    {
        if (PriceSource is not null && index >= 0 && index < PriceSource.Count)
        {
            return PriceSource[index];
        }

        if (CandleSource is not null && index >= 0 && index < CandleSource.Count)
        {
            return CandleSource[index];
        }

        return null;
    }

    private int FindNearestIndex(DateTimeOffset time)
    {
        if (_renderData.Count == 0)
        {
            return -1;
        }

        var bestIndex = 0;
        var bestDistance = Math.Abs((_renderData[0].Time - time).Ticks);
        for (var i = 1; i < _renderData.Count; i++)
        {
            var distance = Math.Abs((_renderData[i].Time - time).Ticks);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void ApplyVisibleCount(int nextVisibleCount)
    {
        if (_renderData.Count == 0)
        {
            return;
        }

        nextVisibleCount = Math.Clamp(nextVisibleCount, 10, _renderData.Count);
        if (nextVisibleCount == _visibleCount)
        {
            SyncViewportBindings();
            InvalidateVisual();
            return;
        }

        var oldCount = Math.Max(1, _visibleCount);
        var anchorIndex = Math.Clamp(_visibleStartIndex + (oldCount / 2), 0, _renderData.Count - 1);
        var nextVisibleStartIndex = Math.Clamp(anchorIndex - (nextVisibleCount / 2), 0, Math.Max(0, _renderData.Count - nextVisibleCount));
        _visibleCount = nextVisibleCount;
        _visibleStartIndex = nextVisibleStartIndex;
        _crosshairIndex = Math.Clamp(_crosshairIndex < 0 ? anchorIndex : _crosshairIndex, 0, _renderData.Count - 1);
        PublishViewportBindings();
        InvalidateVisual();
    }

    private int ConvertZoomRatioToVisibleCount(double zoomRatio, int dataCount)
    {
        var safeRatio = Math.Max(0.1d, zoomRatio);
        return Math.Clamp((int)Math.Round(dataCount / safeRatio), 10, Math.Max(10, dataCount));
    }

    private double CalculateZoomRatio()
    {
        return _renderData.Count <= 0 || _visibleCount <= 0
            ? 1d
            : Math.Max(0.1d, _renderData.Count / (double)_visibleCount);
    }

    private void SyncViewportState()
    {
        PublishViewportBindings();
    }

    private void SyncViewportBindings()
    {
        if (_isUpdatingViewportBindings)
        {
            return;
        }

        try
        {
            _isUpdatingViewportBindings = true;
            DateTimeOffset? start = _renderData.Count > 0 && _visibleCount > 0 && _visibleStartIndex >= 0 && _visibleStartIndex < _renderData.Count
                ? _renderData[_visibleStartIndex].Time
                : null;
            var endIndex = _renderData.Count > 0 && _visibleCount > 0
                ? Math.Clamp(_visibleStartIndex + _visibleCount - 1, 0, _renderData.Count - 1)
                : -1;
            DateTimeOffset? end = endIndex >= 0 ? _renderData[endIndex].Time : null;

            SetCurrentValue(VisibleStartTimeProperty, start);
            SetCurrentValue(VisibleEndTimeProperty, end);
            SetCurrentValue(ZoomRatioProperty, CalculateZoomRatio());
        }
        finally
        {
            _isUpdatingViewportBindings = false;
        }
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
