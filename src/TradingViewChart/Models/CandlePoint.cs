using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TradingViewChart.Models;

public class CandlePoint : INotifyPropertyChanged
{
    private DateTimeOffset _time;
    private double _open;
    private double _high;
    private double _low;
    private double _close;
    private double _volume;
    private double _turnover;
    private double? _previousClose;
    private bool _isLimitUp;
    private bool _isLimitDown;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTimeOffset Time
    {
        get => _time;
        set => SetField(ref _time, value);
    }

    public double Open
    {
        get => _open;
        set => SetField(ref _open, value);
    }

    public double High
    {
        get => _high;
        set => SetField(ref _high, value);
    }

    public double Low
    {
        get => _low;
        set => SetField(ref _low, value);
    }

    public double Close
    {
        get => _close;
        set => SetField(ref _close, value);
    }

    public double Volume
    {
        get => _volume;
        set => SetField(ref _volume, value);
    }

    public double Turnover
    {
        get => _turnover;
        set => SetField(ref _turnover, value);
    }

    public double? PreviousClose
    {
        get => _previousClose;
        set => SetField(ref _previousClose, value);
    }

    public bool IsLimitUp
    {
        get => _isLimitUp;
        set => SetField(ref _isLimitUp, value);
    }

    public bool IsLimitDown
    {
        get => _isLimitDown;
        set => SetField(ref _isLimitDown, value);
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
