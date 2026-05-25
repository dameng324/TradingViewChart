using Avalonia.Controls;
using TradingViewChart.Indicators;
using TradingViewChart.Models;
using TUnit.Assertions;
using TUnit.Core;

namespace TradingViewChart.Tests;

public sealed class TradingViewChartLoadTests
{
    [Before(Class)]
    public static Task BeforeClass()
    {
        TestApplication.BuildAvaloniaApp().SetupWithoutStarting();
        return Task.CompletedTask;
    }

    [Test]
    public async Task TradingViewChart_Can_Load_In_Window()
    {
        var chart = new global::TradingViewChart.TradingViewChart
        {
            Width = 960,
            Height = 540,
            CandleSource = CreateSampleData(240),
        };

        chart.Indicators.Add(new MaIndicator(5, 10, 20));
        chart.Indicators.Add(new MacdIndicator());
        chart.Measure(new Avalonia.Size(960, 540));
        chart.Arrange(new Avalonia.Rect(0, 0, 960, 540));

        var window = new Window
        {
            Width = 960,
            Height = 540,
            Content = chart,
        };

        await Assert.That(window.Content).IsNotNull();
        await Assert.That(ReferenceEquals(window.Content, chart)).IsTrue();
        await Assert.That(chart.Indicators.Count).IsEqualTo(2);
    }

    private static IReadOnlyList<CandlePoint> CreateSampleData(int count)
    {
        var list = new List<CandlePoint>(count);
        double price = 100d;

        for (var i = 0; i < count; i++)
        {
            var open = price;
            var drift = Math.Sin(i / 7d) * 1.8d;
            var close = open + drift;
            var high = Math.Max(open, close) + 1.2d;
            var low = Math.Min(open, close) - 1.1d;
            var previousClose = i == 0 ? open : list[i - 1].Close;
            list.Add(
                new CandlePoint
                {
                    Time = new DateTimeOffset(2025, 1, 1, 9, 30, 0, TimeSpan.Zero).AddMinutes(i),
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = 800_000 + (i * 5_000),
                    Turnover = (800_000 + (i * 5_000)) * close,
                    PreviousClose = previousClose,
                }
            );

            price = close + Math.Cos(i / 11d) * 0.5d;
        }

        return list;
    }
}
