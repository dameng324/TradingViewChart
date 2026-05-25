namespace TradingViewChart.Indicators;

public sealed class TradingIndicatorParameterDefinition
{
    public required string Key { get; init; }

    public required string DisplayName { get; init; }

    public required TradingIndicatorParameterKind Kind { get; init; }

    public required object? DefaultValue { get; init; }

    public double? MinValue { get; init; }

    public double? MaxValue { get; init; }

    public string? Description { get; init; }
}
