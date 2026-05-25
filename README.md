# TradingViewChart

Avalonia trading chart control with bindable viewport, zoom ratio, indicators, markers, and point-click interaction.

![TradingViewChart demo](https://raw.githubusercontent.com/dameng324/TradingViewChart/main/assets/demo.webp)

## Features

- Candle and price-line rendering
- Two-way binding for `VisibleStartTime`, `VisibleEndTime`, and `ZoomRatio`
- `PanByBars(int)` for programmatic panning
- Bindable point-click command support
- Built-in indicator picker plus editable indicator items
- Localizable chart UI text through Avalonia properties
- Avalonia 12 + Skia rendering

## Packages

| Package | Purpose |
| --- | --- |
| `TradingViewChart` | Reusable chart control for Avalonia apps |
| `TradingViewChart.Demo.Tool` | Installs the demo app as a `dotnet tool` |

## Install

```bash
dotnet add package TradingViewChart
```

To install the demo tool:

```bash
dotnet tool install --global TradingViewChart.Demo.Tool
tradingviewchart-demo
```

## Basic usage

```xml
<Window
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:chart="clr-namespace:TradingViewChart;assembly=TradingViewChart">
    <chart:TradingViewChart
        CandleSource="{Binding CandleSource}"
        ChartTitle="{Binding ChartTitle}"
        IndicatorItems="{Binding IndicatorItems}"
        SupportedIndicators="{Binding SupportedIndicators}"
        PointClickCommand="{Binding PointClickCommand}"
        VisibleStartTime="{Binding VisibleStartTime}"
        VisibleEndTime="{Binding VisibleEndTime}"
        ZoomRatio="{Binding ZoomRatio}"
        HoveredTime="{Binding HoveredTime}"
        CrosshairHintMode="{Binding CrosshairHintMode}"
        CrosshairValueMode="{Binding CrosshairValueMode}" />
</Window>
```

## Localizable text properties

These UI strings are exposed as Avalonia properties and can be bound or styled:

- `IndicatorButtonText`
- `EmptyStateText`
- `TooltipTimeLabel`
- `TooltipPriceLabel`
- `TooltipOhlcLabel`
- `TooltipChangeLabel`
- `TooltipTurnoverLabel`
- `TooltipVolumeLabel`
- `IndicatorMenuShowText`
- `IndicatorMenuHideText`
- `IndicatorMenuEditText`
- `IndicatorMenuDeleteText`
- `AddIndicatorTitleFormat`
- `EditIndicatorTitleFormat`

## Demo

The demo project lives in `tests/TradingViewChart.Demo` and shows:

- candle and price-line sources
- point-click commands
- panning and zoom bindings
- indicator add/edit/hide/delete flows
- theme switching
- marker rendering

Run it locally:

```bash
dotnet run --project tests/TradingViewChart.Demo/TradingViewChart.Demo.csproj
```

Or install it as a tool:

```bash
dotnet tool install --global TradingViewChart.Demo.Tool
tradingviewchart-demo
```

## Development

Restore local tools and verify formatting:

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run csharpier check .
```

Build and test:

```bash
dotnet build tests/TradingViewChart.Demo/TradingViewChart.Demo.csproj
dotnet test tests/TradingViewChart.Tests/TradingViewChart.Tests.csproj
```

Before your first publish, verify package creation locally:

```bash
dotnet pack src/TradingViewChart/TradingViewChart.csproj -c Release -o ./artifacts
dotnet pack tests/TradingViewChart.Demo/TradingViewChart.Demo.csproj -c Release -o ./artifacts
```
