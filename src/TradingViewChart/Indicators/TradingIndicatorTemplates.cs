namespace TradingViewChart.Indicators;

public static class TradingIndicatorTemplates
{
    public static TradingIndicatorTemplate MovingAverage { get; } =
        new(
            "MA",
            "MA",
            TradingIndicatorPane.Main,
            [
                new TradingIndicatorParameterDefinition
                {
                    Key = "Periods",
                    DisplayName = "Periods",
                    Kind = TradingIndicatorParameterKind.Text,
                    DefaultValue = "5,10,20",
                },
            ],
            values =>
            {
                var periodsText = values.TryGetValue("Periods", out var value)
                    ? value?.ToString()
                    : null;
                var periods = ParsePeriods(periodsText);
                return new MaIndicator(periods);
            }
        );

    public static TradingIndicatorTemplate Macd { get; } =
        new(
            "MACD",
            "MACD",
            TradingIndicatorPane.Sub,
            [
                new TradingIndicatorParameterDefinition
                {
                    Key = "ShortPeriod",
                    DisplayName = "Short",
                    Kind = TradingIndicatorParameterKind.Integer,
                    DefaultValue = 12d,
                    MinValue = 1d,
                },
                new TradingIndicatorParameterDefinition
                {
                    Key = "LongPeriod",
                    DisplayName = "Long",
                    Kind = TradingIndicatorParameterKind.Integer,
                    DefaultValue = 26d,
                    MinValue = 2d,
                },
                new TradingIndicatorParameterDefinition
                {
                    Key = "SignalPeriod",
                    DisplayName = "Signal",
                    Kind = TradingIndicatorParameterKind.Integer,
                    DefaultValue = 9d,
                    MinValue = 1d,
                },
            ],
            values => new MacdIndicator(
                GetInt(values, "ShortPeriod", 12),
                GetInt(values, "LongPeriod", 26),
                GetInt(values, "SignalPeriod", 9)
            )
        );

    public static TradingIndicatorTemplate Volume { get; } =
        new(
            "VOLUME",
            "Volume",
            TradingIndicatorPane.Sub,
            Array.Empty<TradingIndicatorParameterDefinition>(),
            _ => new VolumeIndicator()
        );

    public static IReadOnlyList<TradingIndicatorTemplate> Default { get; } =
    [MovingAverage, Macd, Volume];

    private static int[] ParsePeriods(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [5, 10, 20];
        }

        var parts = text.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        var periods = new List<int>(parts.Length);
        for (var i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i], out var period) && period > 0)
            {
                periods.Add(period);
            }
        }

        return periods.Count > 0 ? periods.ToArray() : [5, 10, 20];
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> values, string key, int fallback)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            int intValue when intValue > 0 => intValue,
            double doubleValue when doubleValue > 0d => (int)Math.Round(doubleValue),
            float floatValue when floatValue > 0f => (int)Math.Round(floatValue),
            string text when int.TryParse(text, out var parsed) && parsed > 0 => parsed,
            _ => fallback,
        };
    }
}
