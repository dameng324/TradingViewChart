namespace TradingViewChart.Indicators;

public sealed class TradingIndicatorTemplate
{
    private readonly Func<IReadOnlyDictionary<string, object?>, ITradingIndicator> _factory;

    public TradingIndicatorTemplate(
        string id,
        string displayName,
        TradingIndicatorPane pane,
        IReadOnlyList<TradingIndicatorParameterDefinition>? parameters,
        Func<IReadOnlyDictionary<string, object?>, ITradingIndicator> factory
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Id = id;
        DisplayName = displayName;
        Pane = pane;
        Parameters = parameters ?? Array.Empty<TradingIndicatorParameterDefinition>();
    }

    public string Id { get; }

    public string DisplayName { get; }

    public TradingIndicatorPane Pane { get; }

    public IReadOnlyList<TradingIndicatorParameterDefinition> Parameters { get; }

    public TradingIndicatorItem CreateDefaultItem()
    {
        return new TradingIndicatorItem(this);
    }

    internal ITradingIndicator CreateIndicator(IReadOnlyDictionary<string, object?> values)
    {
        return _factory(values);
    }
}
