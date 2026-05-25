using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using TradingViewChart.Indicators;
using TradingViewChart.Models;

namespace TradingViewChart.Demo;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private int _selectedDemoIndex;
    private string _statusText = "Ready";
    private string _frameText = "Frame: --";

    public MainWindowViewModel()
    {
        CandleChart = ChartDemoViewModel.CreateCandles();
        PriceChart = ChartDemoViewModel.CreatePrices();
        CandleChart.PropertyChanged += OnChartPropertyChanged;
        PriceChart.PropertyChanged += OnChartPropertyChanged;

        ToggleMarkersCommand = new DelegateCommand(ToggleMarkers);
        AddLatestCommand = new DelegateCommand(AddLatest);
        UpdateLatestCommand = new DelegateCommand(UpdateLatest);
        DeleteLatestCommand = new DelegateCommand(DeleteLatest);
        PanLeftCommand = new DelegateCommand(() =>
        {
            CurrentChart.ShiftViewport(-1);
            OnPropertyChanged(nameof(CurrentViewportStartTime));
            OnPropertyChanged(nameof(CurrentViewportEndTime));
            OnPropertyChanged(nameof(CurrentViewportText));
        });
        PanRightCommand = new DelegateCommand(() =>
        {
            CurrentChart.ShiftViewport(1);
            OnPropertyChanged(nameof(CurrentViewportStartTime));
            OnPropertyChanged(nameof(CurrentViewportEndTime));
            OnPropertyChanged(nameof(CurrentViewportText));
        });
        SelectedDemoIndex = 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChartDemoViewModel CandleChart { get; }

    public ChartDemoViewModel PriceChart { get; }

    public IReadOnlyList<CrosshairHintMode> CrosshairModes { get; } =
    [
        CrosshairHintMode.FixedCorner,
        CrosshairHintMode.FollowMouse
    ];

    public IReadOnlyList<CrosshairValueMode> CrosshairValueModes { get; } =
    [
        CrosshairValueMode.SnapToData,
        CrosshairValueMode.FollowPointer
    ];

    public IReadOnlyList<string> XAxisFormats { get; } =
    [
        "yyyy-MM-dd",
        "MM-dd",
        "yyyy-MM-dd HH:mm"
    ];

    public int SelectedDemoIndex
    {
        get => _selectedDemoIndex;
        set
        {
            if (SetField(ref _selectedDemoIndex, value))
            {
                OnPropertyChanged(nameof(CurrentChart));
                OnPropertyChanged(nameof(CurrentHoveredTimeText));
                OnPropertyChanged(nameof(CurrentViewportText));
                OnPropertyChanged(nameof(CurrentIndicatorText));
                OnPropertyChanged(nameof(CurrentViewportStartTime));
                OnPropertyChanged(nameof(CurrentViewportEndTime));
                OnPropertyChanged(nameof(CurrentZoomRatio));
            }
        }
    }

    public ChartDemoViewModel CurrentChart => SelectedDemoIndex == 1 ? PriceChart : CandleChart;

    public DateTimeOffset? CurrentViewportStartTime
    {
        get => CurrentChart.VisibleStartTime;
        set
        {
            CurrentChart.VisibleStartTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentViewportText));
        }
    }

    public DateTimeOffset? CurrentViewportEndTime
    {
        get => CurrentChart.VisibleEndTime;
        set
        {
            CurrentChart.VisibleEndTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentViewportText));
        }
    }

    public double CurrentZoomRatio
    {
        get => CurrentChart.ZoomRatio;
        set
        {
            CurrentChart.ZoomRatio = value;
            OnPropertyChanged();
        }
    }

    public string CurrentHoveredTimeText => CurrentChart.HoveredTime.HasValue
        ? $"Hover: {CurrentChart.HoveredTime.Value:yyyy-MM-dd HH:mm:ss}"
        : "Hover: --";

    public string CurrentViewportText => $"Range: {FormatTime(CurrentChart.VisibleStartTime)} -> {FormatTime(CurrentChart.VisibleEndTime)}";

    public string CurrentIndicatorText => $"Indicators: {string.Join(", ", CurrentChart.IndicatorItems.Select(item => $"{item.DisplayName}{(item.IsHidden ? " (hidden)" : string.Empty)}"))}";

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public string FrameText
    {
        get => _frameText;
        set => SetField(ref _frameText, value);
    }

    public ICommand ToggleMarkersCommand { get; }

    public ICommand AddLatestCommand { get; }

    public ICommand UpdateLatestCommand { get; }

    public ICommand DeleteLatestCommand { get; }

    public ICommand PanLeftCommand { get; }

    public ICommand PanRightCommand { get; }

    public void OnFrameRendered(RenderEventArgs e)
    {
        FrameText = $"Frame: {e.FrameTime.TotalMilliseconds:F1} ms | Alloc: {e.AllocatedBytes / 1024d:F1} KB";
    }

    private void ToggleMarkers()
    {
        CurrentChart.ToggleMarkers();
        StatusText = CurrentChart.Markers is null ? "Markers hidden." : "Markers enabled.";
        OnPropertyChanged(nameof(CurrentIndicatorText));
    }

    private void AddLatest()
    {
        var description = CurrentChart.AddLatest();
        StatusText = description;
        RaiseCurrentChartBindingsChanged();
    }

    private void UpdateLatest()
    {
        var description = CurrentChart.UpdateLatest();
        if (!string.IsNullOrWhiteSpace(description))
        {
            StatusText = description;
            RaiseCurrentChartBindingsChanged();
        }
    }

    private void DeleteLatest()
    {
        var description = CurrentChart.DeleteLatest();
        if (!string.IsNullOrWhiteSpace(description))
        {
            StatusText = description;
            RaiseCurrentChartBindingsChanged();
        }
    }

    private void RaiseCurrentChartBindingsChanged()
    {
        CurrentChart.RefreshComputedProperties();
        OnPropertyChanged(nameof(CurrentHoveredTimeText));
        OnPropertyChanged(nameof(CurrentViewportText));
        OnPropertyChanged(nameof(CurrentIndicatorText));
    }

    private void OnChartPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, CurrentChart))
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(ChartDemoViewModel.HoveredTime):
                OnPropertyChanged(nameof(CurrentHoveredTimeText));
                break;
            case nameof(ChartDemoViewModel.VisibleStartTime):
                OnPropertyChanged(nameof(CurrentViewportStartTime));
                OnPropertyChanged(nameof(CurrentViewportText));
                break;
            case nameof(ChartDemoViewModel.VisibleEndTime):
                OnPropertyChanged(nameof(CurrentViewportEndTime));
                OnPropertyChanged(nameof(CurrentViewportText));
                break;
            case nameof(ChartDemoViewModel.ZoomRatio):
                OnPropertyChanged(nameof(CurrentZoomRatio));
                break;
            case nameof(ChartDemoViewModel.IndicatorItems):
            case nameof(ChartDemoViewModel.LastPointClickText):
                OnPropertyChanged(nameof(CurrentIndicatorText));
                break;
        }
    }

    private static string FormatTime(DateTimeOffset? time) => time?.ToString("yyyy-MM-dd HH:mm:ss") ?? "--";

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ChartDemoViewModel : INotifyPropertyChanged
{
    private ObservableCollection<TradingMarker>? _markers;
    private DateTimeOffset? _hoveredTime;
    private DateTimeOffset? _visibleStartTime;
    private DateTimeOffset? _visibleEndTime;
    private double _zoomRatio = 3d;
    private CrosshairHintMode _crosshairHintMode;
    private CrosshairValueMode _crosshairValueMode;
    private string _xAxisLabelFormat = "yyyy-MM-dd";
    private string _lastPointClickText = "Point click: --";

    private ChartDemoViewModel()
    {
        TogglePointClickCommand = new DelegateCommand(OnPointClicked);
        IndicatorEditor = new DemoIndicatorEditor();
        IndicatorItems.CollectionChanged += OnIndicatorItemsChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CandlePoint>? CandleSource { get; private init; }

    public ObservableCollection<PricePoint>? PriceSource { get; private init; }

    public ObservableCollection<TradingMarker> MarkerStore { get; private init; } = [];

    public ObservableCollection<TradingIndicatorItem> IndicatorItems { get; } = [];

    public ObservableCollection<TradingIndicatorTemplate> SupportedIndicators { get; } = [];

    public ITradingChartIndicatorEditor IndicatorEditor { get; }

    public IBrush? UpBrush { get; set; }

    public IBrush? DownBrush { get; set; }

    public IBrush? LimitUpBrush { get; set; }

    public IBrush? LimitDownBrush { get; set; }

    public IBrush? ChartBackgroundBrush { get; set; }

    public IBrush? GridLineBrush { get; set; }

    public IBrush? AxisTextBrush { get; set; }

    public IBrush? TooltipBackgroundBrush { get; set; }

    public IBrush? TooltipTextBrush { get; set; }

    public ObservableCollection<TradingMarker>? Markers
    {
        get => _markers;
        set => SetField(ref _markers, value);
    }

    public DateTimeOffset? HoveredTime
    {
        get => _hoveredTime;
        set
        {
            if (SetField(ref _hoveredTime, value))
            {
                OnPropertyChanged(nameof(HoveredTimeText));
            }
        }
    }

    public DateTimeOffset? VisibleStartTime
    {
        get => _visibleStartTime;
        set
        {
            if (SetField(ref _visibleStartTime, value))
            {
                OnPropertyChanged(nameof(ViewportText));
            }
        }
    }

    public DateTimeOffset? VisibleEndTime
    {
        get => _visibleEndTime;
        set
        {
            if (SetField(ref _visibleEndTime, value))
            {
                OnPropertyChanged(nameof(ViewportText));
            }
        }
    }

    public double ZoomRatio
    {
        get => _zoomRatio;
        set => SetField(ref _zoomRatio, value);
    }

    public CrosshairHintMode CrosshairHintMode
    {
        get => _crosshairHintMode;
        set => SetField(ref _crosshairHintMode, value);
    }

    public CrosshairValueMode CrosshairValueMode
    {
        get => _crosshairValueMode;
        set => SetField(ref _crosshairValueMode, value);
    }

    public string XAxisLabelFormat
    {
        get => _xAxisLabelFormat;
        set => SetField(ref _xAxisLabelFormat, value);
    }

    public ICommand TogglePointClickCommand { get; }

    public object? PointClickCommandParameter { get; set; }

    public string HoveredTimeText => HoveredTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "--";

    public string ViewportText => $"{VisibleStartTime:yyyy-MM-dd HH:mm:ss} -> {VisibleEndTime:yyyy-MM-dd HH:mm:ss}";

    public string LastPointClickText
    {
        get => _lastPointClickText;
        private set => SetField(ref _lastPointClickText, value);
    }

    public IReadOnlyList<DateTimeOffset> AvailableTimes =>
        CandleSource is not null
            ? CandleSource.Select(point => point.Time).ToList()
            : PriceSource is not null
                ? PriceSource.Select(point => point.Time).ToList()
                : Array.Empty<DateTimeOffset>();

    public static ChartDemoViewModel CreateCandles()
    {
        var candleData = new ObservableCollection<CandlePoint>(DemoDataFactory.CreateCandles(360));
        var markers = new ObservableCollection<TradingMarker>(DemoDataFactory.CreateCandleMarkers(candleData));
        var viewModel = new ChartDemoViewModel
        {
            CandleSource = candleData,
            MarkerStore = markers,
            Markers = markers,
            ZoomRatio = 3d,
            CrosshairHintMode = CrosshairHintMode.FixedCorner,
            CrosshairValueMode = CrosshairValueMode.SnapToData
        };

        viewModel.SupportedIndicators.Add(TradingIndicatorTemplates.MovingAverage);
        viewModel.SupportedIndicators.Add(TradingIndicatorTemplates.Macd);
        viewModel.SupportedIndicators.Add(TradingIndicatorTemplates.Volume);
        viewModel.IndicatorItems.Add(new TradingIndicatorItem(TradingIndicatorTemplates.MovingAverage));
        viewModel.IndicatorItems[0].Parameters[0].Value = "5,10,20,60";
        viewModel.IndicatorItems.Add(new TradingIndicatorItem(TradingIndicatorTemplates.Macd));
        viewModel.RefreshComputedProperties();
        return viewModel;
    }

    public static ChartDemoViewModel CreatePrices()
    {
        var priceData = new ObservableCollection<PricePoint>(DemoDataFactory.CreatePrices(360));
        var markers = new ObservableCollection<TradingMarker>(DemoDataFactory.CreatePriceMarkers(priceData));
        var viewModel = new ChartDemoViewModel
        {
            PriceSource = priceData,
            MarkerStore = markers,
            Markers = markers,
            ZoomRatio = 3d,
            CrosshairHintMode = CrosshairHintMode.FixedCorner,
            CrosshairValueMode = CrosshairValueMode.SnapToData
        };

        viewModel.SupportedIndicators.Add(TradingIndicatorTemplates.MovingAverage);
        viewModel.SupportedIndicators.Add(TradingIndicatorTemplates.Macd);
        var ma = new TradingIndicatorItem(TradingIndicatorTemplates.MovingAverage);
        ma.Parameters[0].Value = "12,36";
        viewModel.IndicatorItems.Add(ma);
        viewModel.RefreshComputedProperties();
        return viewModel;
    }

    public void ToggleMarkers()
    {
        Markers = Markers is null ? MarkerStore : null;
    }

    public string AddLatest()
    {
        if (PriceSource is not null)
        {
            var point = DemoDataFactory.CreateNextPrice(PriceSource);
            PriceSource.Add(point);
            OnPropertyChanged(nameof(AvailableTimes));
            return $"Added price point {point.Time:yyyy-MM-dd HH:mm:ss}.";
        }

        var candle = DemoDataFactory.CreateNextCandle(CandleSource!);
        CandleSource!.Add(candle);
        OnPropertyChanged(nameof(AvailableTimes));
        return $"Added candle {candle.Time:yyyy-MM-dd}.";
    }

    public string UpdateLatest()
    {
        if (PriceSource is not null)
        {
            if (PriceSource.Count == 0)
            {
                return string.Empty;
            }

            DemoDataFactory.UpdateLatestPrice(PriceSource);
            return $"Updated latest price {PriceSource[^1].Time:yyyy-MM-dd HH:mm:ss}.";
        }

        if (CandleSource!.Count == 0)
        {
            return string.Empty;
        }

        DemoDataFactory.UpdateLatestCandle(CandleSource);
        return $"Updated latest candle {CandleSource[^1].Time:yyyy-MM-dd}.";
    }

    public string DeleteLatest()
    {
        if (PriceSource is not null)
        {
            if (PriceSource.Count <= 1)
            {
                return string.Empty;
            }

            var removed = PriceSource[^1];
            PriceSource.RemoveAt(PriceSource.Count - 1);
            RemoveMarkersByTime(MarkerStore, removed.Time);
            OnPropertyChanged(nameof(AvailableTimes));
            return $"Deleted price point {removed.Time:yyyy-MM-dd HH:mm:ss}.";
        }

        if (CandleSource!.Count <= 1)
        {
            return string.Empty;
        }

        var candle = CandleSource[^1];
        CandleSource.RemoveAt(CandleSource.Count - 1);
        RemoveMarkersByTime(MarkerStore, candle.Time);
        OnPropertyChanged(nameof(AvailableTimes));
        return $"Deleted candle {candle.Time:yyyy-MM-dd}.";
    }

    public void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(AvailableTimes));
        OnPropertyChanged(nameof(HoveredTimeText));
        OnPropertyChanged(nameof(ViewportText));
        OnPropertyChanged(nameof(LastPointClickText));
    }

    public void ShiftViewport(int offset)
    {
        var times = AvailableTimes;
        if (times.Count == 0)
        {
            return;
        }

        var startIndex = VisibleStartTime.HasValue ? FindIndex(times, VisibleStartTime.Value) : -1;
        var endIndex = VisibleEndTime.HasValue ? FindIndex(times, VisibleEndTime.Value) : -1;
        if (startIndex < 0 || endIndex < 0)
        {
            return;
        }

        startIndex = Math.Clamp(startIndex + offset, 0, Math.Max(0, times.Count - 1));
        endIndex = Math.Clamp(endIndex + offset, 0, Math.Max(0, times.Count - 1));
        VisibleStartTime = times[startIndex];
        VisibleEndTime = times[endIndex];
    }

    private static int FindIndex(IReadOnlyList<DateTimeOffset> items, DateTimeOffset value)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] == value)
            {
                return i;
            }
        }

        return -1;
    }

    private void OnPointClicked(object? parameter)
    {
        if (parameter is TradingChartPointClickInfo info)
        {
            LastPointClickText = $"Point click: {info.Index} @ {info.Time:yyyy-MM-dd HH:mm:ss}";
        }
    }

    private void OnIndicatorItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<TradingIndicatorItem>())
            {
                item.PropertyChanged -= OnIndicatorItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<TradingIndicatorItem>())
            {
                item.PropertyChanged += OnIndicatorItemPropertyChanged;
            }
        }

        OnPropertyChanged(nameof(IndicatorItems));
    }

    private void OnIndicatorItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IndicatorItems));
    }

    private static void RemoveMarkersByTime(ObservableCollection<TradingMarker> markers, DateTimeOffset time)
    {
        for (var i = markers.Count - 1; i >= 0; i--)
        {
            if (markers[i].Time == time)
            {
                markers.RemoveAt(i);
            }
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
