using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using TradingViewChart.Demo.Indicators;
using TradingViewChart.Indicators;
using TradingViewChart.Models;

namespace TradingViewChart.Demo;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly ObservableCollection<CandlePoint> _candleData;
    private readonly ObservableCollection<PricePoint> _priceData;
    private readonly ObservableCollection<TradingMarker> _candleMarkers;
    private readonly ObservableCollection<TradingMarker> _priceMarkers;
    private readonly DispatcherTimer _fpsTimer;
    private DateTimeOffset? _candleHoveredTime;
    private DateTimeOffset? _priceHoveredTime;
    private int _frameCount;
    private int _selectedDemoIndex;
    private string _fpsText = "FPS: --";
    private bool _candleMarkersVisible = true;
    private bool _priceMarkersVisible = true;

    public new event PropertyChangedEventHandler? PropertyChanged;


    private DemoTradingViewChart ActiveChart => SelectedDemoIndex == 1 ? PriceChart : CandleChart;

    public MainWindow()
    {
        InitializeComponent();
        _candleData = new ObservableCollection<CandlePoint>(DemoDataFactory.CreateCandles(360));
        _priceData = new ObservableCollection<PricePoint>(DemoDataFactory.CreatePrices(360));
        _candleMarkers = new ObservableCollection<TradingMarker>(DemoDataFactory.CreateCandleMarkers(_candleData));
        _priceMarkers = new ObservableCollection<TradingMarker>(DemoDataFactory.CreatePriceMarkers(_priceData));
        _fpsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        DataContext = this;

        ConfigureCharts();
        WireControls();

        _fpsTimer.Tick += OnFpsTimerTick;
        _fpsTimer.Start();

        SelectedDemoIndex = 0;
        StatusTextBlock.Text = "Ready";
    }

    public int SelectedDemoIndex
    {
        get => _selectedDemoIndex;
        set
        {
            if (SetField(ref _selectedDemoIndex, value))
            {
                OnPropertyChanged(nameof(CurrentHoveredTimeText));
                OnPropertyChanged(nameof(CurrentDemoSummaryText));
            }
        }
    }

    public DateTimeOffset? CandleHoveredTime
    {
        get => _candleHoveredTime;
        set
        {
            if (SetField(ref _candleHoveredTime, value))
            {
                OnPropertyChanged(nameof(CurrentHoveredTimeText));
            }
        }
    }

    public DateTimeOffset? PriceHoveredTime
    {
        get => _priceHoveredTime;
        set
        {
            if (SetField(ref _priceHoveredTime, value))
            {
                OnPropertyChanged(nameof(CurrentHoveredTimeText));
            }
        }
    }

    public string FpsText
    {
        get => _fpsText;
        private set => SetField(ref _fpsText, value);
    }

    public string CurrentHoveredTimeText
    {
        get
        {
            var value = SelectedDemoIndex == 1 ? PriceHoveredTime : CandleHoveredTime;
            return value.HasValue
                ? $"HoverTime: {value.Value:yyyy-MM-dd HH:mm:ss}"
                : "HoverTime: --";
        }
    }

    public string CurrentDemoSummaryText
    {
        get
        {
            if (SelectedDemoIndex == 1)
            {
                return $"PriceSource | 数据点: {_priceData.Count} | 标记: {(_priceMarkersVisible ? _priceMarkers.Count : 0)} | 指标: MA + MOM";
            }

            return $"CandleSource | 数据点: {_candleData.Count} | 标记: {(_candleMarkersVisible ? _candleMarkers.Count : 0)} | 指标: MA + MACD + Volume";
        }
    }

    [RequiresUnreferencedCode("Calls Avalonia.Data.Binding.Binding(String, BindingMode)")]
    private void ConfigureCharts()
    {
        CandleChart.Bind(
            global::TradingViewChart.TradingViewChart.HoveredTimeProperty,
            new Binding(nameof(CandleHoveredTime)) { Source = this, Mode = BindingMode.TwoWay });
        PriceChart.Bind(
            global::TradingViewChart.TradingViewChart.HoveredTimeProperty,
            new Binding(nameof(PriceHoveredTime)) { Source = this, Mode = BindingMode.TwoWay });

        CandleChart.CandleSource = _candleData;
        CandleChart.Markers = _candleMarkers;
        CandleChart.Indicators.Add(new MaIndicator(5, 10, 20, 60));
        CandleChart.Indicators.Add(new MacdIndicator());
        CandleChart.ContextMenu = BuildChartContextMenu(() => CandleHoveredTime);

        PriceChart.PriceSource = _priceData;
        PriceChart.Markers = _priceMarkers;
        PriceChart.Indicators.Add(new MaIndicator(12, 36));
        PriceChart.Indicators.Add(new MomentumIndicator(10));
        PriceChart.ContextMenu = BuildChartContextMenu(() => PriceHoveredTime);

        CandleChart.FrameRendered += OnChartFrameRendered;
        PriceChart.FrameRendered += OnChartFrameRendered;
    }

    private void WireControls()
    {
        CrosshairModeComboBox.SelectionChanged += OnCrosshairModeChanged;
        CrosshairValueModeComboBox.SelectionChanged += OnCrosshairValueModeChanged;
        XAxisFormatComboBox.SelectionChanged += OnXAxisFormatChanged;

        CrosshairModeComboBox.SelectedIndex = 0;
        CrosshairValueModeComboBox.SelectedIndex = 0;
        XAxisFormatComboBox.SelectedIndex = 0;
        DemoTabs.SelectedIndex = 0;
    }

    private void OnToggleThemeClick(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = RequestedThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }

    private void OnDemoTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl && tabControl.SelectedIndex >= 0)
        {
            SelectedDemoIndex = tabControl.SelectedIndex;
        }
    }

    private void OnCrosshairModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        var mode = CrosshairModeComboBox.SelectedIndex == 1
            ? global::TradingViewChart.CrosshairMode.FollowMouse
            : global::TradingViewChart.CrosshairMode.FixedCorner;

        CandleChart.CrosshairMode = mode;
        PriceChart.CrosshairMode = mode;
    }

    private void OnCrosshairValueModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        var mode = CrosshairValueModeComboBox.SelectedIndex == 1
            ? global::TradingViewChart.CrosshairValueMode.FollowPointer
            : global::TradingViewChart.CrosshairValueMode.SnapToData;

        CandleChart.CrosshairValueMode = mode;
        PriceChart.CrosshairValueMode = mode;
    }

    private void OnXAxisFormatChanged(object? sender, SelectionChangedEventArgs e)
    {
        var format = XAxisFormatComboBox.SelectedIndex switch
        {
            1 => "MM-dd",
            2 => "yyyy-MM-dd HH:mm",
            _ => "yyyy-MM-dd"
        };

        CandleChart.XAxisLabelFormat = format;
        PriceChart.XAxisLabelFormat = format;
    }

    private void OnToggleMarkersClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedDemoIndex == 1)
        {
            _priceMarkersVisible = !_priceMarkersVisible;
            PriceChart.Markers = _priceMarkersVisible ? _priceMarkers : null;
            StatusTextBlock.Text = _priceMarkersVisible ? "Price markers enabled." : "Price markers hidden.";
        }
        else
        {
            _candleMarkersVisible = !_candleMarkersVisible;
            CandleChart.Markers = _candleMarkersVisible ? _candleMarkers : null;
            StatusTextBlock.Text = _candleMarkersVisible ? "Candle markers enabled." : "Candle markers hidden.";
        }

        OnPropertyChanged(nameof(CurrentDemoSummaryText));
    }

    private void OnAddLatestClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedDemoIndex == 1)
        {
            var point = DemoDataFactory.CreateNextPrice(_priceData);
            _priceData.Add(point);
            StatusTextBlock.Text = $"Added price point {point.Time:yyyy-MM-dd HH:mm:ss}.";
        }
        else
        {
            var point = DemoDataFactory.CreateNextCandle(_candleData);
            _candleData.Add(point);
            StatusTextBlock.Text = $"Added candle {point.Time:yyyy-MM-dd}.";
        }

        OnPropertyChanged(nameof(CurrentDemoSummaryText));
    }

    private void OnUpdateLatestClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedDemoIndex == 1)
        {
            if (_priceData.Count == 0)
            {
                return;
            }

            DemoDataFactory.UpdateLatestPrice(_priceData);
            StatusTextBlock.Text = $"Updated latest price {_priceData[^1].Time:yyyy-MM-dd HH:mm:ss}.";
        }
        else
        {
            if (_candleData.Count == 0)
            {
                return;
            }

            DemoDataFactory.UpdateLatestCandle(_candleData);
            StatusTextBlock.Text = $"Updated latest candle {_candleData[^1].Time:yyyy-MM-dd}.";
        }
    }

    private void OnDeleteLatestClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedDemoIndex == 1)
        {
            if (_priceData.Count <= 1)
            {
                return;
            }

            var removed = _priceData[^1];
            _priceData.RemoveAt(_priceData.Count - 1);
            RemoveMarkersByTime(_priceMarkers, removed.Time);
            StatusTextBlock.Text = $"Deleted price point {removed.Time:yyyy-MM-dd HH:mm:ss}.";
        }
        else
        {
            if (_candleData.Count <= 1)
            {
                return;
            }

            var removed = _candleData[^1];
            _candleData.RemoveAt(_candleData.Count - 1);
            RemoveMarkersByTime(_candleMarkers, removed.Time);
            StatusTextBlock.Text = $"Deleted candle {removed.Time:yyyy-MM-dd}.";
        }

        OnPropertyChanged(nameof(CurrentDemoSummaryText));
    }

    private void OnChartFrameRendered(object? sender, EventArgs e)
    {
        _frameCount++;
    }

    private void OnFpsTimerTick(object? sender, EventArgs e)
    {
        FpsText = $"FPS: {_frameCount}";
        _frameCount = 0;
    }

    private ContextMenu BuildChartContextMenu(Func<DateTimeOffset?> getHoveredTime)
    {
        var inspectItem = new MenuItem();
        inspectItem.Click += (_, _) =>
        {
            var hovered = getHoveredTime();
            StatusTextBlock.Text = hovered.HasValue
                ? $"Inspect {hovered.Value:yyyy-MM-dd HH:mm:ss}"
                : "Inspect unavailable";
        };

        var bookmarkItem = new MenuItem();
        bookmarkItem.Click += (_, _) =>
        {
            var hovered = getHoveredTime();
            StatusTextBlock.Text = hovered.HasValue
                ? $"Bookmark {hovered.Value:yyyy-MM-dd HH:mm:ss}"
                : "Bookmark unavailable";
        };

        var menu = new ContextMenu();
        menu.ItemsSource = new[] { inspectItem, bookmarkItem };
        menu.Opening += (_, _) =>
        {
            var hovered = getHoveredTime();
            var header = hovered?.ToString("yyyy-MM-dd HH:mm:ss") ?? "--";
            inspectItem.Header = $"Inspect {header}";
            bookmarkItem.Header = $"Bookmark {header}";
            inspectItem.IsEnabled = hovered.HasValue;
            bookmarkItem.IsEnabled = hovered.HasValue;
        };

        return menu;
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
