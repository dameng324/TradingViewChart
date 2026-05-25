using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TradingViewChart.Indicators;

public sealed class TradingIndicatorItem : INotifyPropertyChanged
{
    private readonly ObservableCollection<TradingIndicatorParameterValue> _parameters;
    private ITradingIndicator? _explicitIndicator;
    private bool _isHidden;
    private int _version;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TradingIndicatorItem(TradingIndicatorTemplate template)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        _parameters = new ObservableCollection<TradingIndicatorParameterValue>(
            template.Parameters.Select(parameter => new TradingIndicatorParameterValue(
                parameter,
                parameter.DefaultValue
            ))
        );
        _parameters.CollectionChanged += OnParametersCollectionChanged;
        SubscribeParameters(_parameters);
    }

    private TradingIndicatorItem(ITradingIndicator indicator)
    {
        _explicitIndicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
        _parameters = new ObservableCollection<TradingIndicatorParameterValue>();
    }

    public TradingIndicatorTemplate? Template { get; }

    public ObservableCollection<TradingIndicatorParameterValue> Parameters => _parameters;

    public string Id => Template?.Id ?? _explicitIndicator?.Id ?? string.Empty;

    public string DisplayName => BuildIndicator().DisplayName;

    public TradingIndicatorPane Pane => BuildIndicator().Pane;

    public bool CanEdit => Template is not null;

    public bool IsHidden
    {
        get => _isHidden;
        set => SetField(ref _isHidden, value);
    }

    public int Version => _version;

    public static TradingIndicatorItem FromIndicator(ITradingIndicator indicator)
    {
        return new TradingIndicatorItem(indicator);
    }

    public ITradingIndicator BuildIndicator()
    {
        if (Template is null)
        {
            return _explicitIndicator
                ?? throw new InvalidOperationException("Indicator is unavailable.");
        }

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _parameters.Count; i++)
        {
            values[_parameters[i].Definition.Key] = _parameters[i].Value;
        }

        return Template.CreateIndicator(values);
    }

    public TradingIndicatorItem Clone()
    {
        if (Template is null)
        {
            var clone = FromIndicator(BuildIndicator());
            clone.IsHidden = IsHidden;
            return clone;
        }

        var item = new TradingIndicatorItem(Template) { IsHidden = IsHidden };

        item._parameters.Clear();
        for (var i = 0; i < _parameters.Count; i++)
        {
            item._parameters.Add(_parameters[i].Clone());
        }

        return item;
    }

    public void CopyFrom(TradingIndicatorItem source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!string.Equals(Id, source.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Cannot copy parameters from a different indicator type."
            );
        }

        IsHidden = source.IsHidden;
        _parameters.Clear();
        for (var i = 0; i < source._parameters.Count; i++)
        {
            _parameters.Add(source._parameters[i].Clone());
        }

        Touch();
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Pane));
    }

    private void OnParametersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<TradingIndicatorParameterValue>())
            {
                oldItem.PropertyChanged -= OnParameterPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            SubscribeParameters(e.NewItems.OfType<TradingIndicatorParameterValue>());
        }

        Touch();
        OnPropertyChanged(nameof(DisplayName));
    }

    private void SubscribeParameters(IEnumerable<TradingIndicatorParameterValue> parameters)
    {
        foreach (var parameter in parameters)
        {
            parameter.PropertyChanged += OnParameterPropertyChanged;
        }
    }

    private void OnParameterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            string.Equals(
                e.PropertyName,
                nameof(TradingIndicatorParameterValue.Value),
                StringComparison.Ordinal
            )
        )
        {
            Touch();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    private void Touch()
    {
        _version++;
        OnPropertyChanged(nameof(Version));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        Touch();
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
