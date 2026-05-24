using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TradingViewChart.Models;

public sealed class PricePoint : INotifyPropertyChanged
{
    private DateTimeOffset _time;
    private double _price;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateTimeOffset Time
    {
        get => _time;
        set => SetField(ref _time, value);
    }

    public double Price
    {
        get => _price;
        set => SetField(ref _price, value);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
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
