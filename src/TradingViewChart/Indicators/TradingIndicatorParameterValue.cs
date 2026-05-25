using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TradingViewChart.Indicators;

public sealed class TradingIndicatorParameterValue : INotifyPropertyChanged
{
    private object? _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TradingIndicatorParameterValue(
        TradingIndicatorParameterDefinition definition,
        object? value
    )
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _value = value;
    }

    public TradingIndicatorParameterDefinition Definition { get; }

    public object? Value
    {
        get => _value;
        set
        {
            if (Equals(_value, value))
            {
                return;
            }

            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public TradingIndicatorParameterValue Clone()
    {
        return new TradingIndicatorParameterValue(Definition, Value);
    }
}
