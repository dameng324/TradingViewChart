using TradingViewChart.Models;

namespace TradingViewChart.Demo;

internal static class DemoDataFactory
{
    public static IReadOnlyList<CandlePoint> CreateCandles(int count)
    {
        var list = new List<CandlePoint>(count);
        var time = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        double price = 100d;

        for (var i = 0; i < count; i++)
        {
            var cycle = Math.Sin(i / 13d) * 1.9d;
            var trend = (i / 90d) * 1.5d;
            var open = price;
            var close = open + cycle + Math.Cos(i / 9d) + trend * 0.04d;
            var high = Math.Max(open, close) + 1.1d + (Math.Sin(i / 6d) * 0.6d);
            var low = Math.Min(open, close) - 1.1d - (Math.Cos(i / 5d) * 0.5d);
            var volume = 900_000d + (Math.Abs(Math.Sin(i / 8d)) * 700_000d) + (i * 1200d);
            var previousClose = i == 0 ? open : list[i - 1].Close;
            var isRising = close >= open;

            list.Add(
                new CandlePoint
                {
                    Time = time.AddDays(i),
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                    Turnover = volume * close,
                    PreviousClose = previousClose,
                    IsLimitUp = i % 97 == 0 && i > 0 && isRising,
                    IsLimitDown = i % 131 == 0 && i > 0 && !isRising,
                }
            );

            price = close + (Math.Sin(i / 17d) * 0.4d);
        }

        return list;
    }

    public static IReadOnlyList<PricePoint> CreatePrices(int count)
    {
        var list = new List<PricePoint>(count);
        var time = new DateTimeOffset(2025, 1, 1, 9, 30, 0, TimeSpan.Zero);
        double price = 100d;

        for (var i = 0; i < count; i++)
        {
            price += Math.Sin(i / 10d) * 0.7d + Math.Cos(i / 16d) * 0.45d + 0.08d;
            list.Add(new PricePoint { Time = time.AddMinutes(i * 5), Price = price });
        }

        return list;
    }

    public static IReadOnlyList<TradingMarker> CreateCandleMarkers(IReadOnlyList<CandlePoint> data)
    {
        if (data.Count == 0)
        {
            return [];
        }

        return
        [
            new TradingMarker
            {
                Time = data[Math.Min(32, data.Count - 1)].Time,
                Shape = TradingMarkerShape.UpArrow,
                Placement = TradingMarkerPlacement.Below,
                Note = "Buy",
            },
            new TradingMarker
            {
                Time = data[Math.Min(87, data.Count - 1)].Time,
                Shape = TradingMarkerShape.Star,
                Placement = TradingMarkerPlacement.Above,
                Note = "Breakout",
            },
            new TradingMarker
            {
                Time = data[Math.Min(148, data.Count - 1)].Time,
                IndicatorId = "MACD",
                SeriesName = "DEA",
                Shape = TradingMarkerShape.Circle,
                Placement = TradingMarkerPlacement.Above,
                Note = "DEA",
            },
        ];
    }

    public static IReadOnlyList<TradingMarker> CreatePriceMarkers(IReadOnlyList<PricePoint> data)
    {
        if (data.Count == 0)
        {
            return [];
        }

        return
        [
            new TradingMarker
            {
                Time = data[Math.Min(40, data.Count - 1)].Time,
                Shape = TradingMarkerShape.Circle,
                Placement = TradingMarkerPlacement.Above,
                Note = "Pivot",
            },
            new TradingMarker
            {
                Time = data[Math.Min(120, data.Count - 1)].Time,
                Shape = TradingMarkerShape.Triangle,
                Placement = TradingMarkerPlacement.Below,
                Note = "Pullback",
            },
        ];
    }

    public static CandlePoint CreateNextCandle(IReadOnlyList<CandlePoint> data)
    {
        var last = data[^1];
        var nextIndex = data.Count;
        var time = last.Time.AddDays(1);
        var open = last.Close;
        var drift = Math.Sin(nextIndex / 9d) * 1.2d + Math.Cos(nextIndex / 14d) * 0.8d;
        var close = open + drift;
        var high = Math.Max(open, close) + 1.0d + (Math.Abs(Math.Sin(nextIndex / 7d)) * 0.45d);
        var low = Math.Min(open, close) - 0.9d - (Math.Abs(Math.Cos(nextIndex / 6d)) * 0.35d);
        var volume = Math.Max(
            1000d,
            last.Volume * (0.94d + (Math.Abs(Math.Sin(nextIndex / 11d)) * 0.22d))
        );
        var isRising = close >= open;

        return new CandlePoint
        {
            Time = time,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
            Turnover = volume * close,
            PreviousClose = last.Close,
            IsLimitUp = nextIndex % 97 == 0 && isRising,
            IsLimitDown = nextIndex % 131 == 0 && !isRising,
        };
    }

    public static PricePoint CreateNextPrice(IReadOnlyList<PricePoint> data)
    {
        var last = data[^1];
        var nextIndex = data.Count;
        return new PricePoint
        {
            Time = last.Time.AddMinutes(5),
            Price =
                last.Price
                + (Math.Sin(nextIndex / 8d) * 0.85d)
                + (Math.Cos(nextIndex / 13d) * 0.35d)
                + 0.04d,
        };
    }

    public static void UpdateLatestCandle(IReadOnlyList<CandlePoint> data)
    {
        var last = data[^1];
        var pivot = Math.Sin(data.Count / 5d) * 0.75d;
        var nextClose = last.Close + pivot;
        var high = Math.Max(last.Open, nextClose) + 0.7d;
        var low = Math.Min(last.Open, nextClose) - 0.7d;
        var isRising = nextClose >= last.Open;

        last.Close = nextClose;
        last.High = high;
        last.Low = low;
        last.Volume *= 1.08d;
        last.Turnover = last.Volume * last.Close;
        last.IsLimitUp = isRising && data.Count % 97 == 0;
        last.IsLimitDown = !isRising && data.Count % 131 == 0;
    }

    public static void UpdateLatestPrice(IReadOnlyList<PricePoint> data)
    {
        var last = data[^1];
        last.Price += (Math.Sin(data.Count / 6d) * 0.65d) + 0.1d;
    }
}
